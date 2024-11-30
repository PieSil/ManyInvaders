using System;
using System.Collections;
using System.Collections.Generic;
using UnityEditor.Experimental.GraphView;
using UnityEngine;

public class GameLogic : MonoBehaviour {

    [SerializeField] private AimCalibrator _calibrator;
    [SerializeField] private UIManager _uiManager;
    [SerializeField] private LevelManager _levelManager;
    [SerializeField] private HandInputManager _handInputManager;
    private Stack<GameState> _stateStack = new Stack<GameState>();
    private int _highScore = 0;

    private void Start() {
        StartCalibration();
    }

    private void Update() {
        if (CurState() == GameState.PLAYING && (Input.GetKeyDown(KeyCode.Escape) || _handInputManager.GetHandInputDown(HandInputType.HAND_OPEN))) {
            PauseGame();
        } else if (CurState() == GameState.PAUSED && Input.GetKeyDown(KeyCode.Escape)) {
            ResumeGame();
        }
    }

    public void PauseGame() {
        if (CurState() != GameState.PAUSED) {
            PushState(GameState.PAUSED);
            OnNewState();
        }
    }

    public void ResumeGame() {
        GameState expectPaused = PopState();
        OnNewState();

        if (expectPaused != GameState.PAUSED) {
            throw new System.Exception($"Expected game state to be PAUSED, got {expectPaused}");
        }
    }

    public void StartGame() {
        Debug.LogWarning("Starting game");
        if (CurState() != GameState.MAIN_MENU) {
            throw new System.Exception($"Expected game state to be MAIN_MENU, got {CurState()}");
        }

        PushState(GameState.PLAYING);
        OnNewState();
        _levelManager.StartLevel();
    }

    public void GoToMainMenu() {

        _levelManager.Clear();

        while (CurState() != GameState.MAIN_MENU || _stateStack.Count == 0) {
            PopState();
        }

        if (_stateStack.Count == 0) {
            PushState(GameState.MAIN_MENU);
        }

        OnNewState();
    }

    public void StartCalibration() {
        PushState(GameState.CALIBRATING);
        OnNewState();
        _calibrator.CalibrationDone += OnCalibrationDone;
        _calibrator.ResetCalibration();
    }

    public void GoBackOneState() {
        PopState();
        OnNewState();
    }

    public void Quit() {
        Application.Quit();
    }

    private void OnCalibrationDone(EventArgs args) {
        _calibrator.CalibrationDone -= OnCalibrationDone;
        PopState();
        if (CurState() == GameState.NONE) {
            // first calibration done, push main menu
            PushState(GameState.MAIN_MENU);
        }

        OnNewState();

    }

    private GameState CurState() {
        if (_stateStack.Count > 0) {
            return _stateStack.Peek();
        } else {
            return GameState.NONE;
        }
    }

    private void PushState(GameState state) {
        _stateStack.Push(state);
    }

    private GameState PopState() {
        if (_stateStack.Count > 0) { 
            return _stateStack.Pop();
        } else {
            return GameState.NONE;
        }
    }

    private void OnNewState() {
        var newState = CurState();

        switch (newState) {
            case GameState.PAUSED:
                SystemState.GetInstance().Pause();
                SystemState.GetInstance().SetMouseEnabled(true);
                _uiManager.DrawPauseMenu();
                break;
            case GameState.PLAYING:
                SystemState.GetInstance().Resume();
                SystemState.GetInstance().SetMouseEnabled(false);
                _uiManager.DrawGameUI();
                break;
            case GameState.MAIN_MENU:
                SystemState.GetInstance().Pause();
                SystemState.GetInstance().SetMouseEnabled(true);
                _uiManager.DrawMainMenu(_highScore);
                break;
            case GameState.CALIBRATING:
                SystemState.GetInstance().Pause();
                SystemState.GetInstance().SetMouseEnabled(false);
                _uiManager.DisableAllUIElements();
                break;
            case GameState.CONTROLS:
                SystemState.GetInstance().Pause();
                SystemState.GetInstance().SetMouseEnabled(true);
                _uiManager.DrawControls();
                break;
            default:
                break;
        }
    }

    private GameState ReplaceState(GameState state) {
        GameState ret = PopState();
        PushState(state);

        return ret;
    }

    private void ClearStates() {
        _stateStack.Clear();
    }
}

public enum GameState {
    NONE, 
    MAIN_MENU,
    PLAYING,
    PAUSED,
    CALIBRATING,
    CONTROLS,
    INTRO
}