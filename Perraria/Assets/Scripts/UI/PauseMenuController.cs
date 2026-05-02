using System.Collections;
using TMPro;
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

    private const string SaveButtonText = "Save";
    private const string SavedButtonText = "Saved";
    private const float SaveFeedbackSeconds = 1f;

    private TMP_Text _saveButtonLabel;
    private Coroutine _saveFeedbackCoroutine;

    public bool IsPaused { get; private set; }

    private void Awake()
    {
        CacheSaveButtonLabel();
        SetPanelActive(false);
        SetSaveButtonEnabled();
    }

    private void OnEnable()
    {
        SubscribeButtons();
        CacheSaveButtonLabel();
        SetSaveButtonEnabled();
        SetSaveButtonText(SaveButtonText);
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
        SaveData snapshot = GameStateSnapshot.Capture();
        SaveSystem.Save(snapshot);
        ShowSaveFeedback();
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

    private void SetSaveButtonEnabled()
    {
        if (_saveButton != null)
        {
            _saveButton.interactable = true;
        }
    }

    private void RestoreTimeScale()
    {
        StopSaveFeedback();
        Time.timeScale = 1f;
        IsPaused = false;
        SetPanelActive(false);
    }

    private void CacheSaveButtonLabel()
    {
        if (_saveButton != null && _saveButtonLabel == null)
        {
            _saveButtonLabel = _saveButton.GetComponentInChildren<TMP_Text>(true);
        }
    }

    private void ShowSaveFeedback()
    {
        if (_saveFeedbackCoroutine != null)
        {
            StopCoroutine(_saveFeedbackCoroutine);
        }

        _saveFeedbackCoroutine = StartCoroutine(SaveFeedbackRoutine());
    }

    private IEnumerator SaveFeedbackRoutine()
    {
        SetSaveButtonText(SavedButtonText);
        yield return new WaitForSecondsRealtime(SaveFeedbackSeconds);
        SetSaveButtonText(SaveButtonText);
        _saveFeedbackCoroutine = null;
    }

    private void StopSaveFeedback()
    {
        if (_saveFeedbackCoroutine != null)
        {
            StopCoroutine(_saveFeedbackCoroutine);
            _saveFeedbackCoroutine = null;
        }

        SetSaveButtonText(SaveButtonText);
    }

    private void SetSaveButtonText(string text)
    {
        CacheSaveButtonLabel();
        if (_saveButtonLabel != null)
        {
            _saveButtonLabel.text = text;
        }
    }
}
