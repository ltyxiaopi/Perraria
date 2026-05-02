using System;
using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
public sealed class EnemySpawner : MonoBehaviour
{
    private const int SpawnPointAttempts = 6;
    private const float TileCenterOffset = 0.5f;

    [SerializeField] private EnemySpawnerConfig _config;
    [SerializeField] private Camera _mainCamera;
    [SerializeField] private Transform _playerTransform;

    private readonly HashSet<Enemy> _aliveEnemies = new();
    private readonly Dictionary<Enemy, Action> _deathCallbacks = new();

    private float _spawnTimer;

    public int AliveCount => _aliveEnemies.Count;
    public float SpawnTimer
    {
        get => _spawnTimer;
        set => _spawnTimer = Mathf.Max(0f, value);
    }

    private void Reset()
    {
        _mainCamera = Camera.main;
    }

    private void Awake()
    {
        CacheReferences();
        CleanupDestroyedEnemies();
        _spawnTimer = GetSpawnInterval();
    }

    private void OnEnable()
    {
        CacheReferences();
        _spawnTimer = GetSpawnInterval();
    }

    private void OnDisable()
    {
    }

    private void Update()
    {
        if (!isActiveAndEnabled)
        {
            return;
        }

        _spawnTimer -= Time.deltaTime;
        if (_spawnTimer > 0f)
        {
            return;
        }

        _spawnTimer = GetSpawnInterval();
        TriggerSpawnAttempt();
    }

    private void OnDestroy()
    {
        foreach (KeyValuePair<Enemy, Action> pair in _deathCallbacks)
        {
            if (pair.Key != null)
            {
                pair.Key.OnDied -= pair.Value;
            }
        }

        _deathCallbacks.Clear();
        _aliveEnemies.Clear();
    }

    public void TriggerSpawnAttempt()
    {
        if (!isActiveAndEnabled)
        {
            return;
        }

        CleanupDestroyedEnemies();
        TrySpawnEnemy();
    }

    private void TrySpawnEnemy()
    {
        if (!CanAttemptSpawn())
        {
            return;
        }

        if (!TryGetSpawnPosition(out Vector3 spawnPosition))
        {
            return;
        }

        if (!TryPickSpawnPrefab(out GameObject prefab))
        {
            return;
        }

        GameObject spawnedObject = Instantiate(prefab, spawnPosition, Quaternion.identity);
        Enemy enemy = spawnedObject.GetComponent<Enemy>();
        if (enemy == null)
        {
            Destroy(spawnedObject);
            return;
        }

        RegisterEnemy(enemy);
    }

    private bool CanAttemptSpawn()
    {
        if (_config == null)
        {
            return false;
        }

        if (_config.SpawnTable == null || _config.SpawnTable.Count == 0)
        {
            return false;
        }

        CacheReferences();
        return _mainCamera != null
            && _playerTransform != null
            && AliveCount < Mathf.Max(0, _config.MaxConcurrent);
    }

    private bool TryGetSpawnPosition(out Vector3 spawnPosition)
    {
        spawnPosition = default;

        float minDistance = Mathf.Max(0f, _config.MinSpawnDistance);
        float maxDistance = Mathf.Max(minDistance, _config.MaxSpawnDistance);
        float rayHeight = Mathf.Max(0f, _config.SkyRaycastHeight);
        float rayDistance = Mathf.Max(0f, _config.RaycastDistance);

        if (rayDistance <= 0f)
        {
            return false;
        }

        for (int attempt = 0; attempt < SpawnPointAttempts; attempt++)
        {
            float sign = UnityEngine.Random.value < 0.5f ? -1f : 1f;
            float distance = UnityEngine.Random.Range(minDistance, maxDistance);
            float candidateX = _playerTransform.position.x + sign * distance;
            candidateX = Mathf.Floor(candidateX) + TileCenterOffset;

            float alignedDistance = Mathf.Abs(candidateX - _playerTransform.position.x);
            if (alignedDistance < minDistance || alignedDistance > maxDistance)
            {
                continue;
            }

            Vector2 rayOrigin = new(candidateX, _playerTransform.position.y + rayHeight);
            RaycastHit2D hit = Physics2D.Raycast(rayOrigin, Vector2.down, rayDistance, _config.GroundLayer);
            if (!hit.collider)
            {
                continue;
            }

            Vector3 candidatePosition = new(candidateX, hit.point.y + TileCenterOffset, 0f);
            if (IsInCameraView(candidatePosition))
            {
                continue;
            }

            spawnPosition = candidatePosition;
            return true;
        }

        return false;
    }

    private bool TryPickSpawnPrefab(out GameObject prefab)
    {
        prefab = null;

        float totalWeight = 0f;
        List<EnemySpawnEntry> spawnTable = _config.SpawnTable;
        for (int i = 0; i < spawnTable.Count; i++)
        {
            EnemySpawnEntry entry = spawnTable[i];
            if (!IsValidEntry(entry))
            {
                continue;
            }

            totalWeight += Mathf.Max(0f, entry.Weight);
        }

        if (totalWeight <= 0f)
        {
            return false;
        }

        float roll = UnityEngine.Random.Range(0f, totalWeight);
        float cumulative = 0f;

        for (int i = 0; i < spawnTable.Count; i++)
        {
            EnemySpawnEntry entry = spawnTable[i];
            if (!IsValidEntry(entry))
            {
                continue;
            }

            cumulative += Mathf.Max(0f, entry.Weight);
            if (roll > cumulative)
            {
                continue;
            }

            prefab = entry.Prefab;
            return true;
        }

        return false;
    }

    private bool IsValidEntry(EnemySpawnEntry entry)
    {
        return entry != null
            && entry.Prefab != null
            && entry.Weight > 0f
            && entry.Prefab.GetComponent<Enemy>() != null;
    }

    private bool IsInCameraView(Vector3 worldPosition)
    {
        Vector3 viewportPosition = _mainCamera.WorldToViewportPoint(worldPosition);
        return viewportPosition.z > 0f
            && viewportPosition.x >= 0f
            && viewportPosition.x <= 1f
            && viewportPosition.y >= 0f
            && viewportPosition.y <= 1f;
    }

    private void RegisterEnemy(Enemy enemy)
    {
        if (enemy == null || _aliveEnemies.Contains(enemy))
        {
            return;
        }

        _aliveEnemies.Add(enemy);

        Action callback = null;
        callback = () => HandleEnemyDied(enemy);
        _deathCallbacks[enemy] = callback;
        enemy.OnDied += callback;
    }

    private void HandleEnemyDied(Enemy enemy)
    {
        if (enemy == null)
        {
            return;
        }

        if (_deathCallbacks.TryGetValue(enemy, out Action callback))
        {
            enemy.OnDied -= callback;
            _deathCallbacks.Remove(enemy);
        }

        _aliveEnemies.Remove(enemy);
    }

    private void CleanupDestroyedEnemies()
    {
        if (_aliveEnemies.Count == 0)
        {
            return;
        }

        List<Enemy> staleEnemies = null;
        foreach (Enemy enemy in _aliveEnemies)
        {
            if (enemy != null)
            {
                continue;
            }

            staleEnemies ??= new List<Enemy>();
            staleEnemies.Add(enemy);
        }

        if (staleEnemies == null)
        {
            return;
        }

        for (int i = 0; i < staleEnemies.Count; i++)
        {
            Enemy enemy = staleEnemies[i];
            _aliveEnemies.Remove(enemy);
            _deathCallbacks.Remove(enemy);
        }
    }

    private void CacheReferences()
    {
        if (_mainCamera == null)
        {
            _mainCamera = Camera.main;
        }

        if (_playerTransform == null)
        {
            GameObject playerObject = GameObject.FindWithTag("Player");
            if (playerObject != null)
            {
                _playerTransform = playerObject.transform;
            }
        }
    }

    private float GetSpawnInterval()
    {
        if (_config == null)
        {
            return 1f;
        }

        return Mathf.Max(0.01f, _config.SpawnInterval);
    }
}
