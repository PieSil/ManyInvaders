using Mediapipe;
using Mediapipe.Unity.HandDetection;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;


public class HandDetectionPreprocess : MonoBehaviour
{
    [SerializeField] bool preferRightHand = true;
    [SerializeField] HandDetection _detector;

    private const int HAND_LANDMARKS_COUNT = 21;
    private string _preferredHandLabel;

    public Action<PreprocessedHandDetectionArgs> HandDetectionEvent;

    private void Awake() {
        _detector.HandDetectionEvent += OnHandDetectionEvent;
    }

    private void OnHandDetectionEvent(HandDetectionEventArgs e) {


        bool IsPreferredHand(string label) {
            return _preferredHandLabel == label;
        }

        var landmarks_list = e.DetectionData.Landmarks;
        var handedness = e.DetectionData.Handedness;
        List<PreprocessedHandData> handDatas = null;

        if (landmarks_list != null) {
            int preferredHandIndex = -1;
            handDatas = new List<PreprocessedHandData>();

            // iterate over all detections
            for (int i = 0; i < landmarks_list.Count; i++) {

                // retrieve landmarks and handedness label
                var hand_landmarks = landmarks_list[i].Landmark;
                String handedness_label = handedness[i].Classification[0].Label;

                if (preferredHandIndex < 0 && IsPreferredHand(handedness_label)) {
                    preferredHandIndex = i;
                }

                // convert landmarks to 3D vectors
                List<Vector3> vectorized_landmarks = new List<Vector3>();
                for (int j = 0; j < HAND_LANDMARKS_COUNT; j++) {
                    Vector3 vec_lm = GetVectorFromLm(hand_landmarks[j]);
                    // scale so that (0.0f, 0.0f) is the center
                    vec_lm.x *= -1.0f;
                    vec_lm.y *= -1.0f;
                    vec_lm.x += 0.5f;
                    vec_lm.y += 0.5f;
                    //vec_lm *= new Vector3(-1.0f, -1.0f, 1.0f);
                    // aiming_point += new Vector2(0.5f, 0.5f);

                    vectorized_landmarks.Add(vec_lm);
                }

                // add data to list
                PreprocessedHandData handData = new PreprocessedHandData();
                handData.isRightHand = handedness_label == "Right";
                handData.vectorizedLandmarks = vectorized_landmarks;
                handDatas.Add(handData);
            }

            // put the first preferred hand found at the head of the list, if it exists and it's not already at the head
            if (preferredHandIndex > 0) {
                handDatas.Swap(0, preferredHandIndex);
            }
        }


        if (HandDetectionEvent != null) {
            HandDetectionEvent(new PreprocessedHandDetectionArgs(handDatas));
        }

    }

    private void SetPreferredHandLabel() {
        _preferredHandLabel = preferRightHand ? "Right" : "Left";
    }

    private Vector3 GetVectorFromLm(NormalizedLandmark lm) {
        return new Vector3(lm.X, lm.Y, lm.Z);
    }
}

public struct PreprocessedHandData {
    public bool isRightHand;
    public List<Vector3> vectorizedLandmarks;
}

public class PreprocessedHandDetectionArgs : EventArgs {

    public readonly List<PreprocessedHandData> HandDatas;

    public PreprocessedHandDetectionArgs(List<PreprocessedHandData> handDatas) {
        HandDatas = handDatas;
    }

}
