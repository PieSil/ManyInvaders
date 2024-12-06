using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ShootingSystem : MonoBehaviour {
    [SerializeField] private HandInputManager _handInputManager;
    [SerializeField] private GameObject _projectilePrefab;
    private bool _reloading = false;
    private int _projectileCount = 0;
    private List<Projectile> _spawnedProjectiles = new List<Projectile>();

    public Action<SpawnedProjectileEventArgs> SpawnedProjectileEvent;
    public Action<EventArgs> EnemyKilledEvent;

    private void Start() {
        Projectile projectileComponent = _projectilePrefab.GetComponent<Projectile>();
        if (projectileComponent == null) {
            throw new Exception("Projectile prefab has no Projectile component");
        }
    }

    private void Update() {
        if (!SystemState.GetInstance().IsPaused) {
            if (!_reloading && _handInputManager.GetHandInputDown(HandInputType.GUN_SHOOTING)) {
                var pointerPos = _handInputManager.GetPointerPos3D();
                var spawnPos = pointerPos;
                spawnPos.z += 10.0f;
                var spawnedObject = Instantiate(_projectilePrefab, spawnPos, Quaternion.LookRotation(ComputeShootingDirection(pointerPos)));
                Projectile spawnedProjectile = spawnedObject.GetComponent<Projectile>();
                _spawnedProjectiles.Add(spawnedProjectile);
                spawnedProjectile.EnemyKilledEvent += OnEnemyKilled;
                if (SpawnedProjectileEvent != null) { 
                    SpawnedProjectileEvent(new SpawnedProjectileEventArgs(spawnedProjectile));
                }
                _reloading = true;
            } else if (_reloading && !_handInputManager.GetHandInput(HandInputType.GUN_SHOOTING)){
                _reloading = false;

            }
        }
    }

    private Vector3 ComputeShootingDirection(Vector3 pointerPos) {
        return pointerPos - Camera.main.transform.position;
    }

    private void OnEnemyKilled(Projectile proj) {

        int index = _spawnedProjectiles.FindIndex(element => element == proj);
        if (index < 0) {
            index = _spawnedProjectiles.FindIndex(element => element == null);
        }

        if (index > -1) {
            _spawnedProjectiles.RemoveAt(index);
        }

        if (EnemyKilledEvent != null) {
            EnemyKilledEvent(new EventArgs());
        }
    }

    public void ClearProjectiles() {
        foreach (Projectile proj in _spawnedProjectiles) {
            if (proj != null) {
                Destroy(proj.gameObject);
            }
        }

        _spawnedProjectiles.Clear();
    }

}

public class SpawnedProjectileEventArgs : EventArgs {
    private Projectile _projectile;
    public Projectile Proj => _projectile;

    public SpawnedProjectileEventArgs(Projectile projectile) {
        _projectile = projectile;
    }
}