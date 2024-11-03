using Mediapipe;
using Mediapipe.Unity.CoordinateSystem;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.SymbolStore;
using System.Resources;
using TMPro;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UI;

using Stopwatch = System.Diagnostics.Stopwatch;

namespace Mediapipe.Unity.HandDetection {

    public class HandDetection : MonoBehaviour {

        [SerializeField] private TextAsset _configAsset;
        //[SerializeField] private RawImage _screen;
        [SerializeField] private int _fps;
        [SerializeField] private int _detectionFps = 0;
        CalculatorGraph _graph;

        private WebCamTexture _webCamTexture;
        public WebCamTexture WebCamTexture => _webCamTexture;
        private Texture2D _inputTexture;
        private Color32[] _inputPixelData;
        private IResourceManager _resourceManager;
        private OutputStream<List<NormalizedLandmarkList>> _handLandmarksStream;
        private OutputStream<List<ClassificationList>> _handenessStream;

        private List<NormalizedLandmarkList> _handLandmarks = null;
        private List<ClassificationList> _handedness = null;

        public event Action<EventArgs> TrackerInitedEvent;
        public event Action<HandEventArgs> HandDetectionEvent;

        Stopwatch _stopwatch;

        /*
        private void Awake() {
            float fwidth = _screen.rectTransform.rect.width + 1;
            float fheight = _screen.rectTransform.rect.height + 1;

            Canvas canvas = _screen.GetComponentInParent<Canvas>();
            if (canvas != null) {
                fwidth *= canvas.scaleFactor;
                fheight *= canvas.scaleFactor;
            }

            _width = (int)fwidth + 1;
            _height = (int)fheight + 1;

            Debug.Log($"width is {_width}\nheight is {_height}");
        }
        */

        private IEnumerator Start() {
            if (WebCamTexture.devices.Length == 0) {
                throw new System.Exception("Web Camera devices are not found");
            }

            var webCamDevice = WebCamTexture.devices[0];

            _webCamTexture = new WebCamTexture(webCamDevice.name);
            _webCamTexture.requestedFPS = _fps;

            _webCamTexture.Play();

            yield return new WaitForSeconds(2); //wait a reasonable amount of time for _webCamtexture to fully initialize, not very clean but it works
            yield return new WaitUntil(() => _webCamTexture.width >= 16); //double check

            _inputTexture = new Texture2D(_webCamTexture.width, _webCamTexture.height, TextureFormat.RGBA32, false);
            _inputPixelData = new Color32[_webCamTexture.width * _webCamTexture.height];

            _resourceManager = new StreamingAssetsResourceManager();
            yield return _resourceManager.PrepareAssetAsync("palm_detection_full.bytes");
            yield return _resourceManager.PrepareAssetAsync("handedness.txt");
            yield return _resourceManager.PrepareAssetAsync("hand_landmark_full.bytes");

            _stopwatch = new Stopwatch();

            _graph = new CalculatorGraph(_configAsset.text);
            _handLandmarksStream = new OutputStream<List<NormalizedLandmarkList>>(_graph, "landmarks");
            _handenessStream = new OutputStream<List<ClassificationList>>(_graph, "handedness");

            _handLandmarksStream.StartPolling();
            _handenessStream.StartPolling();
            _graph.StartRun(GetSidePacket());
            _stopwatch.Start();

            if (TrackerInitedEvent != null) {
                TrackerInitedEvent(new EventArgs());
            }

            StartCoroutine(DetectCoroutine());
        }

        private PacketMap GetSidePacket() {
            var sidePacket = new PacketMap();
            sidePacket.Emplace("input_horizontally_flipped", Packet.CreateBool(true));
            sidePacket.Emplace("input_vertically_flipped", Packet.CreateBool(true));
            sidePacket.Emplace("input_rotation", Packet.CreateInt(0));
            sidePacket.Emplace("output_rotation", Packet.CreateInt(0));
            sidePacket.Emplace("num_hands", Packet.CreateInt(1));

            return sidePacket;
        }

        public string FlipLabel(string label) {
            if (label == "Right") {
                return "Left";
            } else if (label == "Left") {
                return "Right";
            } else {
                return label;
            }
        }
        private IEnumerator DetectCoroutine() {
            long currentTimeStamp;

            while (true) {

                _graph.WaitUntilIdle();
                _inputTexture.SetPixels32(_webCamTexture.GetPixels32(_inputPixelData));

                var imageFrame = new ImageFrame(ImageFormat.Types.Format.Srgba, _webCamTexture.width, _webCamTexture.height, _webCamTexture.width * 4, _inputTexture.GetRawTextureData<byte>());
                currentTimeStamp = _stopwatch.ElapsedTicks / (System.TimeSpan.TicksPerMillisecond / 1000);
                _graph.AddPacketToInputStream("input_video", Packet.CreateImageFrameAt(imageFrame, currentTimeStamp));

                var landmarksTask = _handLandmarksStream.WaitNextAsync();
                var handednessTask = _handenessStream.WaitNextAsync();

                var globalTask = System.Threading.Tasks.Task.WhenAll(landmarksTask, handednessTask);

                //yield return new WaitUntil(() => landmarksTask.IsCompleted);
                yield return new WaitUntil(() => globalTask.IsCompleted);

                var landmarksPacket = landmarksTask.Result.packet;
                
                if (landmarksPacket != null) {
       
                    
                    _handLandmarks = landmarksPacket.Get(NormalizedLandmarkList.Parser);
                    
                    
                    // Flip normalized landmarks horizontally
                    foreach (var landmarks in _handLandmarks) {
                        foreach(var lm in landmarks.Landmark) {
                            lm.X = 1-lm.X;
                        }
                    }
                    

                } else {
                    _handLandmarks = null;
                }

                var handednessPacket = handednessTask.Result.packet;

                if (handednessPacket != null) {
                    _handedness = handednessPacket.Get(ClassificationList.Parser);
                    
                    /*
                    int i = 0;
                    foreach (var item in _handedness) {

                        //flip hand labels in classification as I did not find a better solution
                        //there is a better way to handle this for sure but I'm in a hurry

                        foreach (var classification in item.Classification) {
                            // classification.Label = FlipLabel(classification.Label);
                            /Debug.Log($"handedness item {i} is: {item}");
                        }
                        i++;
                    }
                    */
                    
                } else {
                    _handedness = null;
                }

                if (HandDetectionEvent != null) {
                    HandDetectionEvent(new HandEventArgs(new HandData(_handLandmarks, _handedness)));
                }

                if (_detectionFps > 0) {
                    yield return new WaitForSeconds(1/_detectionFps);
                }

            }
        }

        private void OnDestroy() {
            StopAllCoroutines();

            if (_webCamTexture != null) {
                _webCamTexture.Stop();
            }

            _handLandmarksStream?.Dispose();
            _handLandmarksStream = null;

            _handenessStream?.Dispose();
            _handenessStream = null;

            if (_graph != null) {
                try {
                    _graph.CloseInputStream("input_video");
                    _graph.WaitUntilDone();
                } finally {
                    _graph.Dispose();
                    _graph = null;
                }
                
            }
        }

    }

}
