using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
#if UNITY_EDITOR
using UnityEditor;
#endif

[DisallowMultipleComponent]
public sealed class MainMenuController : MonoBehaviour
{
    [SerializeField] private Button _startButton;
    [SerializeField] private Button _continueButton;
    [SerializeField] private Button _quitButton;
    [SerializeField] private string _gameSceneName = "SampleScene";

    private void OnEnable()
    {
        SubscribeButtons();
    }

    private void Start()
    {
        RefreshContinueButton();
    }

    private void OnDisable()
    {
        UnsubscribeButtons();
    }

    public void OnStartClicked()
    {
        GameManager.PendingLaunchMode = GameLaunchMode.NewGame;
        SceneManager.LoadScene(_gameSceneName, LoadSceneMode.Single);
    }

    public void OnContinueClicked()
    {
        GameManager.PendingLaunchMode = GameLaunchMode.ContinueSave;
        SceneManager.LoadScene(_gameSceneName, LoadSceneMode.Single);
    }

    public void OnQuitClicked()
    {
#if UNITY_EDITOR
        EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    private void SubscribeButtons()
    {
        if (_startButton != null)
        {
            _startButton.onClick.AddListener(OnStartClicked);
        }

        if (_continueButton != null)
        {
            _continueButton.onClick.AddListener(OnContinueClicked);
        }

        if (_quitButton != null)
        {
            _quitButton.onClick.AddListener(OnQuitClicked);
        }
    }

    private void UnsubscribeButtons()
    {
        if (_startButton != null)
        {
            _startButton.onClick.RemoveListener(OnStartClicked);
        }

        if (_continueButton != null)
        {
            _continueButton.onClick.RemoveListener(OnContinueClicked);
        }

        if (_quitButton != null)
        {
            _quitButton.onClick.RemoveListener(OnQuitClicked);
        }
    }

    private void RefreshContinueButton()
    {
        if (_continueButton != null)
        {
            _continueButton.gameObject.SetActive(SaveSystem.HasSave());
        }
    }
}
