using UnityEngine;

public sealed class Slime : Enemy
{
    [Header("Slime Hop")]
    [SerializeField] private float _hopInterval = 1.5f;
    [SerializeField] private float _hopHorizontalSpeed = 3f;
    [SerializeField] private float _hopVerticalSpeed = 6f;

    private float _hopTimer;

    protected override void UpdateBehavior()
    {
        if (_state != EnemyState.Chasing || _playerTransform == null)
        {
            return;
        }

        _hopTimer -= Time.deltaTime;
        if (_hopTimer > 0f || !_isGrounded)
        {
            return;
        }

        Hop();
        _hopTimer = _hopInterval;
    }

    private void Hop()
    {
        float direction = Mathf.Sign(_playerTransform.position.x - transform.position.x);
        if (Mathf.Approximately(direction, 0f))
        {
            direction = _spriteRenderer != null && _spriteRenderer.flipX ? -1f : 1f;
        }

        _rigidbody2D.linearVelocity = new Vector2(direction * _hopHorizontalSpeed, _hopVerticalSpeed);
    }
}
