using JetBrains.Annotations;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.Tracing;
using System.Linq;
using TMPro;
using UnityEngine;

public enum HandInputType {
    NONE,
    HAND_LOST,
    HAND_OPEN,
    GUN_LOADED,
    GUN_SHOOTING
}

public class HandInputManager : MonoBehaviour
{
    /* Hand pose detection from 3D info is a bit rough, thus we need to smooth them though this class in order to collect the correct inputs*/

    public struct InputState {
        public InputState ToDefault() {
            _held = false;
            _just_pressed = false;
            _read = false;

            return this;
        }

        public InputState Pressed(HandInputType debug) {

            /* Set flags accordingly when an input is "pressed" 
             * If was not being held than it means it was just pressed
             * Otherwise it means that it was already being pressed
             */

            // Debug.Log($"Setting pressed for: {debug}");

            _just_pressed = !_held;
            _held = true;
            _just_released = false;
            _read = false;

            return this;
        }

        public InputState Released(HandInputType debug) {

            // Debug.Log($"Setting released for: {debug}");

            _just_released = _held;
            _held = false;
            _just_pressed = false;
            _read = false;

            return this;
        }

        public bool ReadJustPressed() {

            _read = true;
            return _just_pressed;
        }

        public bool ReadJustReleased() {

            _read = true;
            return _just_released;
        }

        public bool ReadHeld(HandInputType debug) {

            // Debug.Log($"Reading {debug}, held: {_held}");

            _read = true;
            return _held;
        }

        public InputState AtEndOfFrame() {

            /* To be called at the end of every frame */
            _read = false;
            _just_pressed = false;
            _just_released = false;

            return this;
        }

        private bool _held;
        public bool Held => _held;
        private bool _just_pressed;
        private bool _just_released;
        private bool _read;
    }

    private struct NextInputWrapper {
        /* Wraps a HandInpuType with a bool in order to check if an input change has been scheduled or not and for which input*/

        public bool Is(HandInputType query) {
            // checks if a change to the given query input has been scheduled

            return _changeScheduled && _handInputType == query;
        }

        public void Set(HandInputType nextInput) {
            _changeScheduled = true;
            _handInputType = nextInput;
        }

        public void Reset() {
            _changeScheduled = false;
            _handInputType = HandInputType.HAND_LOST; //just a placeholder
        }

        private bool _changeScheduled;
        private HandInputType _handInputType;
    }

    [SerializeField] HandPoseDetection _poseDetector;
    [SerializeField] AimCalibrator _calibrator;
    [SerializeField] RectTransform _pointer;
    [SerializeField] RectTransform _canvasRect;
    [SerializeField] UIManager _uIManager;
    private Dictionary<HandInputType, InputState> _inputMap = new Dictionary<HandInputType, InputState>();
    private bool _pointerEnabled = true;

    private bool _mouseIdle = true;
    private float _mouseIdleThresh = 0.5f;
    private float _mouseIdleTimer = 0.0f;
    private Vector3 _lastMousePosition;
    private NextInputWrapper _nextInput; // contains the input that will be set when associated timer expires
    private Coroutine _delayedInputChangeCoroutine; // holds reference to coroutine that will change the input when timer expires
    private float _toOpenHandSecondsDelay = 0.5f;
    private float _toNoneSecondsDelay = 1.0f;
    private float _toLostSecondsDelay = 3.0f;

    private void Awake() {
        ResetInputs();
        _nextInput.Reset();
        _inputMap[HandInputType.HAND_LOST].Pressed(HandInputType.HAND_LOST);

        _poseDetector.PoseEvent += OnPoseChange;
        _calibrator.AimCalibratedEvent += OnAimChange;

        if (_canvasRect == null ) {
            Debug.LogError($"No CanvasRect set");
        }

        StartCoroutine(NextFrameUpdate());
    }

    void Update() {
        if (_pointerEnabled) {
            if (SystemState.GetInstance().IsMouseEnabled) {
                var mousePos = Input.mousePosition;
                if (mousePos != _lastMousePosition) {
                    _mouseIdle = false;
                    _lastMousePosition = mousePos;
                    _mouseIdleTimer = 0.0f;

                    _pointer.position = mousePos;
                } else if (!_mouseIdle) {
                    _mouseIdleTimer += Time.deltaTime;

                    if (_mouseIdleTimer >= _mouseIdleThresh) {
                        _mouseIdle = true;
                    }
                }
            } else {

                if (!_mouseIdle) {
                    _mouseIdleTimer = 0.0f;
                    _mouseIdle = true;
                }
            }
        }
    }

    private void OnPoseChange(PoseEventArgs e) {
        /* For simplcity only one hand input is allowed for each frame, with priority for harder to detect gestures */

        // for gun pose we set the input as soon as we detect the pose, as these poses might be tricky to detect
        // for other kind of inputs we start a timer and only set the input after a given amount of time to be sure that it actually changed
        // if another kind of pose is detected whiel a timer is active we deactivate the timer and act based on the new pose (start another timer or immediately set the input)

        if (e.Poses.gun) {
            if (_inputMap[HandInputType.GUN_LOADED].Held) {
                if (e.Poses.Shooting && _pointerEnabled) {

                    PressedInput(HandInputType.GUN_SHOOTING);
                }
            }
            // Debug.Log("Gun");
             else if (e.Poses.Loaded){
                PressedInput(HandInputType.GUN_LOADED);
            } else {
                ScheduleInput(HandInputType.NONE, _toNoneSecondsDelay);
            }
        } else if (e.Poses.open_hand) {
            // Debug.Log("Open hand");
            // ScheduleInput(HandInputType.HAND_OPEN, _toOpenHandSecondsDelay);
            ScheduleInput(HandInputType.HAND_OPEN, _toOpenHandSecondsDelay);
        } else if (e.Poses.lost_hand) {
            // Debug.Log("Lost hand");
            ScheduleInput(HandInputType.HAND_LOST, _toLostSecondsDelay);
        } else {
            // Debug.Log("No pose");
            ScheduleInput(HandInputType.NONE, _toNoneSecondsDelay);
        }
        
    }

    private void OnAimChange(AimEventArgs e) {
        if (e.HasAimingPoint && _mouseIdle && _pointerEnabled) {
            if (_canvasRect) {
                Vector2 new_pos = new Vector2(e.AimingPoint.x * _canvasRect.rect.width, e.AimingPoint.y * _canvasRect.rect.height/*canvas_rect.sizeDelta.y*/);
                _pointer.anchoredPosition = new_pos;
            }
        }
    }

    private void ScheduleInput(HandInputType target, float delay) {

        // no need to schedule anything if input is already being pressed or was alreadu scheduled
        if (!_inputMap[target].Held && !_nextInput.Is(target)) {
            // stop the timer coroutine and set a new one
            StopDelayedInputTimer();

            _nextInput.Set(target);
            _delayedInputChangeCoroutine = StartCoroutine(DelayedInputChange(target, delay));
        }

        // otherwise it means that input is already scheduled
    }

    private IEnumerator DelayedInputChange(HandInputType target, float delay) {
        yield return new WaitForSeconds(delay);
        if (_nextInput.Is(target)) {
            PressedInput(target);
        }
    }

    private void PressedInput(HandInputType pressedInputType) {
        // stop any delayed input coroutine

        StopDelayedInputTimer();

        foreach (HandInputType inputType in Enum.GetValues(typeof(HandInputType)).Cast<HandInputType>()) {
            if (inputType == pressedInputType) {
                _inputMap[inputType] = _inputMap[inputType].Pressed(inputType);
            } else {
               _inputMap[inputType] = _inputMap[inputType].Released(inputType);
            }
        }

        _uIManager.DrawHandState(pressedInputType);
    }

    private void StopDelayedInputTimer() {
        if (_delayedInputChangeCoroutine != null) {
            StopCoroutine(_delayedInputChangeCoroutine);
            _delayedInputChangeCoroutine = null;
        }

        _nextInput.Reset();
    }

    private void ResetInputs() {
        foreach (HandInputType inputType in Enum.GetValues(typeof(HandInputType)).Cast<HandInputType>()) {
            InputState state = new InputState();
            state.ToDefault();
            _inputMap[inputType] = state;
        }
    }

    private IEnumerator NextFrameUpdate() {
        while (true) {
            yield return null;
            foreach (HandInputType inputType in Enum.GetValues(typeof(HandInputType)).Cast<HandInputType>()) {
                _inputMap[inputType] = _inputMap[inputType].AtEndOfFrame();
            }
        }
    }

    public Vector3 GetPointerPos3D(bool getScreenPoint = false) {
        if (_pointerEnabled) {
            var pos = _pointer.position;
            pos.z += Camera.main.nearClipPlane;
            if (!getScreenPoint) {
                pos = Camera.main.ScreenToWorldPoint(pos);
            }

            return pos;

        } else return Vector3.zero;
    }

    public void EnablePointer() {
        _pointer.gameObject.SetActive(true);
        _pointerEnabled = true;
    }

    public void DisablePointer() {
        _pointerEnabled = false;
        _pointer.gameObject.SetActive(false);
    }

    public bool GetHandInput(HandInputType query) {

        bool result = _inputMap[query].ReadHeld(query);
        // Debug.LogWarning($"Reading held for {query}: {result}");

        return result;
    }

    public bool GetHandInputDown(HandInputType query) {
        return _inputMap[query].ReadJustPressed();
    }

    public bool GetHandInputUp(HandInputType query) {
        return !_inputMap[query].ReadJustReleased();
    }

}
