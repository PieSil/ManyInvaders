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

            var thumb_cmc = ExtractScaledLandmark(1, hand1_landmarks);
            var thumb_mcp = ExtractScaledLandmark(2, hand1_landmarks);
            var thumb_ip = ExtractScaledLandmark(3, hand1_landmarks); 
            var thumb_tip = ExtractScaledLandmark(4, hand1_landmarks);

            //operations on other landmarks are less important so we don't need to scale them
            var wrist = ExtractScaledLandmark(0, hand1_landmarks);
            var midf_mcp = ExtractScaledLandmark(9, hand1_landmarks);
            var midf_pip = ExtractScaledLandmark(10, hand1_landmarks);
            var midf_tip = ExtractScaledLandmark(12, hand1_landmarks);

            var ringf_mcp = ExtractScaledLandmark(13, hand1_landmarks);
            var ringf_pip = ExtractScaledLandmark(14, hand1_landmarks);
            var ringf_tip = ExtractScaledLandmark(16, hand1_landmarks);

            var pinky_mcp = ExtractScaledLandmark(17, hand1_landmarks);
            var pinky_pip = ExtractScaledLandmark(18, hand1_landmarks);
            var pinky_tip = ExtractScaledLandmark(20, hand1_landmarks);

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

            // small angle between "inward" direction and index finger direction
            bool index_toward_screen = Vector3.Angle(index_mcp_to_tip, new Vector3(0, 0, -1)) <= point_inward_angle_thres;
           

            // bool straight_index = Vector3.Angle(index_dip_to_tip, new Vector3(0, 0, -1)) < 20 || IsFingerStraight(index_mcp_to_pip, index_pip_to_tip);
            // bool straight_thumb = Mathf.Abs(Vector3.Angle(index_mcp_to_tip, thumb_mcp_to_tip)) >= gun_thumb_angle_thres;

            Vector3 palm_ortho_vec = Vector3.Cross(wrist_to_thumb_cmc, wrist_to_index_mcp);
            bool gun_thumb = Vector3.Angle(thumb_mcp_to_ip, thumb_ip_to_tip) < 40.0f; // && Vector3.Angle(palm_ortho_vec, thumb_ip_to_tip) >= 45;
            bool index_ortho = Vector3.Angle(index_pip_to_dip, new Vector3(0, 0, -1)) < 50.0f;
            bool straight_index =  Vector3.Angle(index_mcp_to_pip, index_pip_to_tip) < straight_finger_thres;

            bool open_thumb = Vector3.Angle(thumb_mcp_to_ip, thumb_ip_to_tip) < straight_finger_thres && IsFingerOpen(palm_ortho_vec, thumb_ip_to_tip);
            bool open_index = straight_index && IsFingerOpen(palm_ortho_vec, index_pip_to_tip);
            bool open_midf = Vector3.Angle(midf_mcp_to_pip, midf_pip_to_tip) < straight_finger_thres && IsFingerOpen(palm_ortho_vec, midf_pip_to_tip);
            bool open_ringf = Vector3.Angle(ringf_mcp_to_pip, ringf_pip_to_tip) < straight_finger_thres && IsFingerOpen(palm_ortho_vec, ringf_pip_to_tip);
            bool open_pinky = Vector3.Angle(pinky_mcp_to_pip, pinky_pip_to_tip) < straight_finger_thres && IsFingerOpen(palm_ortho_vec, pinky_pip_to_tip);
            // bool straight_midf = Vector3.Angle(midf_mcp_to_pip, midf_pip_to_tip) < straight_finger_thres && Vector3.Angle(palm_ortho_vec, midf_pip_to_tip) >= 45;

            /*
            bool straight_midf = IsFingerStraight(midf_mcp_to_pip, midf_pip_to_tip);
            bool straight_pinkyf = IsFingerStraight(pinky_mcp_to_pip, pinky_pip_to_tip);
            bool straight_ringf = IsFingerStraight(ringf_mcp_to_pip, ringf_pip_to_tip);
            */
            _curPoses.pointing = index_ortho || (straight_index && index_toward_screen);

            if (!_curPoses.pointing) {
                Debug.Log($"pointing: {_curPoses.pointing}, pointing_index: {straight_index}, straight_index_angle: {Vector3.Angle(index_mcp_to_pip, index_pip_to_tip)}");
            }
            // Debug.Log($"pointing index screen: {pointing_index}, toward screen angle: {Vector3.Angle(index_pip_to_dip, new Vector3(0, 0, -1))} phalanx angle: {Vector3.Angle(index_mcp_to_pip, index_pip_to_tip)}");

            // TODO: use old sytsem to detect gun and cross to detect if fingers are open or closed

            // Debug.Log($"thumb: {GetFingerOpennesString(straight_thumb)}, index: {GetFingerOpennesString(straight_index)}, midf: {GetFingerOpennesString(straight_midf)}");

            //Debug.Log($"thumb: {GetFingerOpennesString(straight_thumb)}, index: {GetFingerOpennesString(straight_index)}, midf: {GetFingerOpennesString(straight_midf)}, ringf: {GetFingerOpennesString(straight_ringf)}, pinky: {GetFingerOpennesString(straight_pinkyf)}");

            if ( _curPoses.pointing) {
                // middle finger can be either open or closed
                _curPoses.gun = !open_ringf && ! open_pinky && Mathf.Abs(Vector3.Angle(index_mcp_to_tip, thumb_mcp_to_tip)) >= gun_thumb_angle_thres;
            }

            if (!_curPoses.gun) {
                if (open_ringf || open_pinky) {
                    Debug.Log($"middle: {GetFingerOpennesString(open_midf)}, ring: {GetFingerOpennesString(open_ringf)}, pinky: {GetFingerOpennesString(open_pinky)}");
                } else {
                    Debug.Log($"No gun");
                }
            }


            /*
            if (straight_midf || straight_pinkyf) {
                // check for open hand
                _curPoses.open_hand = straight_midf && straight_pinkyf && straight_index && straight_ringf && straight_thumb;
            } else {
                // check for gun pose
                if (_curPoses.pointing) {
                    // check angle between index and thumb
                    _curPoses.gun = Mathf.Abs(Vector3.Angle(index_mcp_to_tip, thumb_mcp_to_tip)) >= gun_thumb_angle_thres;
                }
            }
            */

        }

        if (PoseEvent != null) {
            PoseEvent(this, new PoseEventArgs(_curPoses));
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

    private bool IsFingerOpen(Vector3 palm_ortho_vec, Vector3 pip_to_tip_vec) {
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
        open_hand = false;
    }

    public bool pointing;
    public bool gun;
    public bool open_hand;
}

public class PoseEventArgs : EventArgs {
    private Poses _poses;
    public Poses Poses => _poses;

    public PoseEventArgs(Poses poses) {
        _poses = poses;
    }
}