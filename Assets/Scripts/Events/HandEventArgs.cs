using Mediapipe;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class HandEventArgs : EventArgs {

    public readonly HandData Hand;

    public HandEventArgs(List<NormalizedLandmarkList> landmarks) {
        Hand = new HandData(landmarks);
    }
}
