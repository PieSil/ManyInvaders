using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

public class SystemState {
    private static SystemState _instance;
    private bool _paused = false;
    public bool IsPaused => _paused;

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
}
