using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

public class SystemState {
    private static SystemState _instance;
    private bool _paused = false;
    private bool _mouseEnabled = true;
    public bool IsPaused => _paused;
    public bool IsMouseEnabled => _mouseEnabled;

    private SystemState() { }

    public static SystemState GetInstance() {
        if (_instance == null) {
            _instance = new SystemState();
        }

        return _instance;
    }

    public void Pause() {
        _paused = true;
    }

    public void Resume() {
        _paused = false;
    }

    public void SetMouseEnabled(bool enabled) {
        _mouseEnabled = enabled;
    }

}
