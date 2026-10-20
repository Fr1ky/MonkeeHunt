using UnityEngine;

// Tunable timings and team health for the match flow.
[CreateAssetMenu(menuName = "MonkeeHunt/Match Flow Settings", fileName = "MatchFlowSettings")]
public class MatchFlowSettings : ScriptableObject {

    [Min(0f)]
    public float countdownSeconds = 3f;

    [Min(0f)]
    public float resultsSeconds = 5f;

    [Min(60f)]
    public float defaultRoundSeconds = 600f;

    [Header("Team health")]
    [Min(1)]
    public int hunterHealth = 100;

    [Min(1)]
    public int monkeyHealth = 40;

    public int HealthFor(Team team) {
        return team == Team.Monkeys ? monkeyHealth : hunterHealth;
    }
}
