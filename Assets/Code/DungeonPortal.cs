using UnityEngine;
using UnityEngine.SceneManagement;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

/// <summary>
/// Hub portal trigger -> dungeon scene loader.
/// Robust player detection for the current Rigidbody + PlayerMovement setup.
/// </summary>
public class DungeonPortal : MonoBehaviour
{
    [Header("Scene")]
    [SerializeField] private string dungeonSceneName = "NGF_CompactDungeon";

    [Header("Interaction UI")]
    [SerializeField] private string interactionKey = "E";
    [SerializeField] private string interactionText = "Enter Dungeon";

    [Header("Detection")]
    [SerializeField] private string playerObjectName = "Player";
    [SerializeField] private bool debugLogs = true;

    private bool playerInside;
    private bool loading;
    private OriginUIState uiState;

    private void Awake()
    {
        TryFindUIState();

        Collider trigger = GetComponent<Collider>();
        if (trigger != null)
            trigger.isTrigger = true;
    }

    private void Update()
    {
        if (!playerInside || loading)
            return;

        bool pressed = false;
#if ENABLE_INPUT_SYSTEM
        pressed = Keyboard.current != null && Keyboard.current.eKey.wasPressedThisFrame;
#else
        pressed = Input.GetKeyDown(KeyCode.E);
#endif

        if (pressed)
        {
            if (debugLogs)
                Debug.Log("[DungeonPortal] E pressed. Loading dungeon...", this);
            EnterDungeon();
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!IsPlayer(other))
            return;

        SetPlayerInside(true);

        if (debugLogs)
            Debug.Log($"[DungeonPortal] Player entered trigger: {other.name}", this);
    }

    // Fallback: if Enter is missed for any reason, Stay restores the state.
    private void OnTriggerStay(Collider other)
    {
        if (playerInside || !IsPlayer(other))
            return;

        SetPlayerInside(true);

        if (debugLogs)
            Debug.Log($"[DungeonPortal] Player detected by TriggerStay: {other.name}", this);
    }

    private void OnTriggerExit(Collider other)
    {
        if (!IsPlayer(other))
            return;

        SetPlayerInside(false);

        if (debugLogs)
            Debug.Log($"[DungeonPortal] Player exited trigger: {other.name}", this);
    }

    private bool IsPlayer(Collider other)
    {
        if (other == null)
            return false;

        // Primary check: our actual player controller can be on this collider or a parent.
        if (other.GetComponentInParent<PlayerMovement>() != null)
            return true;

        // Fallback for the current hierarchy where the root object is named Player.
        Transform root = other.transform.root;
        return root != null && root.name == playerObjectName;
    }

    private void SetPlayerInside(bool inside)
    {
        playerInside = inside;
        TryFindUIState();

        if (uiState != null)
        {
            if (inside)
                uiState.SetInteraction(true, interactionText, interactionKey);
            else
                uiState.SetInteraction(false);
        }
    }

    private void TryFindUIState()
    {
        if (uiState == null)
            uiState = Object.FindAnyObjectByType<OriginUIState>();
    }

    private void EnterDungeon()
    {
        if (string.IsNullOrWhiteSpace(dungeonSceneName))
        {
            Debug.LogError("[DungeonPortal] Dungeon Scene Name is empty.", this);
            return;
        }

        if (!Application.CanStreamedLevelBeLoaded(dungeonSceneName))
        {
            Debug.LogError($"[DungeonPortal] Scene '{dungeonSceneName}' is not enabled in Build Profiles > Scene List.", this);
            return;
        }

        loading = true;
        if (uiState != null)
            uiState.SetInteraction(false);

        SceneManager.LoadScene(dungeonSceneName, LoadSceneMode.Single);
    }
}
