using Mediapipe;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public struct HandData {

    public HandData(List<NormalizedLandmarkList> landmarks) {
        if (landmarks != null) {
            _landmarks = new List<NormalizedLandmarkList>(landmarks.Count);

            // deep copy
            foreach (var item in landmarks) {
                _landmarks.Add(item.Clone());
            }
        } else {
            _landmarks = null;
        }
    }

    private List<NormalizedLandmarkList> _landmarks;
    public readonly List<NormalizedLandmarkList> Landmarks => _landmarks;
}
