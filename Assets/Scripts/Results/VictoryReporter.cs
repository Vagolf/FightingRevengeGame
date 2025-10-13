using TMPro;
using UnityEngine;
using UnityEngine.UI;

// Attach this to your Victory object. It reads name from an InputField or TMP_InputField
// and reads time from VictoryTimeDisplay (or set manually) then stores to JSON via RunResultsStore.
public class VictoryReporter : MonoBehaviour
{
    [Header("Inputs")]
    [Tooltip("Player name input (legacy UI)")] [SerializeField] private TMP_InputField nameInput;
#if TMP_PRESENT
    [Tooltip("Player name input (TextMeshPro)")] [SerializeField] private TMPro.TMP_InputField tmpNameInput;
#endif
    [Tooltip("Optional explicit difficulty string; if empty, tries to read from GameManager")] [SerializeField] private string difficultyOverride;
    [Tooltip("If set, time will be read from this component; otherwise set via API")] [SerializeField] private VictoryTimeDisplay timeDisplay;

    private float cachedSeconds = -1f;

    // Call this from a button (e.g., Save / Continue)
    public void SaveVictory()
    {
        float seconds = GetSeconds();
        string playerName = GetPlayerName();
        string difficulty = GetDifficulty();
        int sceneIndex = UnityEngine.SceneManagement.SceneManager.GetActiveScene().buildIndex;
        RunResultsStore.Ensure().AddResult(playerName, seconds, sceneIndex, difficulty);
        Debug.Log($"[VictoryReporter] Saved result: {playerName} {seconds:F2}s scene={sceneIndex} diff={difficulty}");
    }

    public void SetTimeSeconds(float seconds)
    {
        cachedSeconds = Mathf.Max(0f, seconds);
    }

    private float GetSeconds()
    {
        if (cachedSeconds >= 0f) return cachedSeconds;
        if (timeDisplay != null)
        {
            // Try to read from RoundManager static if available
            return RoundManager.LastPlayerWinTime;
        }
        return RoundManager.LastPlayerWinTime;
    }

    private string GetPlayerName()
    {
#if TMP_PRESENT
        if (tmpNameInput != null) return tmpNameInput.text;
#endif
        if (nameInput != null) return nameInput.text;
        return "Player";
    }

    private string GetDifficulty()
    {
        if (!string.IsNullOrWhiteSpace(difficultyOverride)) return difficultyOverride;
        // Try GameManagerScript if it exposes difficulty
        var gm = FindObjectOfType<GameManagerScript>();
        if (gm != null)
        {
            // If you have a property, adapt here; fallback
            return gm.gameObject.tag == "Hard" ? "Hard" : "Normal";
        }
        return "Normal";
    }
}
