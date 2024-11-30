using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UIElements;

public class LevelManager : MonoBehaviour {
    [SerializeField] private RectTransform _referenceScreenTransform;
    [SerializeField] private ShootingSystem _shootingSystem;
    [SerializeField] private float _startDepth = 400.0f;
    [SerializeField] private float _endDepth = 100.0f;
    [SerializeField][Min(0.0000001f)] private float _spawnPeriodSeconds = 20.0f;
    [SerializeField] private int _nSteps = 3;
    [SerializeField] private GameObject _enemyPlanePrefab;
    [SerializeField] private List<EnemyPlane> _spawnedEnemyPlanes;
    [SerializeField] private List<SpawnGrid> _patterns = new List<SpawnGrid>();
    [SerializeField] private int _maxPlayerLives = 3;
    private bool _levelOngoing = false;
    private float _spawnTimer = 0;
    private int _curScore = 0;
    private int _playerLives = 0;
    private float _speed;
    private float _moveStep;
    private int _curPattern = 0;

    // Start is called before the first frame update
    void Start() {
        Canvas _prefabCanvas = _enemyPlanePrefab.GetComponent<Canvas>();
        EnemyPlane _enemyPlane = _enemyPlanePrefab.GetComponent<EnemyPlane>();
        RectTransform _rectTransform = _enemyPlanePrefab.GetComponent<RectTransform>();

        if (_prefabCanvas == null) {
            string msg = "Prefab has no Canvas component";
            Debug.LogError(msg);
            throw new System.Exception(msg);
        }

        if (_enemyPlane == null) {
            string msg = "Prefab has no EnemyPlane component";
            Debug.LogError(msg);
            throw new System.Exception(msg);
        }

        if (_rectTransform == null) {
            string msg = "Prefab has no RectTransform component";
            Debug.LogError(msg);
            throw new System.Exception(msg);
        }

        _moveStep = Mathf.Abs((_endDepth - _startDepth) / (float)_nSteps);
        _speed = _moveStep / _spawnPeriodSeconds;

    }

    private void InstanceEnemyPlane() {
        GameObject _enemyPlaneObject = Instantiate(_enemyPlanePrefab);
        Canvas enemyPlaneCanvas = _enemyPlaneObject.GetComponent<Canvas>();
        enemyPlaneCanvas.renderMode = RenderMode.WorldSpace;
        enemyPlaneCanvas.worldCamera = Camera.main;

        RectTransform enemyPlaneTransform = _enemyPlaneObject.GetComponent<RectTransform>();

        // Copy RectTransform properties from the ReferenceTransform
        /*
        enemyPlaneTransform.anchorMin = _referenceScreenTransform.anchorMin;
        enemyPlaneTransform.anchorMax = _referenceScreenTransform.anchorMax;
        enemyPlaneTransform.anchoredPosition = _referenceScreenTransform.anchoredPosition;
        enemyPlaneTransform.sizeDelta = _referenceScreenTransform.sizeDelta;
        enemyPlaneTransform.pivot = _referenceScreenTransform.pivot;
        enemyPlaneTransform.localScale = _referenceScreenTransform.localScale;
        enemyPlaneTransform.position = _referenceScreenTransform.position;
        */

        // now edit them as needed
        // enemyPlaneTransform.position = new Vector3(enemyPlaneTransform.position.x, enemyPlaneTransform.position.y, _startDepth);

        float frustumPlaneDepth = 100.0f;
        float height = 2.0f * frustumPlaneDepth * Mathf.Tan(Camera.main.fieldOfView * 0.5f * Mathf.Deg2Rad);
        float width = height * Camera.main.aspect;
        enemyPlaneTransform.sizeDelta = new Vector2(width, height);

        enemyPlaneTransform.position = Camera.main.transform.position + Camera.main.transform.forward * _startDepth;


        EnemyPlane enemyPlane = _enemyPlaneObject.GetComponent<EnemyPlane>();
        enemyPlane.EnemyPlaneDestoyed += OnPlaneDestroyed;
        _spawnedEnemyPlanes.Add(enemyPlane);
        enemyPlane.Init(_patterns[_curPattern]);
        _curPattern = (_curPattern + 1) % _patterns.Count;
        enemyPlane.SpawnAll();
        enemyPlane.AssignAlpha(GetDepthAlpha(enemyPlane.transform.position.z));
    }

    private void OnPlaneDestroyed(EnemyPlaneDestroyedEventArgs args) {
        int index = _spawnedEnemyPlanes.FindIndex(element => element == args.Plane);

        if (index == 0) {
            // was on top of stack, do something...
        }

        _spawnedEnemyPlanes.RemoveAt(index);

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
            }

            _spawnTimer += Time.deltaTime;
            if (_spawnTimer > _spawnPeriodSeconds) {
                _spawnTimer = 0;
                InstanceEnemyPlane();
            }
        }

    }

    public void Clear() {

        _levelOngoing = false;

        foreach (var enemyPlane in _spawnedEnemyPlanes) {
            DespawnEnemyPlane(enemyPlane);
        }

        _spawnedEnemyPlanes.Clear();
        _curPattern = 0;
        _playerLives = _maxPlayerLives;
        _curScore = 0;
        _spawnTimer = _spawnPeriodSeconds;
    }

    public void StartLevel() {
        Clear();
        _levelOngoing = true;
    }

    private void DespawnEnemyPlane(EnemyPlane enemyPlane) {
        Destroy(enemyPlane.gameObject);
    }
}
