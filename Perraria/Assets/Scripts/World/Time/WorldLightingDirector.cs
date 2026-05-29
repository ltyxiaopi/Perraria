using UnityEngine;
using UnityEngine.Rendering.Universal;

[DisallowMultipleComponent]
public sealed class WorldLightingDirector : MonoBehaviour
{
    [SerializeField] private Light2D _globalLight;
    [SerializeField] private LightingKey[] _keys =
    {
        new() { AtMinutes = 120f, Color = new Color(0.30f, 0.40f, 0.70f), Intensity = 0.35f },
        new() { AtMinutes = 300f, Color = new Color(0.55f, 0.55f, 0.70f), Intensity = 0.50f },
        new() { AtMinutes = 360f, Color = new Color(1.00f, 0.95f, 0.85f), Intensity = 0.85f },
        new() { AtMinutes = 600f, Color = new Color(1.00f, 1.00f, 0.95f), Intensity = 1.00f },
        new() { AtMinutes = 840f, Color = new Color(1.00f, 0.90f, 0.80f), Intensity = 0.95f },
        new() { AtMinutes = 1080f, Color = new Color(1.00f, 0.60f, 0.40f), Intensity = 0.70f },
        new() { AtMinutes = 1200f, Color = new Color(0.45f, 0.50f, 0.75f), Intensity = 0.45f },
        new() { AtMinutes = 1320f, Color = new Color(0.30f, 0.40f, 0.70f), Intensity = 0.35f }
    };

    private float[] _atMinutes;
    private Color[] _colors;
    private float[] _intensities;

    [System.Serializable]
    public struct LightingKey
    {
        public float AtMinutes;
        public Color Color;
        public float Intensity;
    }

    private void Awake()
    {
        RebuildCache();
    }

    private void OnValidate()
    {
        RebuildCache();
    }

    private void Update()
    {
        if (_globalLight == null || _atMinutes == null || _atMinutes.Length == 0)
        {
            return;
        }

        float minutes = WorldClock.Instance != null
            ? WorldClock.Instance.CurrentGameMinutes
            : WorldClock.DefaultStartMinutes;
        _globalLight.color = GradientSampler.SampleColor(_atMinutes, _colors, minutes);
        _globalLight.intensity = GradientSampler.SampleFloat(_atMinutes, _intensities, minutes);
    }

    private void RebuildCache()
    {
        if (_keys == null || _keys.Length == 0)
        {
            return;
        }

        _atMinutes = new float[_keys.Length];
        _colors = new Color[_keys.Length];
        _intensities = new float[_keys.Length];

        for (int i = 0; i < _keys.Length; i++)
        {
            _atMinutes[i] = _keys[i].AtMinutes;
            _colors[i] = _keys[i].Color;
            _intensities[i] = _keys[i].Intensity;
        }
    }
}
