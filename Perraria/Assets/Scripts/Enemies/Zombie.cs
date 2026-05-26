using UnityEngine;

public sealed class Zombie : Enemy
{
    [Header("Zombie Walk")]
    [SerializeField] private float _walkSpeed = 2.5f;
    [SerializeField] private float _stepUpVerticalImpulse = 4f;
    [SerializeField] private float _obstacleProbeDistance = 0.5f;

    protected override void UpdateBehavior()
    {
        if (_state != EnemyState.Chasing || _playerTransform == null)
        {
            _rigidbody2D.linearVelocity = new Vector2(0f, _rigidbody2D.linearVelocity.y);
            return;
        }

        float direction = Mathf.Sign(_playerTransform.position.x - transform.position.x);
        Vector2 origin = (Vector2)transform.position + Vector2.right * direction * _obstacleProbeDistance;
        bool blockedFeet = Physics2D.OverlapCircle(origin, 0.15f, _groundLayer);
        bool blockedHead = Physics2D.OverlapCircle(origin + Vector2.up * 1f, 0.15f, _groundLayer);

        if (blockedFeet && blockedHead)
        {
            _rigidbody2D.linearVelocity = new Vector2(0f, _rigidbody2D.linearVelocity.y);
            return;
        }

        _rigidbody2D.linearVelocity = new Vector2(direction * _walkSpeed, _rigidbody2D.linearVelocity.y);

        if (blockedFeet && !blockedHead && _isGrounded)
        {
            _rigidbody2D.linearVelocity = new Vector2(_rigidbody2D.linearVelocity.x, _stepUpVerticalImpulse);
        }
    }
}
