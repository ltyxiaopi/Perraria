using System;
using System.Collections;
using UnityEngine;

[DisallowMultipleComponent]
public sealed class EnemyScenePlacement : MonoBehaviour
{
    [SerializeField] private Transform _player;
    [SerializeField] private LayerMask _groundLayer;
    [SerializeField] private float _raycastHeight = 20f;
    [SerializeField] private float _raycastDistance = 120f;
    [SerializeField] private float[] _spawnOffsets =
    {
        2f,
        6f,
        8f,
        10f,
        12f,
        14f,
        -6f,
        -8f,
        -10f,
        -12f,
        -14f,
        -16f
    };

    private IEnumerator Start()
    {
        yield return null;

        if (_player == null)
        {
            GameObject player = GameObject.FindWithTag("Player");
            if (player != null)
            {
                _player = player.transform;
            }
        }

        if (_player == null)
        {
            yield break;
        }

        Enemy[] enemies = GetComponentsInChildren<Enemy>(false);
        Array.Sort(enemies, (left, right) => left.transform.GetSiblingIndex().CompareTo(right.transform.GetSiblingIndex()));

        int placementCount = Mathf.Min(enemies.Length, _spawnOffsets.Length);
        for (int i = 0; i < placementCount; i++)
        {
            PlaceEnemy(enemies[i], _spawnOffsets[i]);
        }
    }

    private void PlaceEnemy(Enemy enemy, float offsetX)
    {
        if (enemy == null)
        {
            return;
        }

        Vector2 rayOrigin = new Vector2(_player.position.x + offsetX, _player.position.y + _raycastHeight);
        RaycastHit2D hit = Physics2D.Raycast(rayOrigin, Vector2.down, _raycastDistance, _groundLayer);
        if (hit.collider == null)
        {
            return;
        }

        float placementY = hit.point.y + GetBottomToPivotHeight(enemy.transform);
        enemy.transform.position = new Vector3(hit.point.x, placementY, enemy.transform.position.z);

        Rigidbody2D rigidbody2D = enemy.GetComponent<Rigidbody2D>();
        if (rigidbody2D != null)
        {
            rigidbody2D.linearVelocity = Vector2.zero;
            rigidbody2D.angularVelocity = 0f;
        }
    }

    private float GetBottomToPivotHeight(Transform enemyTransform)
    {
        CapsuleCollider2D capsuleCollider2D = enemyTransform.GetComponent<CapsuleCollider2D>();
        if (capsuleCollider2D != null)
        {
            float bottomLocalY = capsuleCollider2D.offset.y - capsuleCollider2D.size.y * 0.5f;
            return -bottomLocalY * enemyTransform.localScale.y;
        }

        BoxCollider2D boxCollider2D = enemyTransform.GetComponent<BoxCollider2D>();
        if (boxCollider2D != null)
        {
            float bottomLocalY = boxCollider2D.offset.y - boxCollider2D.size.y * 0.5f;
            return -bottomLocalY * enemyTransform.localScale.y;
        }

        SpriteRenderer spriteRenderer = enemyTransform.GetComponentInChildren<SpriteRenderer>();
        return spriteRenderer != null ? spriteRenderer.bounds.extents.y : 0.5f;
    }
}
