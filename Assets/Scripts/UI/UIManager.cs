using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class UIManager : MonoBehaviour {
    [SerializeField] private RectTransform _top;
    [SerializeField] private RectTransform _middle;
    [SerializeField] private RectTransform _middleLeft;
    [SerializeField] private RectTransform _left;
    [SerializeField] private RectTransform _middleRight;
    [SerializeField] private RectTransform _right;
    [SerializeField] private RectTransform _bottom;

    [SerializeField] private List<UIElementTuple> _uiElementsList = new List<UIElementTuple>();
    private Dictionary<UIElement, GameObject> _uiElements = new Dictionary<UIElement, GameObject>();

    private void Start() {
        foreach (var element in _uiElementsList) {
            if (element.Item2 != null) {
                if (!_uiElements.ContainsKey(element.Item1)) {
                    _uiElements.Add(element.Item1, element.Item2);
                }
                element.Item2.SetActive(false);
            }
        }
    }

    private void ActivateUIElement(UIElement elementName) {
        if (_uiElements.ContainsKey(elementName)) {
            _uiElements[elementName].SetActive(true);
        } else {
            Debug.LogError($"No {elementName} found in UI elements");
        }
    }

    private void ActivateUIElementWithPosition(UIElement elementName, RectTransform _pos) {
        if (_uiElements.ContainsKey(elementName)) {
            var element = _uiElements[elementName];
            element.SetActive(true);

            RectTransform elementTransform = element.GetComponent<RectTransform>();
            if (elementTransform is not null) {
                elementTransform.localPosition = _pos.localPosition;
            }

        } else {
            Debug.LogError($"No {elementName} found in UI elements");
        }
    }

    private void DisableUIElement(UIElement element) {
        if (_uiElements.ContainsKey(element)) {
            _uiElements[element].SetActive(false);
        } else {
            Debug.LogError($"No {element} found in UI elements");
        }
    }

    public void DisableAllUIElements() {
        foreach (var element in _uiElements.Values) {
            if (element != null && element.activeSelf) {
                element.SetActive(false);
            }
        }
    }

    public void DrawMainMenu(int score = -1) {
        DisableAllUIElements();

        ActivateUIElement(UIElement.HIGH_SCORE_TEXT);
        ActivateUIElementWithPosition(UIElement.PLAY_BUTTON, _top);
        ActivateUIElementWithPosition(UIElement.CALIBRATE_BUTTON, _middleLeft);
        ActivateUIElementWithPosition(UIElement.CONTROLS_BUTTON, _middleRight);
        ActivateUIElementWithPosition(UIElement.QUIT_BUTTON, _bottom);

        if (_uiElements.ContainsKey(UIElement.HIGH_SCORE_TEXT)) {
            var highScoreObj = _uiElements[UIElement.HIGH_SCORE_TEXT];
            var highScoreText = highScoreObj.GetComponent<TMP_Text>();
            highScoreText.text = $"High Score: {(score > 0 ? score.ToString() : "--")}";
        }
    }

    public void DrawPauseMenu() {
        DisableAllUIElements();

        ActivateUIElementWithPosition(UIElement.RESUME_BUTTON, _top);
        ActivateUIElementWithPosition(UIElement.MAIN_MENU_BUTTON, _middleLeft);
        ActivateUIElementWithPosition(UIElement.CALIBRATE_BUTTON, _middleRight);
        ActivateUIElementWithPosition(UIElement.QUIT_BUTTON, _bottom);
    }

    public void DrawControls() {
        DisableAllUIElements();

    }

    public void DrawGameUI() {
        DisableAllUIElements();

        ActivateUIElement(UIElement.SCORE_TEXT);
        ActivateUIElement(UIElement.GAME_UI);
    }

    public void DrawGameOver() {
        DisableAllUIElements();

        ActivateUIElement(UIElement.GAME_OVER);
        ActivateUIElementWithPosition(UIElement.MAIN_MENU_BUTTON, _middle);
        ActivateUIElementWithPosition(UIElement.QUIT_BUTTON, _bottom);
    }

}

[Serializable]
public enum UIElement {
    NONE,
    HIGH_SCORE_TEXT,
    SCORE_TEXT,
    PLAY_BUTTON,
    RESUME_BUTTON,
    CALIBRATE_BUTTON,
    QUIT_BUTTON,
    MAIN_MENU_BUTTON,
    CONTROLS_BUTTON,
    CONTROLS_UI,
    OK_CONTROLS,
    GAME_UI,
    GAME_OVER,
    INTRO_TEXT,
}

[Serializable]
public struct UIElementTuple {
    [SerializeField] private UIElement _elementName;
    [SerializeField] private GameObject _gameObject;
    public UIElement Item1 => _elementName;
    public GameObject Item2 => _gameObject;

    public UIElementTuple(UIElement elementName = UIElement.NONE, GameObject gameObject = null) {
        _elementName = elementName;
        _gameObject = gameObject;
    }
}
