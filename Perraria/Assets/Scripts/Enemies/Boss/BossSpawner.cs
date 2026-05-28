using UnityEngine;

[DisallowMultipleComponent]
public sealed class BossSpawner : MonoBehaviour
{
    [SerializeField] private GameObject _bossPrefab;
    [SerializeField] private float _spawnHeightAboveCaller = 8f;

    public GameObject Spawn(Vector3 callerPosition)
    {
        if (_bossPrefab == null)
        {
            Debug.LogWarning("BossSpawner has no boss prefab configured.", this);
            Destroy(gameObject);
            return null;
        }

        Vector3 spawnPosition = callerPosition + Vector3.up * Mathf.Max(0f, _spawnHeightAboveCaller);
        GameObject boss = Instantiate(_bossPrefab, spawnPosition, Quaternion.identity);
        Destroy(gameObject);
        return boss;
    }
}
