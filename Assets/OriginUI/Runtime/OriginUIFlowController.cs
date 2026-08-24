using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

public class OriginUIFlowController : MonoBehaviour
{
    public GameObject hudRoot;
    public GameObject titleRoot;
    public GameObject pauseRoot;
    public GameObject resultRoot;
    public GameObject gameOverRoot;

    public Button pauseFirstButton;
    public Button resultFirstButton;
    public Button gameOverFirstButton;
    public Button titleFirstButton;

    public bool IsPaused { get; private set; }

    private void Start()
    {
        if (pauseRoot != null) pauseRoot.SetActive(false);
        if (resultRoot != null) resultRoot.SetActive(false);
        if (gameOverRoot != null) gameOverRoot.SetActive(false);

        if (titleRoot != null && titleRoot.activeSelf)
        {
            Time.timeScale = 0f;
            if (hudRoot != null) hudRoot.SetActive(false);
            SelectButton(titleFirstButton);
        }
        else
        {
            Time.timeScale = 1f;
        }
    }

    private void Update()
    {
        bool cancelPressed = false;
#if ENABLE_INPUT_SYSTEM
        cancelPressed = Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame;
#else
        cancelPressed = Input.GetKeyDown(KeyCode.Escape);
#endif
        if (cancelPressed && (resultRoot == null || !resultRoot.activeSelf) && (gameOverRoot == null || !gameOverRoot.activeSelf))
            TogglePause();
    }

    public void TogglePause()
    {
        if (titleRoot != null && titleRoot.activeSelf) return;
        if (IsPaused) Resume(); else Pause();
    }

    public void Pause()
    {
        IsPaused = true;
        Time.timeScale = 0f;
        if (pauseRoot != null) pauseRoot.SetActive(true);
        SelectButton(pauseFirstButton);
    }

    public void Resume()
    {
        IsPaused = false;
        Time.timeScale = 1f;
        if (pauseRoot != null) pauseRoot.SetActive(false);
        if (EventSystem.current != null) EventSystem.current.SetSelectedGameObject(null);
    }

    public void ShowResult()
    {
        IsPaused = false;
        Time.timeScale = 0f;
        if (resultRoot != null) resultRoot.SetActive(true);
        SelectButton(resultFirstButton);
    }

    public void ShowGameOver()
    {
        IsPaused = false;
        Time.timeScale = 0f;
        if (gameOverRoot != null) gameOverRoot.SetActive(true);
        SelectButton(gameOverFirstButton);
    }

    public void StartFromTitle()
    {
        IsPaused = false;
        Time.timeScale = 1f;
        if (titleRoot != null) titleRoot.SetActive(false);
        if (hudRoot != null) hudRoot.SetActive(true);
        if (EventSystem.current != null) EventSystem.current.SetSelectedGameObject(null);
    }

    public void ReturnToTitle()
    {
        IsPaused = false;
        Time.timeScale = 0f;
        if (pauseRoot != null) pauseRoot.SetActive(false);
        if (resultRoot != null) resultRoot.SetActive(false);
        if (gameOverRoot != null) gameOverRoot.SetActive(false);
        if (hudRoot != null) hudRoot.SetActive(false);
        if (titleRoot != null) titleRoot.SetActive(true);
        SelectButton(titleFirstButton);
    }

    public void RestartScene()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    public void QuitGame()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    private void SelectButton(Button button)
    {
        if (button == null || EventSystem.current == null) return;
        EventSystem.current.SetSelectedGameObject(null);
        EventSystem.current.SetSelectedGameObject(button.gameObject);
    }
}
