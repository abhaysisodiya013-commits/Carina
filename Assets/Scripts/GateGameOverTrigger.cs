using MoreMountains.CorgiEngine;
using MoreMountains.Tools;
using UnityEngine;

[AddComponentMenu("Corgi Engine/Environment/Gate Game Over Trigger")]
public class GateGameOverTrigger : MonoBehaviour
{
    [Tooltip("Scene to load when the player enters this gate. If empty, the active GameManager GameOverScene is used.")]
    public string gameOverSceneName = "RetroAdventureGameOver";

    [Tooltip("If true, GameManager.Instance.GameOverScene is preferred when it is set.")]
    public bool preferGameManagerGameOverScene = true;

    protected bool _triggered;

    protected virtual void OnTriggerEnter2D(Collider2D collider)
    {
        if (_triggered || !collider.CompareTag("Player"))
        {
            return;
        }

        Character character = collider.GetComponent<Character>();
        if (character == null || character.CharacterType != Character.CharacterTypes.Player)
        {
            return;
        }

        _triggered = true;
        CorgiEngineEvent.Trigger(CorgiEngineEventTypes.GameOver);

        string targetScene = gameOverSceneName;
        if (preferGameManagerGameOverScene && GameManager.HasInstance && !string.IsNullOrEmpty(GameManager.Instance.GameOverScene))
        {
            targetScene = GameManager.Instance.GameOverScene;
        }

        if (!string.IsNullOrEmpty(targetScene))
        {
            MMSceneLoadingManager.LoadScene(targetScene);
        }
    }
}
