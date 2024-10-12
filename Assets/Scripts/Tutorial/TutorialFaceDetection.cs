using Mediapipe.Unity.CoordinateSystem;
using System.Collections;
using System.Collections.Generic;
using System.Resources;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

using Stopwatch = System.Diagnostics.Stopwatch;

namespace Mediapipe.Unity.Tutorial {
    public class FaceDetection : MonoBehaviour {

        [SerializeField] private TextAsset _configAsset;
        [SerializeField] private RawImage _screen;
        [SerializeField] private MultiFaceLandmarkListAnnotationController _faceLandmarksAnnotationController;
        [SerializeField] private int _fps;
        private int _width;
        private int _height;
        CalculatorGraph _graph;

        private WebCamTexture _webCamTexture;
        private Texture2D _inputTexture;
        //private Texture2D _outputTexture;
        private Color32[] _inputPixelData;
        //private Color32[] _outputPixelData;
        private IResourceManager _resourceManager;
        //private OutputStream<ImageFrame> _outputVideoStream;
        private OutputStream<List<NormalizedLandmarkList>> _faceLandmarksStream;

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
            //_outputTexture = new Texture2D(_webCamTexture.width, _webCamTexture.height, TextureFormat.RGBA32, false);
            //_outputPixelData = new Color32[_webCamTexture.width * _webCamTexture.height];

            //_screen.rectTransform.sizeDelta = new Vector2(_webCamTexture.width, _webCamTexture.height);
            //_screen.texture = _outputTexture;
            _screen.texture = _webCamTexture;

            _resourceManager = new LocalResourceManager();
            yield return _resourceManager.PrepareAssetAsync("face_detection_short_range.bytes");
            yield return _resourceManager.PrepareAssetAsync("face_landmark_with_attention.bytes");

            _stopwatch = new Stopwatch();

            _graph = new CalculatorGraph(_configAsset.text);
            //_outputVideoStream = new OutputStream<ImageFrame>(_graph, "output_video");
            _faceLandmarksStream = new OutputStream<List<NormalizedLandmarkList>>(_graph, "multi_face_landmarks");

            //_outputVideoStream.StartPolling();
            _faceLandmarksStream.StartPolling();
            _graph.StartRun();
            _stopwatch.Start();

            StartCoroutine(DetectCoroutine());
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
                _graph.AddPacketToInputStream("input_video", Packet.CreateImageFrameAt(imageFrame, currentTimeStamp)); ;

                //var detectTask = _outputVideoStream.WaitNextAsync();
                var landmarksTask = _faceLandmarksStream.WaitNextAsync();
                yield return new WaitUntil(() => landmarksTask.IsCompleted);
                //var globalTask = System.Threading.Tasks.Task.WhenAll(detectTask, landmarksTask);
                //yield return new WaitUntil(() => globalTask.IsCompleted);

                /*
                if (!detectTask.Result.ok || !landmarksTask.Result.ok) {
                    throw new System.Exception("Something went wrong");
                }

                var outputPacket = detectTask.Result.packet;
                if (outputPacket != null) {
                    var outputVideo = outputPacket.Get();

                    if (outputVideo.TryReadPixelData(_outputPixelData)) {
                        _outputTexture.SetPixels32(_outputPixelData);
                        _outputTexture.Apply();
                    }
                } else {
                    Debug.LogError("outputPacket is null");
                }
                */

                var landmarksPacket = landmarksTask.Result.packet;
                
                // 
                if (landmarksPacket != null) {
       
                    
                    var faceLandmarks = landmarksPacket.Get(NormalizedLandmarkList.Parser);
                    /*
                    // Flip normalized landmarks horizonally
                    foreach (var landmarks in faceLandmarks) {
                        foreach(var lm in landmarks.Landmark) {
                            lm.X = 1-lm.X;
                        }
                    }
                    */

                    _faceLandmarksAnnotationController.DrawNow(faceLandmarks);
                    /*
                    if (faceLandmarks != null && faceLandmarks.Count > 0) {
                        foreach (var landmark in faceLandmarks) {
                            var topOfHead = landmark.Landmark[10];
                            Debug.Log($"Unity Local Coordinates: {_screen.rectTransform.rect.GetPoint(topOfHead)}, Image Coordinates: {topOfHead}");
                        }
                    }
                    */
                } else {
                    _faceLandmarksAnnotationController.DrawNow(null);
                }

                yield return null;
            }
        }

        private void OnDestroy() {
            StopAllCoroutines();

            if (_webCamTexture != null) {
                _webCamTexture.Stop();
            }

            //_outputVideoStream?.Dispose();
            //_outputVideoStream = null;
            _faceLandmarksStream?.Dispose();
            _faceLandmarksStream = null;

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
