using Google.Protobuf.Collections;
using Mediapipe;
using Mediapipe.Unity.HandDetection;
using System;
using System.Collections.Generic;
using TMPro;
// using UnityEditor.Experimental.GraphView;
using UnityEngine;

public class HandPoseDetection : MonoBehaviour {

    private float _openFingerThresAngle = 45.0f;
    private float _orthoFigerThres = 0.5f;
    private float _straightFingerAngleThres = 30.0f;
    // private float point_inward_angle_thres = 75.0f;
    //private float point_up_angle_thres = 30;
    // private float gun_thumb_angle_thres = 20.0f;
    [SerializeField] HandDetectionPreprocess _detectionPreprocess;
    private Poses _curPoses;
    public Poses CurPoses => _curPoses;

    public Action<PoseEventArgs> PoseEvent;
    public Action<PointingEventArgs> AimEvent;

    void Start() {
        _curPoses.Reset();
        _detectionPreprocess.HandDetectionEvent += OnDetectionChange;
    }

    void OnDetectionChange(PreprocessedHandDetectionArgs args) {

        _curPoses.Reset();  

        if (args.HandDatas != null) {
            // extract only first hand landmarks and label
            List<Vector3> landmarks = args.HandDatas[0].vectorizedLandmarks;
            bool isRightHand = args.HandDatas[0].isRightHand;

            var wrist = landmarks[0];

            var thumbCmc = landmarks[1];
            var thumbMcp = landmarks[2];
            var thumbIp = landmarks[3];
            var thumbTip = landmarks[4];

            var indexMcp = landmarks[5];
            var indexPip = landmarks[6];
            var indexDip = landmarks[7];
            var indexTip = landmarks[8];

            var midfMcp = landmarks[9];
            var midfPip = landmarks[10];
            var midfTip = landmarks[12];

            var ringfMcp = landmarks[13];
            var ringfPip = landmarks[14];
            var ringfTip = landmarks[16];

            var pinkyMcp = landmarks[17];
            var pinkyPip = landmarks[18];
            var pinkyTip = landmarks[20];

            //palm
            Vector3 wristToThumbCmc = GetVectorBetween(wrist, thumbCmc);
            Vector3 wristToIndexMcp = GetVectorBetween(wrist, indexMcp);

            // index
            Vector3 indexMcpToPip = GetVectorBetween(indexMcp, indexPip);
            Vector3 indexPipToTip = GetVectorBetween(indexPip, indexTip);
            Vector3 indexPipToDip = GetVectorBetween(indexPip, indexDip);
            Vector3 indexDipToTip = GetVectorBetween(indexDip, indexTip);
            Vector3 indexMcpToTip = GetVectorBetween(indexMcp, indexTip);

            // thumb
            Vector3 thumbMcpToIp = GetVectorBetween(thumbMcp, thumbIp);
            Vector3 thumbIpToTip = GetVectorBetween(thumbIp, thumbTip);
            Vector3 thumbMcpToTip = GetVectorBetween(thumbMcp, thumbTip);

            // middle finger
            Vector3 midfMcpToPip = GetVectorBetween(midfMcp, midfPip);
            Vector3 midfPipToTip = GetVectorBetween(midfPip, midfTip);
            Vector3 midfMcpToTip = GetVectorBetween(midfMcp, midfTip);

            // ring finger
            Vector3 ringfMcpToPip = GetVectorBetween(ringfMcp, ringfPip);
            Vector3 ringfPipToTip = GetVectorBetween(ringfPip, ringfTip);
            Vector3 ringfMcpToTip = GetVectorBetween(ringfMcp, ringfTip);

            // pinky
            Vector3 pinkyMcpToPip = GetVectorBetween(pinkyMcp, pinkyPip);
            Vector3 pinkyPipToTip = GetVectorBetween(pinkyPip, pinkyTip);
            Vector3 pinkyMcpToTip = GetVectorBetween(pinkyMcp, pinkyTip);

            float thumbToPinkyDist = GetVectorBetween(thumbMcp, pinkyMcp).magnitude;

            // vector orthogonal to palm plane
            Vector3 palm_ortho_vec = Vector3.Cross(wristToThumbCmc, wristToIndexMcp);

            bool straight_index = IsFingerStraight(indexMcpToPip, indexPipToTip, 55.0f);
            bool ortho_index = IsFingerOrtho(indexPipToTip);
            bool open_index_check = IsFingerOpen(palm_ortho_vec, indexPipToTip);

            bool open_thumb = IsFingerStraight(thumbMcpToIp, thumbIpToTip, _straightFingerAngleThres) && IsFingerOpen(palm_ortho_vec, thumbIpToTip);
            bool open_index = straight_index && open_index_check || ortho_index;
            bool open_midf = IsFingerStraight(midfMcpToPip, midfPipToTip, 25.0f) && IsFingerOpen(palm_ortho_vec, midfPipToTip) && TipOutsidePalm(midfPip, midfTip, wrist); // || IsFingerOrtho(midf_pip_to_tip);
            bool open_ringf = IsFingerStraight(ringfMcpToPip, ringfPipToTip, _straightFingerAngleThres) && IsFingerOpen(palm_ortho_vec, ringfPipToTip); // || IsFingerOrtho(ringf_pip_to_tip);
            bool open_pinky = IsFingerStraight(pinkyMcpToPip, pinkyPipToTip, _straightFingerAngleThres) && IsFingerOpen(palm_ortho_vec, pinkyPipToTip); // || IsFingerOrtho(pinky_pip_to_tip);

            _curPoses.pointing = ortho_index || straight_index || open_index_check; //&& index_toward_screen

            if (_curPoses.pointing) {
            // middle finger can be either open or closed
                _curPoses.gun = !open_midf && !open_ringf && !open_pinky;
            // _curPoses.aiming_point = GetAimingPoint(GetVectorFromLm(index_mcp), GetVectorFromLm(index_tip), index_mcp_to_tip, ortho_index);

                if (_curPoses.gun) {

                // distance between thumb tip and index pip is a good approximation for determining shooting gesture
                    float norm_thumb_to_index_dist = GetVectorBetween(thumbTip, indexPip).magnitude / thumbToPinkyDist;
                    _curPoses.Loaded = norm_thumb_to_index_dist > 0.4f;
                }
            }

            _curPoses.open_hand = open_thumb && open_index && open_midf && open_ringf && open_pinky;

            /*
            if (!_curPoses.gun && !_curPoses.open_hand) {
                Debug.LogWarning("No gun detected");
                if (!_curPoses.pointing) {
                    Debug.LogWarning("No ortho index, straight index or index open");
                }

                if (open_midf) {
                    Debug.LogWarning("Middle finger open");
                    bool straight = IsFingerStraight(midfMcpToPip, midfPipToTip, _straightFingerAngleThres, true);
                    bool open = IsFingerOpen(palm_ortho_vec, midfPipToTip, true);
                    bool tipOutsidePalm = TipOutsidePalm(midfPip, midfTip, wrist, true);
                    Debug.LogWarning($"straight: {straight}, open: {open}, tip outside palm: {tipOutsidePalm}");
                }

                if (open_ringf) {
                    Debug.LogWarning("Ring finger open");
                    bool straight = IsFingerStraight(ringfMcpToPip, ringfPipToTip, _straightFingerAngleThres, true);
                    bool open = IsFingerOpen(palm_ortho_vec, ringfPipToTip, true);
                    Debug.LogWarning($"straight: {straight}, open: {open}");
                }

                if (open_pinky) {
                    Debug.LogWarning("Pinky open");
                    bool straight = IsFingerStraight(pinkyMcpToPip, pinkyPipToTip, _straightFingerAngleThres, true);
                    bool open = IsFingerOpen(palm_ortho_vec, pinkyPipToTip, true);
                    Debug.LogWarning($"straight: {straight}, open: {open}");
                }
            }
            */

            if (PoseEvent != null) {
                PoseEvent(new PoseEventArgs(_curPoses));
            }

            if (AimEvent != null) {
                AimEvent(new PointingEventArgs(indexMcp, indexMcpToTip));
            }

        } else {
            _curPoses.lost_hand = true;
            if (PoseEvent != null) {
                PoseEvent(new PoseEventArgs(_curPoses));
            }

            if (AimEvent != null) {
                AimEvent(new PointingEventArgs());
            }
        }
    }

    private bool IsFingerOrtho(Vector3 pipToTip) {
        return Vector3.Dot(new Vector3(0, 0, -1), pipToTip.normalized) > _orthoFigerThres;
    }

    private bool IsFingerStraight(Vector3 mcpToPip, Vector3 pipToTip, float angleThres, bool debug = false) {
        float absAngle = Mathf.Abs(Vector3.Angle(mcpToPip, pipToTip));
        if (debug) {
            Debug.LogWarning($"finger straight check, angle: {absAngle}, thres: {angleThres} (should be < thres for true)");
        }

        return absAngle < angleThres;
    }

    private bool TipOutsidePalm(Vector3 pip, Vector3 tip, Vector3 wrist, bool debug = false) {
        Vector3 wristToPip = GetVectorBetween(wrist, pip);
        Vector3 wristToTip = GetVectorBetween(wrist, tip);
        float wristToPipMagn = wristToPip.magnitude;
        float wristToTipMagn = wristToTip.magnitude;

        if (debug) {
            Debug.LogWarning($"wrist to pip magnitude: {wristToPipMagn}, wrist to tip magnitude: {wristToTipMagn}");
        }

        return wristToTipMagn - wristToPipMagn >= 0.02;
    }

    private bool IsFingerOpen(Vector3 palmOrthoVec, Vector3 pipToTip, bool debug = false) {
        float absAngle = Mathf.Abs(Vector3.Angle(palmOrthoVec, pipToTip));
        if (debug) {
            Debug.LogWarning($"finger open check, angle: {absAngle}, thres: {_openFingerThresAngle} (should be >= thres for true)");
        }
        return absAngle >= _openFingerThresAngle;
    }

    private Vector3 GetVectorBetween(Vector3 start, Vector3 end) {
        return end - start;
    }
}

public struct Poses {

    public void Reset() {
        pointing = false;
        gun = false;
        Loaded = false;
        open_hand = false;
        lost_hand = false;
    }

    public bool lost_hand;
    public bool pointing;
    public bool gun;
    public bool Loaded;
    public readonly bool Shooting => !Loaded;
    public bool open_hand;
}

public class PoseEventArgs : EventArgs {
    private Poses _poses;
    public Poses Poses => _poses;

    public PoseEventArgs(Poses poses) {
        _poses = poses;
    }
}

public class PointingEventArgs : EventArgs {
    public readonly Vector3 Mcp;
    public readonly Vector3 McpToTip;
    public readonly bool HasData;

    public PointingEventArgs(Vector3 mcp, Vector3 mcpToTip) {
        Mcp = mcp;
        McpToTip = mcpToTip;
        HasData = true;
    }

    public PointingEventArgs() {
        HasData = false;
    }
}