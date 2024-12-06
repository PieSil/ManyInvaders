using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Projectile : MonoBehaviour
{
    [SerializeField] private float _speed = 5.0f;
    [SerializeField] private float _maxDepth = 500.0f;

    public Action<Projectile> EnemyKilledEvent;

    void Update() {
        if (!SystemState.GetInstance().IsPaused) {
            if (transform.position.z >= _maxDepth) {
                Destroy(gameObject);
            } else {
                transform.position += transform.forward * _speed * Time.deltaTime;
            }
        }
    }

    void OnCollisionEnter(Collision collision) {
        // we expect to only collied with enemies

        Destroy(collision.gameObject);
        Destroy(gameObject);

        if (EnemyKilledEvent != null) {
            EnemyKilledEvent(this);
        }
    }


}
