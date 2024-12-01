using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class ScoreUpdater : MonoBehaviour
{

    [SerializeField] private LevelManager _levelManager;
    private TMP_Text _tmpro_text;
    // Start is called before the first frame update
    void Start() {
        _tmpro_text = GetComponent<TMP_Text>();
        if (_tmpro_text == null) {
            throw new System.Exception("No TextMeshPro Text component");
        }

        _levelManager.NewScoreEvent += OnScoreEvent;

        _tmpro_text.text = $"Score: --";
    }

    private void OnScoreEvent(int score) {
        _tmpro_text.text = $"Score: {score}";
    }
}
