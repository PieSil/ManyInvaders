using Mediapipe;
using Mediapipe.Unity.HandDetection;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Unity.VisualScripting;
using UnityEngine;

public class LandmarkSmoother : MonoBehaviour
{
    [SerializeField] HandDetection _detector;

    private List<Vector3> _prevSmoothedLandmarks = new List<Vector3>();
    private String _prevHadednessLabel = "";
    private float _alpha = 0.2f;
    private const int HAND_LANDMARKS_COUNT = 21;
    private bool _smooth = false;

    public Action<SmoothedHandDetectionArgs> HandDetectionEvent;

    private void Awake() {
        _detector.HandDetectionEvent += OnHandDetectionEvent;
    }

    private void OnHandDetectionEvent(HandDetectionEventArgs e) {
        var landmarks_list = e.DetectionData.Landmarks;
        var handedness = e.DetectionData.Handedness;

        if (_smooth) {
            // var handednessLabel = e.Hand.Handedness[0].Classification[0].Label;
            List<Vector3> final_landmarks = null;
            String final_label = "";

            bool _prevSmoothedLandmarksFull = _prevSmoothedLandmarks.Count == HAND_LANDMARKS_COUNT;
            bool clearPrevious = false;

            if (_prevSmoothedLandmarksFull) {
                // we already have some landmarks

                if (landmarks_list != null) {
                    // we have a detection
                    var handedness_label = handedness[0].Classification[0].Label;
                    bool sameHandedness = handedness_label == _prevHadednessLabel;

                    if (sameHandedness) {
                        // same handedness as before: can smooth value
                        var first_hand_landmarks = landmarks_list[0].Landmark;
                        final_landmarks = new List<Vector3>();
                        final_label = _prevHadednessLabel; // same as current lable since we have checked

                        for (int i = 0; i < HAND_LANDMARKS_COUNT; i++) {
                            Vector3 new_lm = GetVectorFromLm(first_hand_landmarks[i]);

                            // Exponential Moving Average
                            new_lm *= _alpha;
                            new_lm += (1 - _alpha) * _prevSmoothedLandmarks[i];
                            final_landmarks.Add(new_lm);
                        }

                        FillPreviousLandmarks(final_landmarks);

                    } else {
                        // other handedness: must reset list
                        _prevSmoothedLandmarks.Clear();
                        _prevSmoothedLandmarksFull = false;
                        FillPreviousLandmarks(landmarks_list[0].Landmark, handedness_label);
                    }

                } else {
                    // we have no detection, send the previous landmarks and reset them before next call
                    final_landmarks = _prevSmoothedLandmarks;
                    final_label = _prevHadednessLabel;
                    clearPrevious = true;
                }

            } else {
                // we have no smoothed landmarks

                if (landmarks_list != null) {
                    // we have a detection, fill previous landmarks and send them
                    var handedness_label = handedness[0].Classification[0].Label;
                    FillPreviousLandmarks(landmarks_list[0].Landmark, handedness_label);

                } else {
                    // we have no detection, do nothing
                }

            }

            if (HandDetectionEvent != null) {
                HandDetectionEvent(new SmoothedHandDetectionArgs(final_landmarks, final_label));
            }

            if (clearPrevious) {
                _prevSmoothedLandmarks.Clear();
                _prevHadednessLabel = "";
            }
        } else {
            List<Vector3> final_landmarks = null;
            String final_label = "";

            if (landmarks_list != null) {
                var first_hand_landmarks = landmarks_list[0].Landmark;
                final_landmarks = new List<Vector3>();
                final_label = handedness[0].Classification[0].Label; // same as current label since we have checked

                for (int i = 0; i < HAND_LANDMARKS_COUNT; i++) {
                    Vector3 new_lm = GetVectorFromLm(first_hand_landmarks[i]);

                    final_landmarks.Add(new_lm);
                }
            }

            if (HandDetectionEvent != null) {
                HandDetectionEvent(new SmoothedHandDetectionArgs(final_landmarks, final_label));
            }
        }
    }

    private void FillPreviousLandmarks(Google.Protobuf.Collections.RepeatedField<NormalizedLandmark> landmarks, System.String handedness_label = null) {

        for (int i = 0; i < HAND_LANDMARKS_COUNT; i++) {
            Vector3 landmark = GetVectorFromLm(landmarks[i]);
            if (_prevSmoothedLandmarks.Count <= i) {
                _prevSmoothedLandmarks.Add(landmark);
            } else {
                _prevSmoothedLandmarks[i] = landmark;
            }
        }

        if (handedness_label != null) {
            _prevHadednessLabel = handedness_label;
        }
    }

    private void FillPreviousLandmarks(List<Vector3> landmarks, System.String handedness_label = null) {

        for (int i = 0; i < HAND_LANDMARKS_COUNT; i++) {
            Vector3 landmark = landmarks[i];
            if (_prevSmoothedLandmarks.Count <= i) {
                _prevSmoothedLandmarks.Add(landmark);
            } else {
                _prevSmoothedLandmarks[i] = landmark;
            }
        }

        if (handedness_label != null) {
            _prevHadednessLabel = handedness_label;
        }
    }

    private Vector3 GetVectorFromLm(NormalizedLandmark lm) {
        return new Vector3(lm.X, lm.Y, lm.Z);
    }


}

public class SmoothedHandDetectionArgs : EventArgs {

    private List<Vector3> _landmarks = null;
    public List<Vector3> Landmarks => _landmarks;
    private System.String _handednessLabel = "";
    public System.String HandednessLabel => _handednessLabel;

    public SmoothedHandDetectionArgs(List<Vector3> landmarks, System.String handednessLabel) {
        if (landmarks != null) {
            _landmarks = new List<Vector3>();
            foreach (var lm in landmarks) {
                _landmarks.Add(lm);
            }
        }

        _handednessLabel = handednessLabel;
    }

}
