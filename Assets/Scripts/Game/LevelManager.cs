using System;
using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.SocialPlatforms.Impl;
using UnityEngine.UI;
using UnityEngine.UIElements;

public class LevelManager : MonoBehaviour {
    [SerializeField] private RectTransform _referenceScreenTransform;
    [SerializeField] private ShootingSystem _shootingSystem;
    [SerializeField] private UnityEngine.UI.Image _hurtScreen;
    [SerializeField] private float _startDepth = 400.0f;
    [SerializeField] private float _endDepth = 100.0f;
    [SerializeField][Min(0.0000001f)] private float _baseSpawnPeriodSeconds = 20.0f;
    [SerializeField] private int _nSteps = 3;
    [SerializeField] private GameObject _enemyPlanePrefab;
    private List<EnemyPlane> _spawnedEnemyPlanes = new List<EnemyPlane>();
    [SerializeField] private List<SpawnGrid> _patterns = new List<SpawnGrid>();
    [SerializeField] private List<int> _scoreThresholds = new List<int>();
    [SerializeField] private List<float> _updatedSpawnPeriods = new List<float>();
    private float _spawnPeriodSeconds;
    private int _nextScoreThreshIdx = 0;

    private int _maxPlayerLives = 3;
    private bool _levelOngoing = false;
    private float _spawnTimer = 0;
    private int _curScore = 0;
    private int _playerLives = 0;
    private float _speed;
    private float _moveStep;
    private int _curPattern = 0;

    // Start is called before the first frame update

    public Action<int> NewScoreEvent;
    public Action<int> PLayerLifeEvent;
    public Action GameOverEvent;
    void Start() {
        _spawnPeriodSeconds = _baseSpawnPeriodSeconds;
        Canvas _prefabCanvas = _enemyPlanePrefab.GetComponent<Canvas>();
        EnemyPlane _enemyPlane = _enemyPlanePrefab.GetComponent<EnemyPlane>();
        RectTransform _rectTransform = _enemyPlanePrefab.GetComponent<RectTransform>();

        if (_prefabCanvas == null) {
            string msg = "Prefab has no Canvas component";
            throw new System.Exception(msg);
        }

        if (_enemyPlane == null) {
            string msg = "Prefab has no EnemyPlane component";
            throw new System.Exception(msg);
        }

        if (_rectTransform == null) {
            string msg = "Prefab has no RectTransform component";
            throw new System.Exception(msg);
        }

        if (_hurtScreen == null) {
            string msg = "No HurtScreen (Image) component";
            throw new System.Exception(msg);
        }

        ComputeStepAndSpeed();

        if (_updatedSpawnPeriods.Count < 0) {
            Debug.LogWarning("Speeds list is empty");
            _updatedSpawnPeriods.Add(_baseSpawnPeriodSeconds);
        }

        if (_updatedSpawnPeriods.Count < _scoreThresholds.Count) {
            Debug.LogWarning($"More score thresholds than updated spawn periods, score thresholds in excess won't have any effect");
        }

        while (_updatedSpawnPeriods.Count < _scoreThresholds.Count) {
            _updatedSpawnPeriods.Add(_updatedSpawnPeriods.FindLast(a => true));
        }

        SetNextPatternIndex();
    }

    private void ComputeStepAndSpeed() {
        _moveStep = Mathf.Abs((_endDepth - _startDepth) / (float)_nSteps);
        _speed = _moveStep / _spawnPeriodSeconds;
    }

    private void CheckSpeedUpdate() {
        if (_nextScoreThreshIdx < _scoreThresholds.Count) {
            if (_curScore >= _scoreThresholds[_nextScoreThreshIdx]) {
                _spawnPeriodSeconds = _updatedSpawnPeriods[_nextScoreThreshIdx];
                ComputeStepAndSpeed();
                _nextScoreThreshIdx++;
            }
        }
    }

    private void SetNextPatternIndex() {
        _curPattern = UnityEngine.Random.Range((int)0, (int)_patterns.Count);
    }

    private void InstanceEnemyPlane() {
        GameObject _enemyPlaneObject = Instantiate(_enemyPlanePrefab);
        Canvas enemyPlaneCanvas = _enemyPlaneObject.GetComponent<Canvas>();
        enemyPlaneCanvas.renderMode = RenderMode.WorldSpace;
        enemyPlaneCanvas.worldCamera = Camera.main;

        RectTransform enemyPlaneTransform = _enemyPlaneObject.GetComponent<RectTransform>();

        float frustumPlaneDepth = 100.0f;
        float height = 2.0f * frustumPlaneDepth * Mathf.Tan(Camera.main.fieldOfView * 0.5f * Mathf.Deg2Rad);
        float width = height * Camera.main.aspect;
        enemyPlaneTransform.sizeDelta = new Vector2(width, height);

        enemyPlaneTransform.position = Camera.main.transform.position + Camera.main.transform.forward * _startDepth;


        EnemyPlane enemyPlane = _enemyPlaneObject.GetComponent<EnemyPlane>();
        enemyPlane.EnemyPlaneDestoyed += OnPlaneDestroyed;
        _spawnedEnemyPlanes.Add(enemyPlane);
        enemyPlane.Init(_patterns[_curPattern]);
        SetNextPatternIndex();
        enemyPlane.SpawnAll();
        enemyPlane.AssignAlpha(GetDepthAlpha(enemyPlane.transform.position.z));
    }

    private void OnPlaneDestroyed(EnemyPlaneDestroyedEventArgs args) {
        int index = _spawnedEnemyPlanes.FindIndex(element => element == args.Plane);
        if (index < 0) {
            index = _spawnedEnemyPlanes.FindIndex(element => element == null);
        }

        if (index > -1) {
            _spawnedEnemyPlanes.RemoveAt(index);
        }
        

    }

    private float GetDepthAlpha(float zPos) {
        float t = Mathf.Abs((zPos - _startDepth) / ((_endDepth - _moveStep) - _startDepth));
        return Mathf.Lerp(0.1f, 1.0f, t);
    }

    private void Update() {
        if (_levelOngoing && !SystemState.GetInstance().IsPaused) {
            // Vector3 traslation = new Vector3(0, 0, _speed * Time.deltaTime);
            EnemyPlane toDelete = null;
            foreach (var enemyPlane in _spawnedEnemyPlanes) {
                enemyPlane.transform.position -= enemyPlane.transform.forward * _speed * Time.deltaTime;
                // enemyPlane.transform.Translate(traslation);
                if (Mathf.Abs(enemyPlane.transform.position.z - Camera.main.transform.position.z) <= _endDepth) {
                    toDelete = enemyPlane;
                } else {
                    enemyPlane.AssignAlpha(GetDepthAlpha(enemyPlane.transform.position.z));
                }
            }

            if (toDelete != null) {
                DespawnEnemyPlane(toDelete);
                _playerLives--;
                if (_playerLives < 0) {
                    _playerLives = 0;
                }
                if (PLayerLifeEvent != null) {
                    PLayerLifeEvent(_playerLives);
                }
                AudioSource audioSource = GetComponent<AudioSource>();
                if (audioSource != null) {
                    audioSource.Play();
                }
                ShowHurtScreen();
            }

            _spawnTimer += Time.deltaTime;
            if (_spawnTimer > _spawnPeriodSeconds) {
                _spawnTimer = 0;
                InstanceEnemyPlane();
            }

            if (_playerLives <= 0 && GameOverEvent != null) {
                GameOverEvent();
            }
        }

    }

    public void Clear() {

        _shootingSystem.ClearProjectiles();
        _shootingSystem.EnemyKilledEvent -= OnEnemyKiled;
        _levelOngoing = false;

        foreach (var enemyPlane in _spawnedEnemyPlanes) {
            DespawnEnemyPlane(enemyPlane);
        }

        _nextScoreThreshIdx = 0;
        _spawnPeriodSeconds = _baseSpawnPeriodSeconds;
        _spawnedEnemyPlanes.Clear();
        _curPattern = 0;
        _playerLives = _maxPlayerLives;
        _curScore = 0;
        _spawnTimer = _spawnPeriodSeconds;
        if (PLayerLifeEvent != null) {
            PLayerLifeEvent(_playerLives);
        }

        ComputeStepAndSpeed();
    }

    private void OnEnemyKiled(EventArgs args) {
        _curScore++;
        if (NewScoreEvent != null ) {
            NewScoreEvent(_curScore);
        }
        CheckSpeedUpdate();
    }

    public void StartLevel() {
        Clear();
        SetNextPatternIndex();

        _shootingSystem.EnemyKilledEvent += OnEnemyKiled;
        _levelOngoing = true;

        if (NewScoreEvent != null) {
            NewScoreEvent(_curScore);
        }

        if (PLayerLifeEvent != null) {
            PLayerLifeEvent(_playerLives);
        }
    }

    private void ShowHurtScreen() {
        var color = _hurtScreen.color;
        color.a = 0.2f;
        _hurtScreen.color = color;
        StartCoroutine(ClearHurtScreen());
    }

    private IEnumerator ClearHurtScreen() {
       
        var color = _hurtScreen.color;

        do {
            yield return new WaitForSeconds(0.1f);
            color = _hurtScreen.color;
            color.a -= 0.05f;
            if (color.a < 0) {
                color.a = 0;
            }
            _hurtScreen.color = color;
        } while (color.a > 0);
    }

    private void DespawnEnemyPlane(EnemyPlane enemyPlane) {
        Destroy(enemyPlane.gameObject);
    }
}
