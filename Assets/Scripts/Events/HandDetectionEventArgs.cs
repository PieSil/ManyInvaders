using Mediapipe;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class HandDetectionEventArgs : EventArgs {

    public readonly HandDetectionData DetectionData;

    public HandDetectionEventArgs(HandDetectionData detectionData) {
        DetectionData = detectionData;
    }
}
