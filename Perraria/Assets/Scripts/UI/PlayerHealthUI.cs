using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
public sealed class PlayerHealthUI : MonoBehaviour
{
    private const int HeartCount = 5;
    private const int HpPerHeart = 20;
    private const int HalfHeartThreshold = HpPerHeart / 2;

    [SerializeField] private PlayerHealth _playerHealth;
    [SerializeField] private Image[] _heartImages;
    [SerializeField] private Sprite _heartFull;
    [SerializeField] private Sprite _heartEmpty;
    [SerializeField] private Sprite _heartHalf;

    private void Awake()
    {
        RefreshFromPlayer();
    }

    private void OnEnable()
    {
        SubscribeEvents();
        RefreshFromPlayer();
    }

    private void OnDisable()
    {
        UnsubscribeEvents();
    }

    private void SubscribeEvents()
    {
        if (_playerHealth != null)
        {
            _playerHealth.OnHealthChanged += HandleHealthChanged;
        }
    }

    private void UnsubscribeEvents()
    {
        if (_playerHealth != null)
        {
            _playerHealth.OnHealthChanged -= HandleHealthChanged;
        }
    }

    private void HandleHealthChanged(int currentHealth, int maxHealth)
    {
        RefreshUI(currentHealth, maxHealth);
    }

    private void RefreshFromPlayer()
    {
        if (_playerHealth == null)
        {
            return;
        }

        RefreshUI(_playerHealth.CurrentHealth, _playerHealth.MaxHealth);
    }

    private void RefreshUI(int currentHealth, int maxHealth)
    {
        if (_heartImages == null)
        {
            return;
        }

        int clampedHealth = Mathf.Clamp(currentHealth, 0, Mathf.Max(maxHealth, 0));
        int heartSlots = Mathf.Min(_heartImages.Length, HeartCount);
        for (int i = 0; i < heartSlots; i++)
        {
            UpdateHeartImage(_heartImages[i], clampedHealth, i);
        }
    }

    private void UpdateHeartImage(Image heartImage, int currentHealth, int heartIndex)
    {
        if (heartImage == null)
        {
            return;
        }

        heartImage.sprite = GetHeartSprite(currentHealth, heartIndex);
    }

    private Sprite GetHeartSprite(int currentHealth, int heartIndex)
    {
        int hpForThisHeart = Mathf.Clamp(currentHealth - heartIndex * HpPerHeart, 0, HpPerHeart);
        if (hpForThisHeart >= HpPerHeart)
        {
            return _heartFull;
        }

        if (hpForThisHeart >= HalfHeartThreshold)
        {
            return _heartHalf;
        }

        return _heartEmpty;
    }
}
