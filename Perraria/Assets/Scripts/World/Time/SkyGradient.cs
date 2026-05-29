using UnityEngine;

[DisallowMultipleComponent]
public sealed class SkyGradient : MonoBehaviour
{
    private const int GeneratedTextureWidth = 4;
    private const int GeneratedTextureHeight = 256;

    [SerializeField] private SpriteRenderer _skyFill;
    [SerializeField] private SpriteRenderer _skyTop;
    [SerializeField] private SpriteRenderer _starfield;
    [SerializeField] private SpriteRenderer _moon;
    [SerializeField] private ParallaxCloudLayer[] _cloudLayers;
    [SerializeField] private SkyColorKey[] _skyKeys =
    {
        new() { AtMinutes = 120f, TopColor = new Color(0.06f, 0.08f, 0.20f), HorizonColor = new Color(0.12f, 0.15f, 0.32f) },
        new() { AtMinutes = 300f, TopColor = new Color(0.20f, 0.22f, 0.42f), HorizonColor = new Color(0.45f, 0.40f, 0.55f) },
        new() { AtMinutes = 360f, TopColor = new Color(0.55f, 0.55f, 0.80f), HorizonColor = new Color(0.98f, 0.80f, 0.65f) },
        new() { AtMinutes = 600f, TopColor = new Color(0.40f, 0.55f, 0.92f), HorizonColor = new Color(0.62f, 0.78f, 0.98f) },
        new() { AtMinutes = 840f, TopColor = new Color(0.42f, 0.56f, 0.90f), HorizonColor = new Color(0.65f, 0.78f, 0.95f) },
        new() { AtMinutes = 1080f, TopColor = new Color(0.45f, 0.35f, 0.55f), HorizonColor = new Color(0.92f, 0.50f, 0.38f) },
        new() { AtMinutes = 1200f, TopColor = new Color(0.10f, 0.12f, 0.28f), HorizonColor = new Color(0.20f, 0.20f, 0.40f) },
        new() { AtMinutes = 1320f, TopColor = new Color(0.06f, 0.08f, 0.20f), HorizonColor = new Color(0.12f, 0.15f, 0.32f) }
    };
    [SerializeField] private CloudTintKey[] _cloudTintKeys =
    {
        new() { AtMinutes = 120f, Tint = new Color(0.30f, 0.36f, 0.55f) },
        new() { AtMinutes = 300f, Tint = new Color(0.55f, 0.55f, 0.65f) },
        new() { AtMinutes = 360f, Tint = new Color(1.00f, 0.92f, 0.82f) },
        new() { AtMinutes = 600f, Tint = Color.white },
        new() { AtMinutes = 840f, Tint = new Color(1.00f, 0.98f, 0.95f) },
        new() { AtMinutes = 1080f, Tint = new Color(1.00f, 0.62f, 0.45f) },
        new() { AtMinutes = 1200f, Tint = new Color(0.45f, 0.45f, 0.62f) },
        new() { AtMinutes = 1320f, Tint = new Color(0.30f, 0.36f, 0.55f) }
    };
    [SerializeField] private NightAlphaKey[] _nightKeys =
    {
        new() { AtMinutes = 120f, Alpha = 1f },
        new() { AtMinutes = 300f, Alpha = 1f },
        new() { AtMinutes = 360f, Alpha = 0f },
        new() { AtMinutes = 600f, Alpha = 0f },
        new() { AtMinutes = 840f, Alpha = 0f },
        new() { AtMinutes = 1080f, Alpha = 0f },
        new() { AtMinutes = 1200f, Alpha = 1f },
        new() { AtMinutes = 1320f, Alpha = 1f }
    };

    private float[] _skyAtMinutes;
    private Color[] _topColors;
    private Color[] _horizonColors;
    private float[] _cloudAtMinutes;
    private Color[] _cloudTints;
    private float[] _nightAtMinutes;
    private float[] _nightAlphas;
    private Sprite _generatedFillSprite;
    private Sprite _generatedTopSprite;

    public float CurrentNightAlpha { get; private set; }
    public Color CurrentCloudTint { get; private set; }

    [System.Serializable]
    public struct SkyColorKey
    {
        public float AtMinutes;
        public Color TopColor;
        public Color HorizonColor;
    }

    [System.Serializable]
    public struct CloudTintKey
    {
        public float AtMinutes;
        public Color Tint;
    }

    [System.Serializable]
    public struct NightAlphaKey
    {
        public float AtMinutes;
        public float Alpha;
    }

    private void Awake()
    {
        CreateRuntimeSprites();
        RebuildCache();
        RefreshCameraScale();
    }

    private void OnValidate()
    {
        RebuildCache();
    }

    private void Update()
    {
        if (_skyAtMinutes == null || _skyAtMinutes.Length == 0)
        {
            return;
        }

        float minutes = WorldClock.Instance != null
            ? WorldClock.Instance.CurrentGameMinutes
            : WorldClock.DefaultStartMinutes;
        ApplySky(minutes);
        ApplyNight(minutes);
        ApplyCloudTint(minutes);
    }

    private void LateUpdate()
    {
        RefreshCameraScale();
    }

    private void OnDestroy()
    {
        DestroyGeneratedSprite(_generatedFillSprite);
        DestroyGeneratedSprite(_generatedTopSprite);
    }

    private void ApplySky(float minutes)
    {
        if (_skyTop != null)
        {
            _skyTop.color = GradientSampler.SampleColor(_skyAtMinutes, _topColors, minutes);
        }

        if (_skyFill != null)
        {
            _skyFill.color = GradientSampler.SampleColor(_skyAtMinutes, _horizonColors, minutes);
        }
    }

    private void ApplyNight(float minutes)
    {
        CurrentNightAlpha = GradientSampler.SampleFloat(_nightAtMinutes, _nightAlphas, minutes);
        SetRendererAlpha(_starfield, CurrentNightAlpha);
        SetRendererAlpha(_moon, CurrentNightAlpha);
    }

    private void ApplyCloudTint(float minutes)
    {
        CurrentCloudTint = GradientSampler.SampleColor(_cloudAtMinutes, _cloudTints, minutes);
        if (_cloudLayers == null)
        {
            return;
        }

        for (int i = 0; i < _cloudLayers.Length; i++)
        {
            if (_cloudLayers[i] != null)
            {
                _cloudLayers[i].SetTint(CurrentCloudTint);
            }
        }
    }

    private void CreateRuntimeSprites()
    {
        if (_skyFill != null && _skyFill.sprite == null)
        {
            _generatedFillSprite = CreateFillSprite();
            _skyFill.sprite = _generatedFillSprite;
        }

        if (_skyTop != null && _skyTop.sprite == null)
        {
            _generatedTopSprite = CreateTopGradientSprite();
            _skyTop.sprite = _generatedTopSprite;
        }
    }

    private void RefreshCameraScale()
    {
        Camera camera = Camera.main;
        if (camera == null)
        {
            return;
        }

        FitToCamera(_skyFill, camera);
        FitToCamera(_skyTop, camera);
        FitToCamera(_starfield, camera);
        FitToCamera(_moon, camera);
    }

    private void RebuildCache()
    {
        BuildSkyCache();
        BuildCloudCache();
        BuildNightCache();
    }

    private void BuildSkyCache()
    {
        if (_skyKeys == null || _skyKeys.Length == 0)
        {
            return;
        }

        _skyAtMinutes = new float[_skyKeys.Length];
        _topColors = new Color[_skyKeys.Length];
        _horizonColors = new Color[_skyKeys.Length];
        for (int i = 0; i < _skyKeys.Length; i++)
        {
            _skyAtMinutes[i] = _skyKeys[i].AtMinutes;
            _topColors[i] = _skyKeys[i].TopColor;
            _horizonColors[i] = _skyKeys[i].HorizonColor;
        }
    }

    private void BuildCloudCache()
    {
        if (_cloudTintKeys == null || _cloudTintKeys.Length == 0)
        {
            return;
        }

        _cloudAtMinutes = new float[_cloudTintKeys.Length];
        _cloudTints = new Color[_cloudTintKeys.Length];
        for (int i = 0; i < _cloudTintKeys.Length; i++)
        {
            _cloudAtMinutes[i] = _cloudTintKeys[i].AtMinutes;
            _cloudTints[i] = _cloudTintKeys[i].Tint;
        }
    }

    private void BuildNightCache()
    {
        if (_nightKeys == null || _nightKeys.Length == 0)
        {
            return;
        }

        _nightAtMinutes = new float[_nightKeys.Length];
        _nightAlphas = new float[_nightKeys.Length];
        for (int i = 0; i < _nightKeys.Length; i++)
        {
            _nightAtMinutes[i] = _nightKeys[i].AtMinutes;
            _nightAlphas[i] = _nightKeys[i].Alpha;
        }
    }

    private static Sprite CreateFillSprite()
    {
        Texture2D texture = new(GeneratedTextureWidth, GeneratedTextureWidth, TextureFormat.RGBA32, false);
        texture.name = "RuntimeSkyFill";
        texture.filterMode = FilterMode.Point;
        texture.wrapMode = TextureWrapMode.Clamp;

        for (int y = 0; y < texture.height; y++)
        {
            for (int x = 0; x < texture.width; x++)
            {
                texture.SetPixel(x, y, Color.white);
            }
        }

        texture.Apply();
        return Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height), new Vector2(0.5f, 0.5f), 1f);
    }

    private static Sprite CreateTopGradientSprite()
    {
        Texture2D texture = new(GeneratedTextureWidth, GeneratedTextureHeight, TextureFormat.RGBA32, false);
        texture.name = "RuntimeSkyTopGradient";
        texture.filterMode = FilterMode.Bilinear;
        texture.wrapMode = TextureWrapMode.Clamp;

        for (int y = 0; y < texture.height; y++)
        {
            float alpha = y / (float)(texture.height - 1);
            Color color = new(1f, 1f, 1f, alpha);
            for (int x = 0; x < texture.width; x++)
            {
                texture.SetPixel(x, y, color);
            }
        }

        texture.Apply();
        return Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height), new Vector2(0.5f, 0.5f), 1f);
    }

    private static void FitToCamera(SpriteRenderer renderer, Camera camera)
    {
        if (renderer == null || renderer.sprite == null)
        {
            return;
        }

        float viewHeight = camera.orthographicSize * 2f;
        float viewWidth = viewHeight * camera.aspect;
        Vector2 spriteSize = renderer.sprite.bounds.size;
        renderer.transform.localScale = new Vector3(viewWidth / spriteSize.x, viewHeight / spriteSize.y, 1f);
    }

    private static void SetRendererAlpha(SpriteRenderer renderer, float alpha)
    {
        if (renderer == null)
        {
            return;
        }

        Color color = renderer.color;
        color.a = alpha;
        renderer.color = color;
    }

    private static void DestroyGeneratedSprite(Sprite sprite)
    {
        if (sprite == null)
        {
            return;
        }

        Texture texture = sprite.texture;
        Destroy(sprite);
        Destroy(texture);
    }
}
