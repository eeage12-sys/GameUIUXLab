using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

public class OriginUIDemoKeys : MonoBehaviour
{
    public bool enableDemoKeys = true;
    public OriginUIState state;
    public OriginToastController toast;

    private void Update()
    {
        if (!enableDemoKeys || state == null) return;

#if ENABLE_INPUT_SYSTEM
        if (Keyboard.current == null) return;
        if (Keyboard.current.f6Key.wasPressedThisFrame) state.Damage(250f);
        if (Keyboard.current.f7Key.wasPressedThisFrame) state.Heal(250f);
        if (Keyboard.current.f8Key.wasPressedThisFrame)
            state.SetEnemyProgress(Mathf.Min(state.defeatedEnemies + 1, state.totalEnemies), state.totalEnemies);
        if (Keyboard.current.f9Key.wasPressedThisFrame && toast != null) toast.Show("OBJECTIVE UPDATED");
        if (Keyboard.current.f10Key.wasPressedThisFrame) state.SetInteraction(!state.interactionVisible, "Interact", "E");
#else
        if (Input.GetKeyDown(KeyCode.F6)) state.Damage(250f);
        if (Input.GetKeyDown(KeyCode.F7)) state.Heal(250f);
        if (Input.GetKeyDown(KeyCode.F8))
            state.SetEnemyProgress(Mathf.Min(state.defeatedEnemies + 1, state.totalEnemies), state.totalEnemies);
        if (Input.GetKeyDown(KeyCode.F9) && toast != null) toast.Show("OBJECTIVE UPDATED");
        if (Input.GetKeyDown(KeyCode.F10)) state.SetInteraction(!state.interactionVisible, "Interact", "E");
#endif
    }
}
