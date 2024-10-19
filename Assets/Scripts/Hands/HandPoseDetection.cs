using Google.Protobuf.Collections;
using Mediapipe;
using Mediapipe.Unity.HandDetection;
using System;
using UnityEngine;

public class HandPoseDetection : MonoBehaviour {

    private float straight_finger_thres = 40.0f;
    private float point_in_angle_thres = 90.0f;
    //private float point_up_angle_thres = 30;
    private float gun_thumb_angle_thres = 30.0f;
    [SerializeField] HandDetection _detector;
    private Poses _curPoses;
    public Poses CurPoses => _curPoses;

    public EventHandler<PoseEventArgs> PoseEvent;

    void Start() {
        _curPoses.Reset();
        _detector.HandDetectionEvent += OnDetectionChange;
    }

    void OnDetectionChange(HandEventArgs args) {

        _curPoses.Reset();  
        var landmarks_list = args.Hand.Landmarks;

        if (landmarks_list != null) {

            var hand1_landmarks = landmarks_list[0].Landmark;
            var index_mcp = ExtractScaledLandmark(5, hand1_landmarks);
            var index_pip = ExtractScaledLandmark(6, hand1_landmarks);
            var index_dip = ExtractScaledLandmark(7, hand1_landmarks); 
            var index_tip = ExtractScaledLandmark(8, hand1_landmarks);

            var thumb_mcp = ExtractScaledLandmark(2, hand1_landmarks);
            var thumb_tip = ExtractScaledLandmark(4, hand1_landmarks);

            Vector3 index_mcp_to_pip = GetVectorBetweenLm(index_mcp, index_pip);
            Vector3 index_pip_to_tip = GetVectorBetweenLm(index_pip, index_tip);
            Vector3 index_dip_to_tip = GetVectorBetweenLm(index_dip, index_tip);
            //Vector3 pip_to_dip = GetVectorBetweenLm(index_pip, index_dip);
            Vector3 index_mcp_to_tip = GetVectorBetweenLm(index_mcp, index_tip);

            Vector3 thumb_mcp_to_tip = GetVectorBetweenLm(thumb_mcp, thumb_tip);

            // small absolute angle bewteen phalanges angles
            float inward_index_angle = Vector3.Angle(index_mcp_to_tip, new Vector3(0, 0, -1));
            bool straight_index = Vector3.Angle(index_dip_to_tip, new Vector3(0, 0, -1)) < 20 || Mathf.Abs(Vector3.Angle(index_mcp_to_pip, index_pip_to_tip)) <= straight_finger_thres;

            // small angle between "inward" direction and index finger direction
            bool index_toward_screen = inward_index_angle <= point_in_angle_thres;
            _curPoses.pointing = straight_index && index_toward_screen;

            //if index is pointing we also check if gun pose is true 
            if (_curPoses.pointing) {
                // check angle between index and thumb
                _curPoses.gun = Mathf.Abs(Vector3.Angle(index_mcp_to_tip, thumb_mcp_to_tip)) >= gun_thumb_angle_thres;
            }
        }

        if (PoseEvent != null) {
            PoseEvent(this, new PoseEventArgs(_curPoses));
        }
    }

    private NormalizedLandmark ExtractScaledLandmark(int index, RepeatedField<NormalizedLandmark> hand_landmarks) {
        var lm = hand_landmarks[index];
        lm.X *= _detector.WebCamTexture.width;
        lm.Y *= _detector.WebCamTexture.height;
        lm.Z *= _detector.WebCamTexture.width; // depth is directly proportional to image width
        return lm;
    }

    private Vector3 GetVectorBetweenLm(NormalizedLandmark start, NormalizedLandmark end) {
        return new Vector3(end.X - start.X, end.Y - start.Y, end.Z - start.Z);
    }
}

public struct Poses {

    public void Reset() {
        pointing = false;
        gun = false;
    }

    public bool pointing;
    public bool gun;
}

public class PoseEventArgs : EventArgs {
    private Poses _poses;
    public Poses Poses => _poses;

    public PoseEventArgs(Poses poses) {
        _poses = poses;
    }
}