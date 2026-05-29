using UnityEngine;

[DisallowMultipleComponent]
public sealed class ParallaxCloudLayer : MonoBehaviour
{
    [SerializeField] private SpriteRenderer _copyA;
    [SerializeField] private SpriteRenderer _copyB;
    [SerializeField] private Transform _camera;
    [SerializeField] private float _parallaxFactor = 0.15f;
    [SerializeField] private float _windSpeed = 0.2f;

    private float _layerWidth;

    public float ParallaxFactor => _parallaxFactor;
    public float LayerWidth => _layerWidth;
    public Vector3 CopyAPosition => _copyA != null ? _copyA.transform.position : Vector3.zero;

    private void Awake()
    {
        if (_camera == null && Camera.main != null)
        {
            _camera = Camera.main.transform;
        }

        RefreshScale();
    }

    private void LateUpdate()
    {
        if (_camera == null || _copyA == null || _copyB == null || _layerWidth <= 0f)
        {
            return;
        }

        transform.position = new Vector3(_camera.position.x, _camera.position.y, transform.position.z);

        float scroll = _camera.position.x * (1f - _parallaxFactor) + _windSpeed * Time.time;
        float wrapped = Mathf.Repeat(scroll, _layerWidth);
        _copyA.transform.localPosition = new Vector3(-wrapped, 0f, 0f);
        _copyB.transform.localPosition = new Vector3(-wrapped + _layerWidth, 0f, 0f);
    }

    public void SetTint(Color color)
    {
        if (_copyA != null)
        {
            _copyA.color = color;
        }

        if (_copyB != null)
        {
            _copyB.color = color;
        }
    }

    public void RefreshScale()
    {
        Camera camera = _camera != null ? _camera.GetComponent<Camera>() : Camera.main;
        SpriteRenderer reference = _copyA != null ? _copyA : _copyB;
        if (camera == null || reference == null || reference.sprite == null)
        {
            return;
        }

        float viewHeight = camera.orthographicSize * 2f;
        float scale = viewHeight / reference.sprite.bounds.size.y;
        Vector3 targetScale = new(scale, scale, 1f);
        if (_copyA != null)
        {
            _copyA.transform.localScale = targetScale;
        }

        if (_copyB != null)
        {
            _copyB.transform.localScale = targetScale;
        }

        _layerWidth = reference.sprite.bounds.size.x * scale;
    }
}
