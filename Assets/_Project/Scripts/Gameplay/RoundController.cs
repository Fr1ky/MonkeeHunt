using System;
using System.Collections.Generic;
using PurrNet;
using UnityEngine;

public enum RoundState : byte {
    Countdown,
    Playing,
    Results
}

public enum RoundResult : byte {
    None,
    HuntersWin,
    MonkeysWin,
    Draw
}

// One per round scene. Server runs Countdown -> Playing -> Results, checks win conditions,
// then sends everyone back to the lobby. Clients read the synced state and time.
//
// Win rules (GDD §4): all monkeys out -> Hunters; all hunters out -> Monkeys; timer out -> Monkeys;
// both out at once -> Draw. A team that started empty can't be eliminated (only the timer ends it).
public class RoundController : NetworkBehaviour {

    [SerializeField]
    private MatchFlowSettings settings;

    [SerializeField]
    private string lobbySceneName = "Lobby";

    [SerializeField]
    private SyncVar<RoundState> state = new(RoundState.Countdown);

    // Seconds left in the current state (countdown, round or results). Sent twice a second;
    // clients count down locally between updates.
    [SerializeField]
    private SyncVar<float> timeRemaining = new(0f, 0.5f);

    [SerializeField]
    private SyncVar<RoundResult> result = new(RoundResult.None);

    [SerializeField]
    private SyncVar<int> huntersAlive = new(0);

    [SerializeField]
    private SyncVar<int> huntersTotal = new(0);

    [SerializeField]
    private SyncVar<int> monkeysAlive = new(0);

    [SerializeField]
    private SyncVar<int> monkeysTotal = new(0);

    public static RoundController Instance { get; private set; }

    public RoundState State => state.value;
    public RoundResult Result => result.value;
    public float TimeRemaining => localTimeRemaining;
    public int HuntersAlive => huntersAlive.value;
    public int HuntersTotal => huntersTotal.value;
    public int MonkeysAlive => monkeysAlive.value;
    public int MonkeysTotal => monkeysTotal.value;

    // Fires on every peer when the state changes.
    public event Action<RoundState> OnStateChanged;

    private readonly List<(Team team, PlayerHealth health)> participants = new();
    private float localTimeRemaining;
    private bool returningToLobby;

    private void Awake() {
        Instance = this;
        state.onChanged += StateChanged;
        timeRemaining.onChanged += TimeSynced;
    }

    protected override void OnDestroy() {
        base.OnDestroy();
        if (Instance == this) {
            Instance = null;
        }
        state.onChanged -= StateChanged;
        timeRemaining.onChanged -= TimeSynced;
    }

    protected override void OnSpawned(bool asServer) {
        if (asServer) {
            EnterState(RoundState.Countdown, settings.countdownSeconds);
        }
    }

    // Server: called by TeamSpawner for every body it spawns into this round.
    public void RegisterParticipant(Team team, PlayerHealth health) {
        if (!isServer || team == Team.Unassigned) {
            return;
        }
        participants.Add((team, health));
        RecountAlive();
    }

    private void Update() {
        localTimeRemaining = Mathf.Max(0f, localTimeRemaining - Time.deltaTime);

        var local = PlayerNetworkController.Local;
        if (local) {
            local.SetMovementLocked(state.value == RoundState.Countdown);
        }

        if (!isServer) {
            return;
        }

        timeRemaining.value = localTimeRemaining;
        RecountAlive();

        switch (state.value) {
            case RoundState.Countdown:
                if (localTimeRemaining <= 0f) {
                    EnterState(RoundState.Playing, MatchSession.RoundTimeSeconds);
                }
                break;
            case RoundState.Playing:
                var outcome = CheckWin();
                if (outcome != RoundResult.None) {
                    result.value = outcome;
                    EnterState(RoundState.Results, settings.resultsSeconds);
                }
                break;
            case RoundState.Results:
                if (localTimeRemaining <= 0f && !returningToLobby) {
                    returningToLobby = true;
                    networkManager.sceneModule.LoadSceneAsync(lobbySceneName);
                }
                break;
        }
    }

    private RoundResult CheckWin() {
        var huntersOut = huntersTotal.value > 0 && huntersAlive.value == 0;
        var monkeysOut = monkeysTotal.value > 0 && monkeysAlive.value == 0;
        if (huntersOut && monkeysOut) {
            return RoundResult.Draw;
        }
        if (monkeysOut) {
            return RoundResult.HuntersWin;
        }
        if (huntersOut) {
            return RoundResult.MonkeysWin;
        }
        if (localTimeRemaining <= 0f) {
            return RoundResult.MonkeysWin;
        }
        return RoundResult.None;
    }

    // A despawned body (death, disconnect) reads as Unity-null and counts as out; so does a caged one
    // (captured). A monkey freed from its cage counts as alive again.
    private void RecountAlive() {
        int hAlive = 0, hTotal = 0, mAlive = 0, mTotal = 0;
        foreach (var (team, health) in participants) {
            var alive = health && !health.IsDead
                && !(health.TryGetComponent(out NetTrap net) && net.State == NetState.Caged);
            if (team == Team.Hunters) {
                hTotal++;
                if (alive) hAlive++;
            } else if (team == Team.Monkeys) {
                mTotal++;
                if (alive) mAlive++;
            }
        }
        huntersAlive.value = hAlive;
        huntersTotal.value = hTotal;
        monkeysAlive.value = mAlive;
        monkeysTotal.value = mTotal;
    }

    private void EnterState(RoundState newState, float duration) {
        localTimeRemaining = duration;
        timeRemaining.value = duration;
        state.value = newState;
    }

    private void StateChanged(RoundState newState) {
        OnStateChanged?.Invoke(newState);
    }

    private void TimeSynced(float value) {
        if (!isServer) {
            localTimeRemaining = value;
        }
    }
}
