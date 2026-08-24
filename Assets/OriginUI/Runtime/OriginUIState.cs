using UnityEngine;

public class OriginUIState : MonoBehaviour
{
    [Header("Player")]
    [Min(1f)] public float maxHP = 3456f;
    [Min(0f)] public float currentHP = 3456f;
    [Min(1)] public int playerLevel = 1;

    [Header("Region")]
    public string regionText = "FIELD  /  VILLAGE";

    [Header("Objective")]
    public string objectiveTitle = "CURRENT OBJECTIVE";
    public string objectiveText = "Talk to the village resident";
    public string objectiveDistance = "138m";

    [Header("Dungeon / Hunt")]
    public bool dungeonInfoVisible = false;
    public bool timerRunning = false;
    [Min(0f)] public float remainingTime = 180f;
    [Min(0)] public int defeatedEnemies = 0;
    [Min(0)] public int totalEnemies = 5;

    [Header("Interaction")]
    public bool interactionVisible = false;
    public string interactionKey = "E";
    public string interactionText = "Interact";

    public float HP01 => maxHP <= 0f ? 0f : Mathf.Clamp01(currentHP / maxHP);

    private void Update()
    {
        // Time.deltaTime intentionally respects Time.timeScale, so the dungeon timer pauses with ESC.
        if (timerRunning && remainingTime > 0f)
            remainingTime = Mathf.Max(0f, remainingTime - Time.deltaTime);
    }

    public void SetHP(float current, float max)
    {
        maxHP = Mathf.Max(1f, max);
        currentHP = Mathf.Clamp(current, 0f, maxHP);
    }

    public void Damage(float amount) => SetHP(currentHP - Mathf.Abs(amount), maxHP);
    public void Heal(float amount) => SetHP(currentHP + Mathf.Abs(amount), maxHP);

    public void SetObjective(string body, string distance = "")
    {
        objectiveText = body;
        objectiveDistance = distance;
    }

    public void SetEnemyProgress(int defeated, int total)
    {
        totalEnemies = Mathf.Max(0, total);
        defeatedEnemies = Mathf.Clamp(defeated, 0, totalEnemies);
    }

    public void ResetDungeonProgress(float seconds = 180f, int total = 5)
    {
        remainingTime = Mathf.Max(0f, seconds);
        totalEnemies = Mathf.Max(0, total);
        defeatedEnemies = 0;
    }

    public void SetInteraction(bool visible, string text = "Interact", string key = "E")
    {
        interactionVisible = visible;
        interactionText = text;
        interactionKey = key;
    }
}
