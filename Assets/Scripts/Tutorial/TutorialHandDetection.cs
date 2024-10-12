using Mediapipe.Unity.CoordinateSystem;
using System.Collections;
using System.Collections.Generic;
using System.Resources;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

using Stopwatch = System.Diagnostics.Stopwatch;

namespace Mediapipe.Unity.Tutorial {
    public class HandDetection : MonoBehaviour {

        [SerializeField] private TextAsset _configAsset;
        [SerializeField] private RawImage _screen;
        [SerializeField] private MultiHandLandmarkListAnnotationController _handLandmarksAnnotationController;
        [SerializeField] private int _fps;
        private int _width;
        private int _height;
        CalculatorGraph _graph;

        private WebCamTexture _webCamTexture;
        private Texture2D _inputTexture;
        private Color32[] _inputPixelData;
        private IResourceManager _resourceManager;
        private OutputStream<List<NormalizedLandmarkList>> _handLandmarksStream;
        private List<NormalizedLandmarkList> _handLandmarks = null;

        Stopwatch _stopwatch;

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

        private IEnumerator Start() {
            if (WebCamTexture.devices.Length == 0) {
                throw new System.Exception("Web Camera devices are not found");
            }

            var webCamDevice = WebCamTexture.devices[0];
            _webCamTexture = new WebCamTexture(webCamDevice.name, _width, _height, _fps);
            _webCamTexture.Play();

            yield return new WaitUntil(() => _webCamTexture.width >= 16);

            _inputTexture = new Texture2D(_webCamTexture.width, _webCamTexture.height, TextureFormat.RGBA32, false);
            _inputPixelData = new Color32[_webCamTexture.width * _webCamTexture.height];

            _screen.texture = _webCamTexture;

            _resourceManager = new StreamingAssetsResourceManager();
            yield return _resourceManager.PrepareAssetAsync("palm_detection_full.bytes");
            yield return _resourceManager.PrepareAssetAsync("handedness.txt");
            yield return _resourceManager.PrepareAssetAsync("hand_landmark_full.bytes");

            _stopwatch = new Stopwatch();

            _graph = new CalculatorGraph(_configAsset.text);
            _handLandmarksStream = new OutputStream<List<NormalizedLandmarkList>>(_graph, "landmarks");

            _handLandmarksStream.StartPolling();
            _graph.StartRun(GetSidePacket());
            _stopwatch.Start();

            StartCoroutine(DetectCoroutine());
        }

        private PacketMap GetSidePacket() {
            var sidePacket = new PacketMap();
            sidePacket.Emplace("input_horizontally_flipped", Packet.CreateBool(false));
            sidePacket.Emplace("input_vertically_flipped", Packet.CreateBool(true));
            sidePacket.Emplace("input_rotation", Packet.CreateInt(0));
            sidePacket.Emplace("output_rotation", Packet.CreateInt(0));
            sidePacket.Emplace("num_hands", Packet.CreateInt(1));

            return sidePacket;
        }

        private void Update() {
            if (Input.GetKeyDown(KeyCode.Space)) {
                ToggleDisplayWebCam();
            }
        }

        private void ToggleDisplayWebCam() {
            _screen.enabled = !_screen.enabled;
        }

        private IEnumerator DetectCoroutine() {
            long currentTimeStamp;

            while (true) {
                _inputTexture.SetPixels32(_webCamTexture.GetPixels32(_inputPixelData));

                var imageFrame = new ImageFrame(ImageFormat.Types.Format.Srgba, _webCamTexture.width, _webCamTexture.height, _webCamTexture.width * 4, _inputTexture.GetRawTextureData<byte>());
                currentTimeStamp = _stopwatch.ElapsedTicks / (System.TimeSpan.TicksPerMillisecond / 1000);
                _graph.AddPacketToInputStream("input_video", Packet.CreateImageFrameAt(imageFrame, currentTimeStamp));

                var landmarksTask = _handLandmarksStream.WaitNextAsync();
                yield return new WaitUntil(() => landmarksTask.IsCompleted);

                var landmarksPacket = landmarksTask.Result.packet;
                
                if (landmarksPacket != null) {
       
                    
                    _handLandmarks = landmarksPacket.Get(NormalizedLandmarkList.Parser);
                    /*
                    // Flip normalized landmarks horizonally
                    foreach (var landmarks in faceLandmarks) {
                        foreach(var lm in landmarks.Landmark) {
                            lm.X = 1-lm.X;
                        }
                    }
                    */

                } else {
                    _handLandmarks = null;
                }

                _handLandmarksAnnotationController.DrawNow(_handLandmarks);

                _graph.WaitUntilIdle();
            }
        }

        private void OnDestroy() {
            StopAllCoroutines();

            if (_webCamTexture != null) {
                _webCamTexture.Stop();
            }

            _handLandmarksStream?.Dispose();
            _handLandmarksStream = null;

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
