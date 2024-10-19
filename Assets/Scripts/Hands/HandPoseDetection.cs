using Mediapipe.Unity.HandDetection;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class HandPositionDetection : MonoBehaviour {

    [SerializeField] HandDetection _detector;

    private 

    void Start() {
        _detector.HandDetectionEvent += OnDetectionChange;
    }

    void OnDetectionChange(HandEventArgs args) {

    }
}
