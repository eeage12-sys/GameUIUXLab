using System.Collections;
using System.Reflection;
using TMPro;
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

    [Header("Scene Flow")]
    public string hubSceneName = "Hub_Field_Lightweight_V2";
    public string dungeonSceneName = "NGF_CompactDungeon";

    public bool IsPaused { get; private set; }

    private static EventSystem persistentEventSystem;

    private ThirdPersonCamera cachedThirdPersonCamera;
    private PlayerMovement cachedPlayerMovement;
    private PlayerAttack cachedPlayerAttack;

    private CanvasGroup hudCanvasGroup;
    private bool hudOldBlocksRaycasts = true;
    private bool hudOldInteractable = true;

    private Coroutine gameplayRestoreRoutine;

    // ThirdPersonCamera has its OWN private cursorLocked flag.
    // Cursor.lockState alone is not enough.
    private FieldInfo cameraCursorLockedField;

    private void Awake()
    {
        KeepEventSystemAlive();
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnDestroy()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private void Start()
    {
        ReacquireGameplayComponents();

        if (pauseRoot != null) pauseRoot.SetActive(false);
        if (resultRoot != null) resultRoot.SetActive(false);
        if (gameOverRoot != null) gameOverRoot.SetActive(false);

        if (hudRoot != null)
            hudCanvasGroup = GetOrAddCanvasGroup(hudRoot);

        if (titleRoot != null && titleRoot.activeSelf)
        {
            Time.timeScale = 0f;
            if (hudRoot != null) hudRoot.SetActive(false);

            DisableGameplayInput();
            SetCameraInternalCursorLock(false);
            UnlockCursorForUI();

            ConfigureMenuButtons(titleRoot);
            PrepareMenuForMouse(titleRoot);
            SelectButton(titleFirstButton);
        }
        else
        {
            StartGameplayRestore();
        }
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        KeepEventSystemAlive();
        DestroyDuplicateEventSystems();
        ReacquireGameplayComponents();

        IsPaused = false;
        Time.timeScale = 1f;

        if (pauseRoot != null) pauseRoot.SetActive(false);
        if (resultRoot != null) resultRoot.SetActive(false);
        if (gameOverRoot != null) gameOverRoot.SetActive(false);

        StartGameplayRestore();
    }

    private void Update()
    {
        bool escPressed = false;
        bool returnHotkeyPressed = false;

#if ENABLE_INPUT_SYSTEM
        if (Keyboard.current != null)
        {
            escPressed = Keyboard.current.escapeKey.wasPressedThisFrame;
            returnHotkeyPressed = Keyboard.current.rKey.wasPressedThisFrame;
        }
#else
        escPressed = Input.GetKeyDown(KeyCode.Escape);
        returnHotkeyPressed = Input.GetKeyDown(KeyCode.R);
#endif

        if (IsPaused && IsDungeonScene() && returnHotkeyPressed)
        {
            ReturnToVillage();
            return;
        }

        if (escPressed &&
            (resultRoot == null || !resultRoot.activeSelf) &&
            (gameOverRoot == null || !gameOverRoot.activeSelf))
        {
            TogglePause();
        }
    }

    public void TogglePause()
    {
        if (titleRoot != null && titleRoot.activeSelf) return;
        if (IsPaused) Resume();
        else Pause();
    }

    public void Pause()
    {
        if (gameplayRestoreRoutine != null)
        {
            StopCoroutine(gameplayRestoreRoutine);
            gameplayRestoreRoutine = null;
        }

        IsPaused = true;
        Time.timeScale = 0f;

        ReacquireGameplayComponents();

        // IMPORTANT: update the camera script's internal lock flag BEFORE disabling it.
        SetCameraInternalCursorLock(false);
        DisableGameplayInput();
        UnlockCursorForUI();

        if (hudRoot != null)
        {
            hudCanvasGroup = GetOrAddCanvasGroup(hudRoot);
            hudOldBlocksRaycasts = hudCanvasGroup.blocksRaycasts;
            hudOldInteractable = hudCanvasGroup.interactable;
            hudCanvasGroup.blocksRaycasts = false;
            hudCanvasGroup.interactable = false;
        }

        if (pauseRoot != null)
        {
            pauseRoot.SetActive(true);
            PrepareMenuForMouse(pauseRoot);
            ConfigureMenuButtons(pauseRoot);
        }

        SelectButton(pauseFirstButton);
        Debug.Log("[ORIGIN UI] PAUSE opened. Camera internal cursorLocked=FALSE.");
    }

    public void Resume()
    {
        Debug.Log("[ORIGIN UI] CONTINUE requested.");

        IsPaused = false;
        Time.timeScale = 1f;

        if (pauseRoot != null)
            pauseRoot.SetActive(false);

        RestoreHudRaycasts();

        if (EventSystem.current != null)
            EventSystem.current.SetSelectedGameObject(null);

        StartGameplayRestore();
    }

    public void StartFromTitle()
    {
        Debug.Log("[ORIGIN UI] START GAME requested.");

        IsPaused = false;
        Time.timeScale = 1f;

        if (titleRoot != null)
            titleRoot.SetActive(false);

        if (hudRoot != null)
            hudRoot.SetActive(true);

        RestoreHudRaycasts();

        if (EventSystem.current != null)
            EventSystem.current.SetSelectedGameObject(null);

        StartGameplayRestore();
    }

    public void RestartScene()
    {
        string currentScene = SceneManager.GetActiveScene().name;
        Debug.Log("[ORIGIN UI] RESTART requested. Reloading scene: " + currentScene);

        IsPaused = false;
        Time.timeScale = 1f;
        RestoreHudRaycasts();

        if (pauseRoot != null) pauseRoot.SetActive(false);
        if (resultRoot != null) resultRoot.SetActive(false);
        if (gameOverRoot != null) gameOverRoot.SetActive(false);

        SetCameraInternalCursorLock(false);
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        if (!Application.CanStreamedLevelBeLoaded(currentScene))
        {
            Debug.LogError($"[ORIGIN UI] Cannot restart scene '{currentScene}'. Check Build Profiles > Scene List.");
            return;
        }

        SceneManager.LoadScene(currentScene, LoadSceneMode.Single);
    }

    public void ShowResult()
    {
        IsPaused = false;
        Time.timeScale = 0f;

        ReacquireGameplayComponents();
        SetCameraInternalCursorLock(false);
        DisableGameplayInput();
        UnlockCursorForUI();

        if (resultRoot != null)
        {
            resultRoot.SetActive(true);
            PrepareMenuForMouse(resultRoot);
            ConfigureMenuButtons(resultRoot);
        }

        SelectButton(resultFirstButton);
    }

    public void ShowGameOver()
    {
        IsPaused = false;
        Time.timeScale = 0f;

        ReacquireGameplayComponents();
        SetCameraInternalCursorLock(false);
        DisableGameplayInput();
        UnlockCursorForUI();

        if (gameOverRoot != null)
        {
            gameOverRoot.SetActive(true);
            PrepareMenuForMouse(gameOverRoot);
            ConfigureMenuButtons(gameOverRoot);
        }

        SelectButton(gameOverFirstButton);
    }

    public void ReturnToTitle()
    {
        if (IsDungeonScene())
        {
            ReturnToVillage();
            return;
        }

        Debug.Log("[ORIGIN UI] BACK TO TITLE requested.");

        IsPaused = false;
        Time.timeScale = 0f;

        if (pauseRoot != null) pauseRoot.SetActive(false);
        if (resultRoot != null) resultRoot.SetActive(false);
        if (gameOverRoot != null) gameOverRoot.SetActive(false);
        if (hudRoot != null) hudRoot.SetActive(false);

        ReacquireGameplayComponents();
        SetCameraInternalCursorLock(false);
        DisableGameplayInput();
        UnlockCursorForUI();

        if (titleRoot != null)
        {
            titleRoot.SetActive(true);
            PrepareMenuForMouse(titleRoot);
            ConfigureMenuButtons(titleRoot);
        }

        SelectButton(titleFirstButton);
    }

    public void ReturnToVillage()
    {
        Debug.Log("[ORIGIN UI] RETURN TO VILLAGE requested.");

        IsPaused = false;
        Time.timeScale = 1f;
        RestoreHudRaycasts();

        if (pauseRoot != null) pauseRoot.SetActive(false);
        if (resultRoot != null) resultRoot.SetActive(false);
        if (gameOverRoot != null) gameOverRoot.SetActive(false);

        SetCameraInternalCursorLock(false);
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        if (!Application.CanStreamedLevelBeLoaded(hubSceneName))
        {
            Debug.LogError($"[ORIGIN UI] Cannot load Hub scene '{hubSceneName}'. Check Build Profiles > Scene List.");
            return;
        }

        SceneManager.LoadScene(hubSceneName, LoadSceneMode.Single);
    }

    public void QuitGame()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    private void StartGameplayRestore()
    {
        if (gameplayRestoreRoutine != null)
            StopCoroutine(gameplayRestoreRoutine);

        gameplayRestoreRoutine = StartCoroutine(RestoreGameplayNextFrames());
    }

    private IEnumerator RestoreGameplayNextFrames()
    {
        ForceGameplayReady();
        yield return null;
        ForceGameplayReady();
        yield return new WaitForEndOfFrame();
        ForceGameplayReady();
        gameplayRestoreRoutine = null;
    }

    private void ForceGameplayReady()
    {
        if (IsPaused)
            return;

        ReacquireGameplayComponents();

        if (cachedThirdPersonCamera != null)
            cachedThirdPersonCamera.enabled = true;
        if (cachedPlayerMovement != null)
            cachedPlayerMovement.enabled = true;
        if (cachedPlayerAttack != null)
            cachedPlayerAttack.enabled = true;

        Time.timeScale = 1f;

        // CRITICAL FIX:
        // ThirdPersonCamera checks its own private "cursorLocked" bool before reading Mouse.delta.
        SetCameraInternalCursorLock(true);

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        bool internalLocked = ReadCameraInternalCursorLock();

        Debug.Log(
            $"[ORIGIN UI] HARD MouseLook restore. " +
            $"Camera={(cachedThirdPersonCamera != null ? "ON" : "NOT FOUND")}, " +
            $"Movement={(cachedPlayerMovement != null ? "ON" : "NOT FOUND")}, " +
            $"Attack={(cachedPlayerAttack != null ? "ON" : "NOT FOUND")}, " +
            $"Cursor={Cursor.lockState}, CameraFlag={internalLocked}");
    }

    private void SetCameraInternalCursorLock(bool locked)
    {
        if (cachedThirdPersonCamera == null)
            return;

        if (cameraCursorLockedField == null)
        {
            cameraCursorLockedField = typeof(ThirdPersonCamera).GetField(
                "cursorLocked",
                BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
        }

        if (cameraCursorLockedField != null && cameraCursorLockedField.FieldType == typeof(bool))
        {
            cameraCursorLockedField.SetValue(cachedThirdPersonCamera, locked);
        }
        else
        {
            Debug.LogWarning(
                "[ORIGIN UI] ThirdPersonCamera.cursorLocked field was not found. " +
                "If mouse look still fails, send ThirdPersonCamera.cs.");
        }
    }

    private bool ReadCameraInternalCursorLock()
    {
        if (cachedThirdPersonCamera == null)
            return false;

        if (cameraCursorLockedField == null)
        {
            cameraCursorLockedField = typeof(ThirdPersonCamera).GetField(
                "cursorLocked",
                BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
        }

        if (cameraCursorLockedField != null && cameraCursorLockedField.FieldType == typeof(bool))
            return (bool)cameraCursorLockedField.GetValue(cachedThirdPersonCamera);

        return false;
    }

    private void PrepareMenuForMouse(GameObject menuRoot)
    {
        if (menuRoot == null) return;

        menuRoot.transform.SetAsLastSibling();

        CanvasGroup group = GetOrAddCanvasGroup(menuRoot);
        group.interactable = true;
        group.blocksRaycasts = true;

        foreach (Button button in menuRoot.GetComponentsInChildren<Button>(true))
        {
            button.interactable = true;
            foreach (Graphic graphic in button.GetComponentsInChildren<Graphic>(true))
                graphic.raycastTarget = true;
        }
    }

    private void ConfigureMenuButtons(GameObject menuRoot)
    {
        if (menuRoot == null) return;

        foreach (Button button in menuRoot.GetComponentsInChildren<Button>(true))
        {
            if (button == null) continue;

            TMP_Text label = button.GetComponentInChildren<TMP_Text>(true);
            string text = label != null
                ? label.text.Trim().ToUpperInvariant()
                : button.name.Trim().ToUpperInvariant();

            button.onClick.RemoveAllListeners();

            if (text.Contains("CONTINUE") || text.Contains("RESUME"))
                button.onClick.AddListener(Resume);
            else if (text.Contains("RESTART") || text.Contains("RETRY"))
                button.onClick.AddListener(RestartScene);
            else if (text.Contains("RETURN TO VILLAGE") ||
                     text.Contains("BACK TO TITLE") ||
                     button.name == "TitleButton")
            {
                if (IsDungeonScene())
                {
                    if (label != null) label.text = "RETURN TO VILLAGE";
                    button.onClick.AddListener(ReturnToVillage);
                }
                else
                {
                    if (label != null) label.text = "BACK TO TITLE";
                    button.onClick.AddListener(ReturnToTitle);
                }
            }
            else if (text.Contains("START"))
                button.onClick.AddListener(StartFromTitle);
            else if (text.Contains("QUIT") || text.Contains("EXIT"))
                button.onClick.AddListener(QuitGame);
        }
    }

    private void DisableGameplayInput()
    {
        if (cachedThirdPersonCamera != null) cachedThirdPersonCamera.enabled = false;
        if (cachedPlayerMovement != null) cachedPlayerMovement.enabled = false;
        if (cachedPlayerAttack != null) cachedPlayerAttack.enabled = false;
    }

    private void ReacquireGameplayComponents()
    {
#if UNITY_6000_0_OR_NEWER
        cachedThirdPersonCamera = Object.FindAnyObjectByType<ThirdPersonCamera>(FindObjectsInactive.Include);
        cachedPlayerMovement = Object.FindAnyObjectByType<PlayerMovement>(FindObjectsInactive.Include);
        cachedPlayerAttack = Object.FindAnyObjectByType<PlayerAttack>(FindObjectsInactive.Include);
#else
        cachedThirdPersonCamera = Object.FindObjectOfType<ThirdPersonCamera>(true);
        cachedPlayerMovement = Object.FindObjectOfType<PlayerMovement>(true);
        cachedPlayerAttack = Object.FindObjectOfType<PlayerAttack>(true);
#endif
    }

    private void RestoreHudRaycasts()
    {
        if (hudCanvasGroup == null) return;
        hudCanvasGroup.blocksRaycasts = hudOldBlocksRaycasts;
        hudCanvasGroup.interactable = hudOldInteractable;
    }

    private CanvasGroup GetOrAddCanvasGroup(GameObject go)
    {
        CanvasGroup group = go.GetComponent<CanvasGroup>();
        if (group == null) group = go.AddComponent<CanvasGroup>();
        return group;
    }

    private void KeepEventSystemAlive()
    {
        EventSystem[] systems;
#if UNITY_6000_0_OR_NEWER
        systems = Object.FindObjectsByType<EventSystem>(FindObjectsInactive.Include, FindObjectsSortMode.None);
#else
        systems = Object.FindObjectsOfType<EventSystem>(true);
#endif

        if (persistentEventSystem == null)
        {
            foreach (EventSystem es in systems)
            {
                if (es == null) continue;

                persistentEventSystem = es;

                if (es.transform.parent != null)
                    es.transform.SetParent(null);

                DontDestroyOnLoad(es.gameObject);
                Debug.Log("[ORIGIN UI] EventSystem is now persistent.");
                break;
            }
        }
    }

    private void DestroyDuplicateEventSystems()
    {
        if (persistentEventSystem == null)
        {
            KeepEventSystemAlive();
            return;
        }

        EventSystem[] systems;
#if UNITY_6000_0_OR_NEWER
        systems = Object.FindObjectsByType<EventSystem>(FindObjectsInactive.Include, FindObjectsSortMode.None);
#else
        systems = Object.FindObjectsOfType<EventSystem>(true);
#endif

        foreach (EventSystem es in systems)
        {
            if (es == null || es == persistentEventSystem) continue;
            Destroy(es.gameObject);
        }
    }

    private bool IsDungeonScene()
    {
        return SceneManager.GetActiveScene().name == dungeonSceneName;
    }

    private void UnlockCursorForUI()
    {
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    private void SelectButton(Button button)
    {
        if (button == null || EventSystem.current == null) return;
        EventSystem.current.SetSelectedGameObject(null);
        EventSystem.current.SetSelectedGameObject(button.gameObject);
    }
}
