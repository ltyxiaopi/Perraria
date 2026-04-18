using UnityEngine;
using UnityEngine.InputSystem;

// Temporary debug entry point until the player combat task is implemented.
public sealed class EnemyDebugInput : MonoBehaviour
{
    [SerializeField] private int _damagePerPress = 10;
    [SerializeField] private float _maxRange = 5f;
    [SerializeField] private Transform _player;

    private void Awake()
    {
        if (_player == null)
        {
            GameObject player = GameObject.FindWithTag("Player");
            if (player != null)
            {
                _player = player.transform;
            }
        }
    }

    private void Update()
    {
        if (Keyboard.current == null || !Keyboard.current.kKey.wasPressedThisFrame || _player == null)
        {
            return;
        }

        Enemy nearestEnemy = FindNearestEnemy();
        if (nearestEnemy != null)
        {
            nearestEnemy.TakeDamage(_damagePerPress);
        }
    }

    private Enemy FindNearestEnemy()
    {
        Enemy[] enemies = Object.FindObjectsByType<Enemy>(FindObjectsInactive.Exclude);
        Enemy nearestEnemy = null;
        float bestDistanceSqr = _maxRange * _maxRange;

        foreach (Enemy enemy in enemies)
        {
            if (enemy == null || enemy.IsDead)
            {
                continue;
            }

            float distanceSqr = ((Vector2)(enemy.transform.position - _player.position)).sqrMagnitude;
            if (distanceSqr > bestDistanceSqr)
            {
                continue;
            }

            bestDistanceSqr = distanceSqr;
            nearestEnemy = enemy;
        }

        return nearestEnemy;
    }
}
