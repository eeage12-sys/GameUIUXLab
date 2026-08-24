using UnityEngine;
using UnityEngine.SceneManagement;

[DefaultExecutionOrder(-100)]
public class OriginUISceneContext : MonoBehaviour
{
    [Header("References")]
    public OriginUIState state;

    [Header("Scene Names")]
    public string hubSceneName = "Hub_Field_Lightweight_V2";
    public string dungeonSceneName = "NGF_CompactDungeon";

    [Header("Scene UI Rules")]
    public bool persistAcrossScenes = true;
    public bool autoConfigureObjective = true;
    [Min(0f)] public float dungeonTimeLimit = 180f;
    [Min(0)] public int dungeonEnemyCount = 5;

    private static OriginUISceneContext instance;

    private void Awake()
    {
        if (state == null) state = GetComponent<OriginUIState>();

        if (persistAcrossScenes)
        {
            if (instance != null && instance != this)
            {
                Destroy(gameObject);
                return;
            }

            instance = this;
            DontDestroyOnLoad(gameObject);
        }

        SceneManager.sceneLoaded += OnSceneLoaded;
        ApplyScene(SceneManager.GetActiveScene().name, true);
    }

    private void OnDestroy()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
        if (instance == this) instance = null;
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        ApplyScene(scene.name, true);
    }

    public void ApplyCurrentScene()
    {
        ApplyScene(SceneManager.GetActiveScene().name, false);
    }

    private void ApplyScene(string sceneName, bool resetDungeon)
    {
        if (state == null) return;

        bool isDungeon = string.Equals(sceneName, dungeonSceneName, System.StringComparison.Ordinal);
        state.dungeonInfoVisible = isDungeon;
        state.timerRunning = isDungeon;

        if (isDungeon)
        {
            state.regionText = "DUNGEON  /  HUNT";
            if (resetDungeon) state.ResetDungeonProgress(dungeonTimeLimit, dungeonEnemyCount);
            if (autoConfigureObjective) state.SetObjective("Defeat all monsters", "");
        }
        else
        {
            state.regionText = "FIELD  /  VILLAGE";
            if (autoConfigureObjective) state.SetObjective("Talk to the village resident", "138m");
        }
    }
}
