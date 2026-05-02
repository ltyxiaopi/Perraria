using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

[DisallowMultipleComponent]
public sealed class PauseMenuController : MonoBehaviour
{
    [SerializeField] private GameObject _pausePanel;
    [SerializeField] private Button _resumeButton;
    [SerializeField] private Button _saveButton;
    [SerializeField] private Button _mainMenuButton;
    [SerializeField] private string _mainMenuSceneName = "MainMenu";

    public bool IsPaused { get; private set; }

    private void Awake()
    {
        SetPanelActive(false);
        SetSaveButtonDisabled();
    }

    private void OnEnable()
    {
        SubscribeButtons();
        SetSaveButtonDisabled();
    }

    private void OnDisable()
    {
        UnsubscribeButtons();
        RestoreTimeScale();
    }

    private void OnDestroy()
    {
        RestoreTimeScale();
    }

    private void Update()
    {
        if (Keyboard.current == null || !Keyboard.current.escapeKey.wasPressedThisFrame)
        {
            return;
        }

        if (IsPaused)
        {
            Resume();
            return;
        }

        Pause();
    }

    public void Pause()
    {
        SetPanelActive(true);
        Time.timeScale = 0f;
        IsPaused = true;
    }

    public void Resume()
    {
        SetPanelActive(false);
        Time.timeScale = 1f;
        IsPaused = false;
    }

    public void OnSaveClicked()
    {
    }

    public void OnMainMenuClicked()
    {
        Time.timeScale = 1f;
        IsPaused = false;
        SetPanelActive(false);
        SceneManager.LoadScene(_mainMenuSceneName, LoadSceneMode.Single);
    }

    private void SubscribeButtons()
    {
        if (_resumeButton != null)
        {
            _resumeButton.onClick.AddListener(Resume);
        }

        if (_saveButton != null)
        {
            _saveButton.onClick.AddListener(OnSaveClicked);
        }

        if (_mainMenuButton != null)
        {
            _mainMenuButton.onClick.AddListener(OnMainMenuClicked);
        }
    }

    private void UnsubscribeButtons()
    {
        if (_resumeButton != null)
        {
            _resumeButton.onClick.RemoveListener(Resume);
        }

        if (_saveButton != null)
        {
            _saveButton.onClick.RemoveListener(OnSaveClicked);
        }

        if (_mainMenuButton != null)
        {
            _mainMenuButton.onClick.RemoveListener(OnMainMenuClicked);
        }
    }

    private void SetPanelActive(bool active)
    {
        if (_pausePanel != null)
        {
            _pausePanel.SetActive(active);
        }
    }

    private void SetSaveButtonDisabled()
    {
        if (_saveButton != null)
        {
            _saveButton.interactable = false;
        }
    }

    private void RestoreTimeScale()
    {
        Time.timeScale = 1f;
        IsPaused = false;
        SetPanelActive(false);
    }
}
