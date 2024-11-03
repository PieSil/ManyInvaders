using Mediapipe;
using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

public struct HandData {

    public HandData(List<NormalizedLandmarkList> landmarks, List<ClassificationList> handedness) {
        if (landmarks != null) {
            _landmarks = new List<NormalizedLandmarkList>(landmarks.Count);

            // deep copy
            foreach (var landmarkList in landmarks) {
  
                _landmarks.Add(landmarkList.Clone());
            }
        } else {
            _landmarks = null;
        }

        if (handedness != null) {
            _handedness = new List<ClassificationList>(handedness.Count);

            foreach(var handednessList in handedness) {
                _handedness.Add(handednessList.Clone());
            }
        } else {
            _handedness = null;
        }
    }
    
    private List<NormalizedLandmarkList> _landmarks;
    public readonly List<NormalizedLandmarkList> Landmarks => _landmarks;

    private List<ClassificationList> _handedness;
    public readonly List<ClassificationList> Handedness => _handedness;
}
