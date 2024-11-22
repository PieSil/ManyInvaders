using System;
using System.Collections;
using System.Collections.Generic;
using System.Drawing;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.UIElements;

public class EnemyPlane : MonoBehaviour {

    [SerializeField] private SpawnGrid _pattern = new SpawnGrid(4, 4);
    [SerializeField][Range(0, 0.99999f)] private float _left_margin = 0;
    [SerializeField][Range(0, 0.99999f)] private float _right_margin = 0;
    [SerializeField][Range(0, 0.99999f)] private float _top_margin = 0;
    [SerializeField][Range(0, 0.99999f)] private float _bottom_margin = 0;
    [SerializeField] GameObject _invaderPrefab;
    [SerializeField] float _forwardStep = -10;
    [SerializeField] public RectTransform PlaneCanvasRectTransform;
    private InvaderSlot _referenceSlot;
    // private RectTransform _screenCanvasRectTransform;
    private Dictionary<ValueTuple<int, int>, Invader> _spawnedInvaders = new Dictionary<ValueTuple<int, int>, Invader>();
    private float _x_offset;
    private float _y_offset;

    public Action<EnemyPlaneDestroyedEventArgs> EnemyPlaneDestoyed;

    private void Start() {
        Init();
    }

    // Start is called before the first frame update
    private void Init() {

        ClearInvaders();

        if (PlaneCanvasRectTransform == null) {
            Debug.LogError("No plane canvas was set");
            throw new Exception("No plane canvas was set");
        }

        if (_invaderPrefab == null) {
            Debug.LogError("_invaderPrefab not set");
            throw new Exception("_invaderPrefabNotSet");
        }

        RectTransform _invaderTransform = _invaderPrefab.GetComponent<RectTransform>();
        if (_invaderTransform == null) {
            Debug.LogError("_invaderPrefab has no RectTransform component");
            throw new Exception("_invaderPrefab has no RectTransform component");
        }

        if (GetScreenWidth() <= 0) {
            Debug.LogError("Right and left margin exceed screen bounds");
            throw new Exception("Right and left margin exceed screen bounds");
        }

        if (GetScreenHeight() <= 0) {
            Debug.LogError("Top and bottom margin exceed screen bounds");
            throw new Exception("Top and bottom margin exceed screen bounds");
        }

        _referenceSlot = new InvaderSlot(_invaderTransform);
        _referenceSlot.InitScale(GetScreenWidth(), GetScreenHeight(), _pattern, 0, 0);

        _x_offset = Mathf.Max(GetScreenWidth() - _referenceSlot.Width * _pattern.Cols, 0) / (_pattern.Cols + 1);
        _y_offset = Mathf.Max(GetScreenHeight() - _referenceSlot.Height * _pattern.Rows, 0) / (_pattern.Rows + 1);

        SpawnAll();

    }

    public void SpawnAll() {
        Vector3[] corners = new Vector3[4];
        PlaneCanvasRectTransform.GetWorldCorners(corners);
        var topLeft = corners[1];
        topLeft.x /= PlaneCanvasRectTransform.localScale.x;
        topLeft.y /= PlaneCanvasRectTransform.localScale.y;
        Debug.Log($"bottom left: {topLeft}");

        foreach (ValueTuple<int, int> gridIndices in _pattern) {
            var pos = topLeft;
            // gridIndices[0] == col index
            // gridIndices[1] == row index
            pos.x += Mathf.Floor((GetScreenLeftOffset() + ((float)gridIndices.Item2 + 1) * (_x_offset) + (_referenceSlot.Width / 2.0f) + (float)gridIndices.Item2 * _referenceSlot.Width));
            pos.y -= Mathf.Floor((GetScreenTopOffset() + ((float)gridIndices.Item1 + 1) * (_y_offset) + (_referenceSlot.Height / 2.0f) + (float)gridIndices.Item1 * _referenceSlot.Height));
            //float shift_x = gridIndices.x % 2 == 0 ? _referenceSlot.InvaderWidth/4 : -_referenceSlot.InvaderWidth / 4;
            //pos.x += Mathf.Floor(shift_x);
            // float shift_y = gridIndices.y % 2 == 0 ? -_referenceSlot.Height / 4 : +_referenceSlot.Height / 4;
            // pos.y += Mathf.Floor(shift_y);
            var spawned = Instantiate(_invaderPrefab, pos, Quaternion.identity);
            Invader invader = spawned.GetComponent<Invader>();
            if (invader == null) {
                Debug.LogError("Spawned enemy has no Invader script");
                throw new Exception("Spawned enemy has no Invader script");
            }
            invader.SetGridIndices(gridIndices);
            invader.InvaderDestroyed += OnInvaderDestroyed;

            RectTransform invaderTransform = spawned.GetComponent<RectTransform>();

            invaderTransform.localScale = new Vector3(_referenceSlot.Scale, _referenceSlot.Scale, 1);
            spawned.transform.SetParent(PlaneCanvasRectTransform.gameObject.transform, false);
            _spawnedInvaders.Add(gridIndices, invader);
        }
    }

    private void OnInvaderDestroyed(InvaderDestroyedEventArgs args) {
        if (_spawnedInvaders.ContainsKey(args.GridIndices)) {
            _spawnedInvaders.Remove(args.GridIndices);
        } else {
            Debug.LogWarning($"Received InvaderDestoyed event with GridIndices: [{args.GridIndices.Item1}, {args.GridIndices.Item2}] but no such object was found in _spawnedObjects");
        }

        if (_spawnedInvaders.Count <= 0) {
            Destroy(gameObject);
        }
    }

    private void ClearInvaders() {
        foreach (var gridIndices in _spawnedInvaders.Keys) {
            var invader = _spawnedInvaders[gridIndices];
            invader.InvaderDestroyed -= OnInvaderDestroyed;
            Destroy(invader.gameObject);
            _spawnedInvaders.Remove(gridIndices);
        }
    }

    private void OnDestroy() {
        ClearInvaders();

        if (EnemyPlaneDestoyed != null) {
            EnemyPlaneDestoyed(new EnemyPlaneDestroyedEventArgs(this));
        }
    }

    private float GetScreenLeftOffset() {
        return PlaneCanvasRectTransform.rect.width * _left_margin;
    }

    private float GetScreenTopOffset() {
        return PlaneCanvasRectTransform.rect.height * _top_margin;
    }

    private float GetScreenRightOffsetFromLeft() {
        float right_offset = PlaneCanvasRectTransform.rect.width * _right_margin;
        return GetScreenLeftOffset() + (PlaneCanvasRectTransform.rect.width - right_offset);
    }

    private float GetScreenBottomOffsetFromTop() {
        float bottom_offset = PlaneCanvasRectTransform.rect.height * _bottom_margin;
        return GetScreenTopOffset() + (PlaneCanvasRectTransform.rect.width - _bottom_margin);
    }

    private float GetScreenWidth() {
        float left_offset = PlaneCanvasRectTransform.rect.width * _left_margin;
        float right_offset = PlaneCanvasRectTransform.rect.width * _right_margin;
        return PlaneCanvasRectTransform.rect.width - (left_offset + right_offset);
    }

    private float GetScreenHeight() {
        float top_offset = PlaneCanvasRectTransform.rect.height * _top_margin;
        float bottom_offset = PlaneCanvasRectTransform.rect.height * _bottom_margin;
        return PlaneCanvasRectTransform.rect.height - (top_offset + bottom_offset);
    }

    private void ResizeInvaderSlot() {

    }
}

public class InvaderSlot {

    private float _scale = 1.0f;
    private RectTransform _invaderTransform;
    public float UnscaledWidth => 1.5f * UnscaledInvaderWidth;
    public float UnscaledHeight => 1.5f * UnscaledInvaderHeight;
    public float Width => 1.5f * UnscaledInvaderWidth * _scale;
    public float Height => 1.5f * UnscaledInvaderHeight * _scale;
    public float UnscaledInvaderWidth => _invaderTransform.rect.width;
    public float UnscaledInvaderHeight => _invaderTransform.rect.height;
    public float InvaderWidth => _invaderTransform.rect.width * _scale;
    public float InvaderHeight => _invaderTransform.rect.height * _scale;
    public float Scale => _scale;


    public InvaderSlot(RectTransform invaderTransform) {
        _invaderTransform = invaderTransform;
    }

    public void InitScale(float screenWidth, float screenHeight, SpawnGrid spawnGrid, float minOffsetX = 0, float minOffsetY = 0) {
        int Nx = spawnGrid.Cols;
        int Ny = spawnGrid.Rows;
        float W = screenWidth - minOffsetX * (Nx + 1);
        float H = screenHeight - minOffsetY * (Ny + 1);

        if (W <= 0) {
            Debug.LogWarning("screen width after considering minimum X offset is <= 0, ignoring requested offset");
            W = screenWidth;
        }

        if (H <= 0) {
            Debug.LogWarning("screen height after considering minimum Y offset is <= 0, ignoring requested offset");
            H = screenHeight;
        }

        if (W / Width < (float)Nx) {
            // find biggest value of width that allows for at least Nx slots
            float minWidth = W / Nx;

            // compute resize factor
            _scale = minWidth / UnscaledWidth;

        }

        if (H / Height < (float)Ny) {
            // find biggest value of height that allows for at least Ny slots
            float minHeight = H / Ny;

            // compute resize factor
            _scale = minHeight / UnscaledHeight;
        }

        Debug.Log($"Scale is {_scale}");

    }

}

[Serializable]
public struct SpawnGrid : IEnumerable<ValueTuple<int, int>> {
    [SerializeField][Min(0)] private int _rows;
    [SerializeField][Min(0)] private int _cols;
    public int Rows => _rows;
    public int Cols => _cols;

    public SpawnGrid(int rows, int cols) {
        _rows = rows;
        _cols = cols;
    }

    public IEnumerator<ValueTuple<int, int>> GetEnumerator() {
        for (int i = 0; i < _rows; i++) {
            for (int j = 0; j < _cols; j++) {
                yield return new ValueTuple<int, int>(i, j);
            }
        }
    }

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
}

public class EnemyPlaneDestroyedEventArgs : EventArgs {
    private EnemyPlane _enemyPlane;

    public EnemyPlane Plane => _enemyPlane;

    public EnemyPlaneDestroyedEventArgs(EnemyPlane enemyPlane) {
        _enemyPlane = enemyPlane;
    }
}
