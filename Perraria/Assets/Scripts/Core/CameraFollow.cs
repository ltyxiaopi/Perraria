using UnityEngine;

[DisallowMultipleComponent]
public sealed class CameraFollow : MonoBehaviour
{
    [SerializeField] private Transform _target;
    [SerializeField] private float _smoothSpeed = 5f;
    [SerializeField] private Vector3 _offset = new Vector3(0f, 2f, -10f);

    private void LateUpdate()
    {
        if (_target == null)
            return;

        Vector3 desired = _target.position + _offset;
        Vector3 smoothed = Vector3.Lerp(transform.position, desired, _smoothSpeed * Time.deltaTime);
        transform.position = smoothed;
    }
}
