using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LifeBar : MonoBehaviour
{
    [SerializeField] LevelManager _levelManager;
    [SerializeField] List<GameObject> _lifeSprites = new List<GameObject>();

    private void Start() {
        _levelManager.PLayerLifeEvent += OnPlayerLifeChange;
    }

    private void OnPlayerLifeChange(int _curLife) {
        Debug.Log("Plyer life changed");
        int i = 0;
        foreach (var lifeObj in _lifeSprites) {
            if (i < _curLife) {
                lifeObj.SetActive(true);
            } else {
                lifeObj.SetActive(false);
            }
            i++;
        }
    }
}
