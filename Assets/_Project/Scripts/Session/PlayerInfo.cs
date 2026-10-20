using System;
using PurrNet;
using UnityEngine;

// Synced name + team for one player body.
// Bodies are spawned by the server and briefly owned by the host (PurrNet's Unsafe rules use
// "spawner owns by default") before ownership is handed to the real player. So:
// - the server (re)fills name/team from MatchSession whenever ownership changes, not just at spawn;
// - the owning client sends its typed name and saved hunter loadout when it gains ownership;
// - the server only accepts a name from the body's actual owner.
public class PlayerInfo : NetworkBehaviour {

    [SerializeField]
    private SyncVar<string> displayName = new("");

    [SerializeField]
    private SyncVar<Team> team = new(Team.Unassigned);

    public string DisplayName => displayName.value;
    public Team Team => team.value;

    // Fires on every peer when the name, team or ownership changes.
    public event Action OnInfoChanged;

    private void Awake() {
        displayName.onChanged += NameChanged;
        team.onChanged += TeamChanged;
    }

    protected override void OnDestroy() {
        base.OnDestroy();
        displayName.onChanged -= NameChanged;
        team.onChanged -= TeamChanged;
    }

    protected override void OnSpawned(bool asServer) {
        Refresh(asServer);
    }

    protected override void OnOwnerChanged(PlayerID? oldOwner, PlayerID? newOwner, bool asServer) {
        Refresh(asServer);
    }

    private void Refresh(bool asServer) {
        if (asServer && owner.HasValue) {
            var entry = MatchSession.GetOrAdd(owner.Value);
            displayName.value = entry.Name;
            team.value = entry.Team;
        }
        if (!asServer && isOwner) {
            SubmitName(LocalPlayerProfile.Name);
            SubmitLoadout(LocalPlayerProfile.Loadout);
        }
        OnInfoChanged?.Invoke();
    }

    [ServerRpc]
    private void SubmitName(string requestedName, RPCInfo info = default) {
        if (!owner.HasValue || info.sender != owner.Value || string.IsNullOrWhiteSpace(requestedName)) {
            return;
        }
        var trimmed = requestedName.Trim();
        if (trimmed.Length > LocalPlayerProfile.MaxNameLength) {
            trimmed = trimmed.Substring(0, LocalPlayerProfile.MaxNameLength);
        }
        MatchSession.GetOrAdd(owner.Value).Name = trimmed;
        displayName.value = trimmed;
    }

    // Owner: send the saved loadout on gaining ownership. The server keeps it for the round body and, in
    // the hub, puts it on this body too (rack picks after that go through HunterLoadout.RequestSetSlot).
    [ServerRpc]
    public void SubmitLoadout(int packedLoadout, RPCInfo info = default) {
        if (!owner.HasValue || info.sender != owner.Value || !LoadoutCode.IsValid(packedLoadout)) {
            return;
        }
        MatchSession.GetOrAdd(owner.Value).Loadout = packedLoadout;
        if (HunterLoadout.InHub && TryGetComponent(out HunterLoadout loadout)) {
            loadout.SetItems(packedLoadout);
        }
    }

    // Server only.
    public void SetTeam(Team newTeam) {
        if (!isServer || !owner.HasValue) {
            return;
        }
        MatchSession.GetOrAdd(owner.Value).Team = newTeam;
        team.value = newTeam;
    }

    private void NameChanged(string value) {
        OnInfoChanged?.Invoke();
    }

    private void TeamChanged(Team value) {
        OnInfoChanged?.Invoke();
    }
}
