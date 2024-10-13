using Mediapipe;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class HandEventArgs : EventArgs {

    public readonly HandData Hand;

    public HandEventArgs(HandData handData) {
        Hand = handData;
    }
}
