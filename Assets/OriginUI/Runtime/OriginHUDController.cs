using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class OriginHUDController : MonoBehaviour
{
    [Header("Data")]
    public OriginUIState state;

    [Header("Region")]
    public TMP_Text regionText;

    [Header("Player HUD")]
    public Image hpFill;
    public TMP_Text hpText;
    public TMP_Text levelText;

    [Header("Objective")]
    public TMP_Text objectiveTitleText;
    public TMP_Text objectiveBodyText;
    public TMP_Text objectiveDistanceText;

    [Header("Dungeon Info")]
    public GameObject dungeonInfoRoot;
    public TMP_Text timerText;
    public TMP_Text enemyCountText;

    [Header("Interaction")]
    public GameObject interactionRoot;
    public TMP_Text interactionKeyText;
    public TMP_Text interactionBodyText;

    private void LateUpdate()
    {
        if (state == null) return;

        if (regionText != null) regionText.text = state.regionText;
        if (hpFill != null) hpFill.fillAmount = state.HP01;
        if (hpText != null) hpText.text = $"{Mathf.CeilToInt(state.currentHP)} / {Mathf.CeilToInt(state.maxHP)}";
        if (levelText != null) levelText.text = $"Lv. {state.playerLevel}";

        if (objectiveTitleText != null) objectiveTitleText.text = state.objectiveTitle;
        if (objectiveBodyText != null) objectiveBodyText.text = state.objectiveText;
        if (objectiveDistanceText != null) objectiveDistanceText.text = state.objectiveDistance;

        if (dungeonInfoRoot != null && dungeonInfoRoot.activeSelf != state.dungeonInfoVisible)
            dungeonInfoRoot.SetActive(state.dungeonInfoVisible);

        if (timerText != null)
        {
            int totalSeconds = Mathf.Max(0, Mathf.CeilToInt(state.remainingTime));
            timerText.text = $"TIME  {totalSeconds / 60:00}:{totalSeconds % 60:00}";
        }
        if (enemyCountText != null) enemyCountText.text = $"HUNT  {state.defeatedEnemies} / {state.totalEnemies}";

        if (interactionRoot != null && interactionRoot.activeSelf != state.interactionVisible)
            interactionRoot.SetActive(state.interactionVisible);
        if (interactionKeyText != null) interactionKeyText.text = state.interactionKey;
        if (interactionBodyText != null) interactionBodyText.text = state.interactionText;
    }
}
