using Mediapipe.Unity;
using Mediapipe.Unity.HandDetection;
using Mediapipe.Unity.Tutorial;
using System;
using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UI;

public class WebcamDisplay : MonoBehaviour {

    [SerializeField] private MultiHandLandmarkListAnnotationController _handLandmarksAnnotationController;
    [SerializeField] private HandDetection _detector;
    private RawImage _display;

    public void Awake() {

        _display = GetComponent<RawImage>();
        if (_display == null) {
            Debug.LogError("WebcamDisplay must be attached to RawImage component");

            _detector.HandDetectionEvent -= OnHandLandmarksChanged;
            _detector.TrackerInitedEvent -= OnTrackerInited;
            Destroy(this);
            return;
        }

        if (_display.enabled) {
            _detector.HandDetectionEvent += OnHandLandmarksChanged;
        }

        _detector.TrackerInitedEvent += OnTrackerInited;

        if (_detector.WebCamTexture != null) {
            OnTrackerInited(new EventArgs());
        }
    }

    private void Update() {
        if (Input.GetKeyDown(KeyCode.Space)) {
            ToggleDisplayWebCam();
        }
    }

    private void OnHandLandmarksChanged(HandDetectionEventArgs args) {
        _handLandmarksAnnotationController.DrawNow(args.DetectionData.Landmarks);
    }

    private void OnTrackerInited(EventArgs args) {

        _detector.TrackerInitedEvent -= OnTrackerInited;
        float ar = ((float) _detector.WebCamTexture.width) / _detector.WebCamTexture.height;
        ResizeDisplay(ar);
        _display.texture = _detector.WebCamTexture;

    }

    private void ResizeDisplay(float ar) {

        float newWidth = Math.Abs((float)_display.rectTransform.rect.height * ar);
        float newHeight = Math.Abs((float)_display.rectTransform.rect.height);

        _display.rectTransform.sizeDelta = new Vector2(newWidth, newHeight);
    }

    private void ToggleDisplayWebCam() {
        _display.enabled = !_display.enabled;
        if (_display.enabled) {
            _detector.HandDetectionEvent += OnHandLandmarksChanged;
        } else { 
            _detector.HandDetectionEvent -= OnHandLandmarksChanged;
            _handLandmarksAnnotationController.DrawNow(null);
        }
    }

}
