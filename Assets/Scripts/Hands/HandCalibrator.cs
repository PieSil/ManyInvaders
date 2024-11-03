using System;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UI;

public class HandCalibrator : MonoBehaviour {

    enum CalibrationState {
        OFF,
        LEFT,
        RIGHT,
        DOWN,
        TOP,
        CENTER
    }

    [SerializeField] Image _leftAnchorImg;
    [SerializeField] Image _rightAnchorImg;
    [SerializeField] Image _topAnchorImg;
    [SerializeField] Image _downAnchorImg;

    Image _curActiveAnchorImg = null;

    private CalibrationState _state;
    public bool Calibrating => _state != CalibrationState.OFF; 

    private List<float> _recorded_values = new List<float>();

    private float _aiming_plane_depth = -1.0f;
    private float _negative_x_scale = 1.0f;
    private float _negative_y_scale = 1.0f;
    private float _positive_x_scale = 1.0f;
    private float _positive_y_scale = 1.0f;

    private Vector2 _startPos;
    private List<float> _detectedValues = new List<float>();
    private float _closePointsMaxDist = 0.2f;

    private float _calibrationStartTime = -1.0f;
    private float _calibrationSeconds = 250.0f;
    private float _remainingCalibrationTime;

    int _nLost = 0;
    int _maxSubsequentLosses = 30;

    public EventHandler<PoseEventArgs> PoseCalibratedEvent;
    [SerializeField] private HandPoseSmoother _poseSmoother;

    private void Start() {
        _leftAnchorImg.enabled = false;
        _rightAnchorImg.enabled = false;
        _topAnchorImg.enabled = false;
        _downAnchorImg.enabled = false;
        _state = CalibrationState.OFF;

        _poseSmoother.PoseEvent += OnPoseChange;

        _remainingCalibrationTime = _calibrationSeconds;
        NextState();
    }

    private void NextState() {
        if (_curActiveAnchorImg != null) {
            _curActiveAnchorImg.enabled = false;
        }

        if (_state == CalibrationState.LEFT) {
            // go to right
            _state = CalibrationState.RIGHT;

            // render appropriate arrow
            _rightAnchorImg.enabled = true;
            _curActiveAnchorImg = _rightAnchorImg;

            // start gathering info

        } else if (_state == CalibrationState.RIGHT) {
            // go to down
            _state = CalibrationState.DOWN;

            // render appropriate arrow
            _downAnchorImg.enabled = true;
            _curActiveAnchorImg = _downAnchorImg;

            // start gathering info

        } else if (_state == CalibrationState.DOWN) {
            // go to top
            _state = CalibrationState.TOP;

            // render appropriate arrow
            _topAnchorImg.enabled = true;
            _curActiveAnchorImg = _topAnchorImg;

            // start gathering info

        } else if (_state == CalibrationState.TOP) {
            // go to off
            _state = CalibrationState.OFF;

            _curActiveAnchorImg = null;

        } else {
            // start from left
            _state = CalibrationState.LEFT;

            // render appropriate arrow
            _leftAnchorImg.enabled = true;
            _curActiveAnchorImg = _leftAnchorImg;

            // start gathering info
        }
    }

    private float GetScaleFactor(List<float> values, float target) {
        List<float> scale_factors = new List<float>();

        foreach (float v in values) {
            float scale;
            if (v != 0) {
                scale = target / v;
            } else {
                scale = 0;
            }

            scale_factors.Add(scale);
        }

        float avg = scale_factors.Sum() / scale_factors.Count();
        return avg;
    }

    private void ComputeCurrentStateCalibration() {

        if (_state == CalibrationState.LEFT) {
            _negative_x_scale = GetScaleFactor(_detectedValues, -0.5f);
            Debug.Log($"scale for negative x is: {_negative_x_scale}");
        } else if (_state == CalibrationState.RIGHT) {
            _positive_x_scale = GetScaleFactor(_detectedValues, 0.5f);
            Debug.Log($"scale for positive x is: {_positive_x_scale}");
        } else if (_state == CalibrationState.DOWN) {
            _negative_y_scale = GetScaleFactor(_detectedValues, -0.5f);
            Debug.Log($"scale for negative y is: {_negative_y_scale}");
        } else if (_state == CalibrationState.TOP) {
            _positive_y_scale = GetScaleFactor(_detectedValues, 0.5f);
            Debug.Log($"scale for positive y is: {_positive_y_scale}");
        }

        // else do nothing

    }

    private void RecordValue(Vector2 aiming_point) {
        if (_state == CalibrationState.LEFT || _state == CalibrationState.RIGHT) {
            _detectedValues.Add(aiming_point.x);
        } else if (_state == CalibrationState.DOWN || _state == CalibrationState.TOP) {
            _detectedValues.Add(aiming_point.y);
        }

        // else do nothing

    }

    private void OnPoseChange(object sender, RawPoseEventArgs e) {
        Vector2 aiming_point = new Vector2(.0f, .0f);
        if (Calibrating) {
            CheckStartCalibrationStep();

            bool skip_this = false;
            if (e.Poses.lost_hand) {
                skip_this = true;
                _nLost += 1;
            } else {
                _nLost = 0;
            }



            if (!skip_this) {

                // gather data
                aiming_point = GetAimingPoint(e.Mcp, e.Mcp_to_tip);

                if (_detectedValues.Count == 0) {
                    // select first starting point as _startPos
                    _startPos = aiming_point;
                }

                RecordValue(aiming_point);

                if ((aiming_point - _startPos).magnitude > _closePointsMaxDist) {
                    // hand moved too much, reset this step
                    ResetCaibrationStep(aiming_point);

                } else {
                    // check if data gathering needs to end
                    _remainingCalibrationTime -= Time.time - _calibrationStartTime;
                    // Debug.Log($"remaining time is: {_remainingCalibrationTime}");
                    if (_remainingCalibrationTime < 0) {
                        // calibrate and go to next step
                        ComputeCurrentStateCalibration();
                        StopCalibrationStep();
                        NextState();
                    }
                }
            } else if (_nLost > _maxSubsequentLosses) {
                // lost hand
                StopCalibrationStep();
            }

            if (_curActiveAnchorImg != null) {
                var text = _curActiveAnchorImg.GetComponentInChildren<TMP_Text>();
                text.text = ((int) _remainingCalibrationTime).ToString();
            }

        } else {
            aiming_point = GetCalibratedAimingPoint(e.Mcp, e.Mcp_to_tip);
        }

        Poses new_poses = e.Poses;
        new_poses.aiming_point = aiming_point;

        if (PoseCalibratedEvent != null) {
            PoseCalibratedEvent(this, new PoseEventArgs(new_poses));
        }
    }

    private void CheckStartCalibrationStep() {
        if (_calibrationStartTime < 0) {
            ResetCalibrationTimer();
        }
    }

    private void ResetCaibrationStep(Vector2 startPos) {
        ResetCalibrationTimer();
        _detectedValues.Clear();
        _startPos = startPos;
        RecordValue(startPos);
    }

    private void StopCalibrationStep() {
        StopCalibrationTimer();
        _detectedValues.Clear();
    }

    private void StopCalibrationTimer() {
        _calibrationStartTime = -1.0f;
        _remainingCalibrationTime = _calibrationSeconds;
    }

    private void ResetCalibrationTimer() {
        _calibrationStartTime = Time.time;
        _remainingCalibrationTime = _calibrationSeconds;
    }

    private Vector2 GetAimingPoint(Vector3 mcp, Vector3 mcp_to_tip) {

        Vector2 aiming_point;
        if (Vector3.Dot(mcp_to_tip, new Vector3(0, 0, 1)) == 0) {
            aiming_point = new Vector2(.0f, .0f);
        } else {

            float numerator = _aiming_plane_depth - mcp.z;
            float z_frac = numerator / mcp_to_tip.z;
            float x_intersection = (z_frac * mcp_to_tip.x) + mcp.x;
            float y_intersection = (z_frac * mcp_to_tip.y) + mcp.y;

            aiming_point = new Vector2(x_intersection, y_intersection);

            aiming_point = new Vector2(Mathf.Clamp(aiming_point.x, 0, 1), Mathf.Clamp(aiming_point.y, 0, 1));

            // scale so that (0.0f, 0.0f) is the center
            aiming_point *= new Vector2(-1.0f, -1.0f);
            aiming_point += new Vector2(0.5f, 0.5f);
        }

        return aiming_point;
    }

    private Vector2 GetCalibratedAimingPoint(Vector3 mcp, Vector3 mcp_to_tip) {
        Vector2 aiming_point = GetAimingPoint(mcp, mcp_to_tip);
        float x_scale = Mathf.Lerp(_negative_x_scale, _positive_x_scale, aiming_point.x + 0.5f);
        float y_scale = Mathf.Lerp(_negative_y_scale, _positive_y_scale, aiming_point.y + 0.5f);

        Debug.Log($"Aiming point pre-scale, x: {aiming_point.x}, y: {aiming_point.y}");

        aiming_point *= new Vector2(x_scale, y_scale);

        Debug.Log($"Aiming point post-scale, x: {aiming_point.x}, y: {aiming_point.y}");

        return aiming_point;
    }

}

public class PoseEventArgs : EventArgs {
    private Poses _poses;
    public Poses Poses => _poses;

    public PoseEventArgs(Poses poses) {
        _poses = poses;
    }
}
