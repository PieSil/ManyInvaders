using System;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UI;

public class AimCalibrator : MonoBehaviour {

    enum CalibrationState {
        OFF,
        LEFT,
        RIGHT,
        DOWN,
        TOP,
        CENTER
    }

    [SerializeField] HandInputManager _inputManager;
    [SerializeField] Image _leftAnchorImg;
    [SerializeField] Image _rightAnchorImg;
    [SerializeField] Image _topAnchorImg;
    [SerializeField] Image _downAnchorImg;

    Image _curActiveAnchorImg = null;

    private CalibrationState _state;
    public bool Calibrating => _state != CalibrationState.OFF; 

    private List<float> _recorded_values = new List<float>();
    private float _aiming_plane_depth = -1.0f;

    /*
    private float _negative_x_scale = 1.0f;
    private float _negative_y_scale = 1.0f;
    private float _center_x_scale = 1.0f;
    private float _center_y_scale = 1.0f;
    private float _positive_x_scale = 1.0f;
    private float _positive_y_scale = 1.0f;
    */

    private float _min_x = -0.5f;
    private float _max_x = 0.5f;
    private float _min_y = -0.5f;
    private float _max_y = 0.5f;

    private Vector2 _startPos;
    private List<Vector2> _detectedValues = new List<Vector2>();
    private float _closePointsMaxDist = 0.5f;

    private float _calibrationStartTime = -1.0f;
    private float _calibrationSeconds = 3.0f;
    private float _remainingCalibrationTime;

    int _nLost = 0;
    int _maxSubsequentLosses = 30;

    public Action<AimEventArgs> AimCalibratedEvent;
    [SerializeField] private AimSmoother _aimSmoother;

    private void Start() {
        _leftAnchorImg.gameObject.SetActive(false);
        _rightAnchorImg.gameObject.SetActive(false);
        _topAnchorImg.gameObject.SetActive(false);
        _downAnchorImg.gameObject.SetActive(false);
        _state = CalibrationState.OFF;

        _aimSmoother.AimEvent += OnPoseChange;

        _remainingCalibrationTime = _calibrationSeconds;
        NextState();
    }

    private void NextState() {
        if (_curActiveAnchorImg != null) {
            _curActiveAnchorImg.gameObject.SetActive(false);
        }

        if (_state == CalibrationState.LEFT) {
            // go to right
            _state = CalibrationState.RIGHT;

            // render appropriate arrow
            _rightAnchorImg.gameObject.SetActive(true);
            _curActiveAnchorImg = _rightAnchorImg;

            // start gathering info

        } else if (_state == CalibrationState.RIGHT) {
            // go to down
            _state = CalibrationState.DOWN;

            // render appropriate arrow
            _downAnchorImg.gameObject.SetActive(true);
            _curActiveAnchorImg = _downAnchorImg;

            // start gathering info

        } else if (_state == CalibrationState.DOWN) {
            // go to top
            _state = CalibrationState.TOP;

            // render appropriate arrow
            _topAnchorImg.gameObject.SetActive(true);
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
            _leftAnchorImg.gameObject.SetActive(true);
            _curActiveAnchorImg = _leftAnchorImg;

            // start gathering info
        }
    }

    private float GetScaleFactor(List<Vector2> vectors, float target, bool useX) {
        List<float> scale_factors = new List<float>();

        foreach (Vector2 vec in vectors) {
            float scale;
            float value = useX ? vec.x : vec.y;
            if (value != 0) {
                scale = target / value;
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
            _min_x = GetAverage(_detectedValues, true);
        } else if (_state == CalibrationState.RIGHT) {
            _max_x = GetAverage(_detectedValues, true);
        } else if (_state == CalibrationState.DOWN) {
            _min_y = GetAverage(_detectedValues, false);
        } else if (_state == CalibrationState.TOP) {
            _max_y = GetAverage(_detectedValues, false);
        }

    }

    private void RecordValue(Vector2 aiming_point) {
        _detectedValues.Add(aiming_point);
    }

    private void OnPoseChange(AimEventArgs e) {
        Vector2 newAimingPoint = new Vector2(0, 0);
        if (Calibrating) {
            CheckStartCalibrationStep();

            bool skip_this = false;
            if (!e.HasAimingPoint || !(_inputManager.GetHandInput(HandInputType.GUN_SHOOTING) || _inputManager.GetHandInput(HandInputType.GUN_LOADED))) {
                skip_this = true;
                _nLost += 1;
            } else {
                _nLost = 0;
            }

            if (!skip_this) {

                // gather data
                newAimingPoint = e.AimingPoint;

                if (_detectedValues.Count == 0) {
                    // select first starting point as _startPos
                    _startPos = newAimingPoint;
                }

                RecordValue(newAimingPoint);

                if ((newAimingPoint - _startPos).magnitude > _closePointsMaxDist) {
                    // hand moved too much, reset this step
                    ResetCaibrationStep(newAimingPoint);

                } else {
                    // check if data gathering needs to end
                    _remainingCalibrationTime = _calibrationSeconds - (Time.time - _calibrationStartTime);

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
                float fillAmount = 0.5f + (_remainingCalibrationTime / _calibrationSeconds)/2;
                _curActiveAnchorImg.fillAmount = fillAmount;

                // var text = _curActiveAnchorImg.GetComponentInChildren<TMP_Text>();
                // text.text = ((int) _remainingCalibrationTime).ToString();
            }

        } else {
            if (e.HasAimingPoint) {
                newAimingPoint = GetCalibratedAimingPoint(e.AimingPoint);
            }
        }

        if (AimCalibratedEvent != null) {
            AimCalibratedEvent(new AimEventArgs(newAimingPoint));
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

    private static float GetAverage(List<Vector2> vectors, bool useX) {
        float sum = 0;
        foreach (var vec in vectors) {
            sum += useX ? vec.x : vec.y;
        }

        return sum / vectors.Count;
    }

    private Vector2 GetCalibratedAimingPoint(Vector2 aimingPoint) {

        // remap aiming point to [-0.5f, 0.5f]
        float x_slope = 1.0f / (_max_x - _min_x);
        float y_slope = 1.0f / (_max_y - _min_y);
        float clamped_x = 0.0f;
        float clamped_y = 0.0f;
        try {
            clamped_x = Math.Clamp(aimingPoint.x, _min_x, _max_x);
            clamped_y = Math.Clamp(aimingPoint.y, _min_y, _max_y);
        } catch (ArgumentException e) {
            // -.-

            Debug.LogError(e);
        }
        float new_x = (clamped_x - _min_x) * x_slope -0.5f;
        float new_y = (clamped_y - _min_y) * y_slope - 0.5f;

        aimingPoint = new Vector2(new_x, new_y);


        return aimingPoint;
    }

}
