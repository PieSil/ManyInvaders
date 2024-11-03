using Google.Protobuf.Collections;
using Mediapipe;
using Mediapipe.Unity.HandDetection;
using System;
using UnityEditor.Experimental.GraphView;
using UnityEngine;

public class HandPoseDetection : MonoBehaviour {

    private float straight_finger_thres = 30.0f;
    private float point_inward_angle_thres = 75.0f;
    //private float point_up_angle_thres = 30;
    private float gun_thumb_angle_thres = 20.0f;
    [SerializeField] HandDetection _detector;
    private Poses _curPoses;
    public Poses CurPoses => _curPoses;

    public EventHandler<RawPoseEventArgs> PoseEvent;

    void Start() {
        _curPoses.Reset();
        _detector.HandDetectionEvent += OnDetectionChange;
    }

    void OnDetectionChange(HandEventArgs args) {

        _curPoses.Reset();  
        var landmarks_list = args.Hand.Landmarks;

        if (landmarks_list != null) {

            var hand1_landmarks = landmarks_list[0].Landmark;
            var index_mcp = ExtractLandmark(5, hand1_landmarks);
            var index_pip = ExtractLandmark(6, hand1_landmarks);
            var index_dip = ExtractLandmark(7, hand1_landmarks); 
            var index_tip = ExtractLandmark(8, hand1_landmarks);

            var thumb_cmc = ExtractLandmark(1, hand1_landmarks);
            var thumb_mcp = ExtractLandmark(2, hand1_landmarks);
            var thumb_ip = ExtractLandmark(3, hand1_landmarks); 
            var thumb_tip = ExtractLandmark(4, hand1_landmarks);

            //operations on other landmarks are less important so we don't need to scale them
            var wrist = ExtractScaledLandmark(0, hand1_landmarks);
            var midf_mcp = ExtractLandmark(9, hand1_landmarks);
            var midf_pip = ExtractLandmark(10, hand1_landmarks);
            var midf_tip = ExtractLandmark(12, hand1_landmarks);

            var ringf_mcp = ExtractLandmark(13, hand1_landmarks);
            var ringf_pip = ExtractLandmark(14, hand1_landmarks);
            var ringf_tip = ExtractLandmark(16, hand1_landmarks);

            var pinky_mcp = ExtractLandmark(17, hand1_landmarks);
            var pinky_pip = ExtractLandmark(18, hand1_landmarks);
            var pinky_tip = ExtractLandmark(20, hand1_landmarks);

            //palm
            Vector3 wrist_to_thumb_cmc = GetVectorBetweenLm(wrist, thumb_cmc);
            Vector3 wrist_to_index_mcp = GetVectorBetweenLm(wrist, index_mcp);

            // index
            Vector3 index_mcp_to_pip = GetVectorBetweenLm(index_mcp, index_pip);
            Vector3 index_pip_to_tip = GetVectorBetweenLm(index_pip, index_tip);
            Vector3 index_pip_to_dip = GetVectorBetweenLm(index_pip, index_dip);
            Vector3 index_dip_to_tip = GetVectorBetweenLm(index_dip, index_tip);
            Vector3 index_mcp_to_tip = GetVectorBetweenLm(index_mcp, index_tip);

            // thumb
            Vector3 thumb_mcp_to_ip = GetVectorBetweenLm(thumb_mcp, thumb_ip);
            Vector3 thumb_ip_to_tip = GetVectorBetweenLm(thumb_ip, thumb_tip);
            Vector3 thumb_mcp_to_tip = GetVectorBetweenLm(thumb_mcp, thumb_tip);

            // middle finger
            Vector3 midf_mcp_to_pip = GetVectorBetweenLm(midf_mcp, midf_pip);
            Vector3 midf_pip_to_tip = GetVectorBetweenLm(midf_pip, midf_tip);
            Vector3 midf_mcp_to_tip = GetVectorBetweenLm(midf_mcp, midf_tip);

            // ring finger
            Vector3 ringf_mcp_to_pip = GetVectorBetweenLm(ringf_mcp, ringf_pip);
            Vector3 ringf_pip_to_tip = GetVectorBetweenLm(ringf_pip, ringf_tip);
            Vector3 ringf_mcp_to_tip = GetVectorBetweenLm(ringf_mcp, ringf_tip);

            // pinky
            Vector3 pinky_mcp_to_pip = GetVectorBetweenLm(pinky_mcp, pinky_pip);
            Vector3 pinky_pip_to_tip = GetVectorBetweenLm(pinky_pip, pinky_tip);
            Vector3 pinky_mcp_to_tip = GetVectorBetweenLm(pinky_mcp, pinky_tip);

            float thumb_to_pinky_dist = GetVectorBetweenLm(thumb_mcp, pinky_mcp).magnitude;

            // small angle between "inward" direction and index finger direction
            // bool index_toward_screen = Vector3.Angle(index_mcp_to_tip, new Vector3(0, 0, -1)) <= point_inward_angle_thres;
           

            // bool straight_index = Vector3.Angle(index_dip_to_tip, new Vector3(0, 0, -1)) < 20 || IsFingerStraight(index_mcp_to_pip, index_pip_to_tip);
            // bool straight_thumb = Mathf.Abs(Vector3.Angle(index_mcp_to_tip, thumb_mcp_to_tip)) >= gun_thumb_angle_thres;

            Vector3 palm_ortho_vec = Vector3.Cross(wrist_to_thumb_cmc, wrist_to_index_mcp);
            // bool gun_thumb = Vector3.Angle(thumb_mcp_to_ip, thumb_ip_to_tip) < 40.0f; // && Vector3.Angle(palm_ortho_vec, thumb_ip_to_tip) >= 45;
            bool index_ortho = Vector3.Angle(index_pip_to_dip, new Vector3(0, 0, -1)) < 50.0f;
            bool straight_index =  Vector3.Angle(index_mcp_to_pip, index_pip_to_tip) < straight_finger_thres;

            bool open_thumb = Vector3.Angle(thumb_mcp_to_ip, thumb_ip_to_tip) < straight_finger_thres && IsFingerOpen(palm_ortho_vec, thumb_ip_to_tip, thumb_to_pinky_dist);
            bool ortho_index = Vector3.Dot(new Vector3(0, 0, -1), index_pip_to_tip.normalized) > 0.6f;
            bool open_index = straight_index && IsFingerOpen(palm_ortho_vec, index_pip_to_tip, thumb_to_pinky_dist) || ortho_index;
            bool open_midf = Vector3.Angle(midf_mcp_to_pip, midf_pip_to_tip) < straight_finger_thres && IsFingerOpen(palm_ortho_vec, midf_pip_to_tip, thumb_to_pinky_dist) || Vector3.Dot(new Vector3(0, 0, -1), midf_pip_to_tip.normalized) > 0.6f;
            bool open_ringf = Vector3.Angle(ringf_mcp_to_pip, ringf_pip_to_tip) < straight_finger_thres && IsFingerOpen(palm_ortho_vec, ringf_pip_to_tip, thumb_to_pinky_dist) || Vector3.Dot(new Vector3(0, 0, -1), ringf_pip_to_tip.normalized) > 0.6f;
            // Debug.Log($"ringf dot with entering vector: {Vector3.Dot(new Vector3(0, 0, -1), ringf_pip_to_tip.normalized)}");
            bool open_pinky = Vector3.Angle(pinky_mcp_to_pip, pinky_pip_to_tip) < straight_finger_thres && IsFingerOpen(palm_ortho_vec, pinky_pip_to_tip, thumb_to_pinky_dist) || Vector3.Dot(new Vector3(0, 0, -1), pinky_pip_to_tip.normalized) > 0.6f; ;

            _curPoses.pointing = index_ortho || (straight_index); //&& index_toward_screen

            if ( _curPoses.pointing) {
                // middle finger can be either open or closed
                _curPoses.gun = !open_ringf && !open_pinky;
                // _curPoses.aiming_point = GetAimingPoint(GetVectorFromLm(index_mcp), GetVectorFromLm(index_tip), index_mcp_to_tip, ortho_index);

                if (_curPoses.gun) {

                    // distance between thumb tip and index pip is a good approximation for determining shooting gesture
                    float norm_thumb_to_index_dist = GetVectorBetweenLm(thumb_tip, index_pip).magnitude / thumb_to_pinky_dist;
                    _curPoses.Loaded = norm_thumb_to_index_dist > 0.4f;
                }
            }

            _curPoses.open_hand = open_thumb && open_index && open_midf && open_ringf && open_pinky;

            if (PoseEvent != null) {
                PoseEvent(this, new RawPoseEventArgs(_curPoses, GetVectorFromLm(index_mcp), GetVectorFromLm(index_tip), index_mcp_to_tip));
            }

        } else {
            _curPoses.lost_hand = true;
            if (PoseEvent != null) {
                PoseEvent(this, new RawPoseEventArgs(_curPoses, new Vector3(0, 0, 0), new Vector3(0, 0, 0), new Vector3(0, 0, 0)));
            }
        }
    }

    private NormalizedLandmark ExtractLandmark(int index, RepeatedField<NormalizedLandmark> hand_landmarks) {
        return hand_landmarks[index];
    }

    private NormalizedLandmark ExtractScaledLandmark(int index, RepeatedField<NormalizedLandmark> hand_landmarks) {
        var lm = hand_landmarks[index];
        lm.X *= _detector.WebCamTexture.width;
        lm.Y *= _detector.WebCamTexture.height;
        lm.Z *= _detector.WebCamTexture.width; // depth is directly proportional to image width
        return lm;
    }

    private bool IsFingerStraight(Vector3 mcp_to_pip, Vector3 pip_to_tip) {
        return Mathf.Abs(Vector3.Angle(mcp_to_pip, pip_to_tip)) <= straight_finger_thres;
    }

    private bool IsFingerOpen(Vector3 palm_ortho_vec, Vector3 pip_to_tip_vec, float norm_factor) {
        return Vector3.Angle(palm_ortho_vec, pip_to_tip_vec) >= 45;
    }

    private Vector3 GetVectorBetweenLm(NormalizedLandmark start, NormalizedLandmark end) {
        return new Vector3(end.X - start.X, end.Y - start.Y, end.Z - start.Z);
    }

    private Vector3 GetVectorFromLm(NormalizedLandmark lm) {
        return new Vector3(lm.X, lm.Y, lm.Z);
    }

    private string GetFingerOpennesString(bool isOpen) {
        if (isOpen) {
            return "open";
        } else {
            return "closed";
        }
    }
}

public struct Poses {

    public void Reset() {
        pointing = false;
        gun = false;
        Loaded = false;
        open_hand = false;
        lost_hand = false;
        aiming_point = new Vector2();
    }

    public Vector2 aiming_point;
    public bool lost_hand;
    public bool pointing;
    public bool gun;
    public bool Loaded;
    public readonly bool Shooting => !Loaded;
    public bool open_hand;
}

public class RawPoseEventArgs : EventArgs {
    private Vector3 _mcp;
    public Vector3 Mcp => _mcp;
    private Vector3 _tip;
    public Vector3 Tip => _tip;
    private Vector3 _mcp_to_tip;
    public Vector3 Mcp_to_tip => _mcp_to_tip;
    private Poses _poses;
    public Poses Poses => _poses;

    public RawPoseEventArgs(Poses poses) {
        _poses = poses;
        _mcp = new Vector3();
        _tip = new Vector3();
        _mcp_to_tip = new Vector3();
    }

    public RawPoseEventArgs(Poses poses, Vector3 mcp, Vector3 tip, Vector3 mcp_to_tip) {
        _poses = poses;
        _mcp = mcp;
        _tip = tip;
        _mcp_to_tip = mcp_to_tip;
    }
}