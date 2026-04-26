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
    [SerializeField] private Button _quitButton;
    [SerializeField] private string _gameSceneName = "SampleScene";

    private void OnEnable()
    {
        SubscribeButtons();
    }

    private void OnDisable()
    {
        UnsubscribeButtons();
    }

    public void OnStartClicked()
    {
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

        if (_quitButton != null)
        {
            _quitButton.onClick.RemoveListener(OnQuitClicked);
        }
    }
}
