using UnityEngine;

[DefaultExecutionOrder(-900)]
public class RetroStartingWeaponsFix : MonoBehaviour
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Install()
    {
        // Starting weapons are now owned by RuntimeLevelBoundsOverride in the scene.
    }
}
