using System.Collections.Generic;
using PurrNet;

public enum Team : byte {
    Unassigned,
    Hunters,
    Monkeys
}

public enum EnemyType : byte {
    Monkey
}

// Server-only state for the whole hosted session: player names, teams and match settings.
// Survives scene loads (lobby <-> round). Clients never read it; scene controllers sync what they need.
public static class MatchSession {

    public class PlayerEntry {
        public string Name;
        public Team Team;
        // Hunter loadout (LoadoutCode-packed 5 slots).
        public int Loadout = LoadoutCode.Default;
    }

    private static readonly Dictionary<PlayerID, PlayerEntry> players = new();

    public static IReadOnlyDictionary<PlayerID, PlayerEntry> Players => players;

    public static EnemyType EnemyType = EnemyType.Monkey;
    public static float RoundTimeSeconds = 600f;

    public static PlayerEntry GetOrAdd(PlayerID player) {
        if (!players.TryGetValue(player, out var entry)) {
            entry = new PlayerEntry { Name = "Player " + player.id, Team = Team.Unassigned };
            players.Add(player, entry);
        }
        return entry;
    }

    public static void Remove(PlayerID player) {
        players.Remove(player);
    }

    public static void Reset(float defaultRoundSeconds) {
        players.Clear();
        EnemyType = EnemyType.Monkey;
        RoundTimeSeconds = defaultRoundSeconds;
    }
}
