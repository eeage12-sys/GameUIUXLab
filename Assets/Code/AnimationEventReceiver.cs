using UnityEngine;

public class AnimationEventReceiver : MonoBehaviour
{
    // Imported animation packs often contain footstep Animation Events.
    // These empty receivers prevent "has no receiver" Console errors
    // until we add real footstep sounds/effects later.

    public void FootR()
    {
    }

    public void FootL()
    {
    }
}
