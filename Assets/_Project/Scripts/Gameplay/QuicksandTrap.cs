using PurrNet;
using UnityEngine;

// Quicksand state for one body (server-held, synced). Any body standing in a Quicksand trigger gets
// trapped: it sinks slowly, can look / shoot / throw but not move or press E, and dies when the death
// timer runs out. There is no self-escape.
// - A teammate looks at the trapped body within reach and holds E (started through NetTrap.Interact,
//   the body's E handler). The timer pauses while they hold; letting go, looking away, leaving reach or
//   being blocked (e.g. ridden) resets the hold. Finishing pulls the body out next to the helper and
//   frees it from a net too.
// - A trapped carrier drops its monkey; a trapped hunter loses its rider (RideMount.IsDownServer); a
//   trapped body can't be picked up. Caged, carried and head-riding bodies are never trapped.
// - The owner holds its body still (kinematic) and lowers it as time runs out, so its view sinks too.
public class QuicksandTrap : NetworkBehaviour {

    [SerializeField]
    private PlayerNetworkController player;

    [SerializeField]
    private PlayerInfo info;

    [SerializeField]
    private PlayerHealth health;

    [SerializeField]
    private NetTrap net;

    [SerializeField]
    private Rigidbody body;

    [SerializeField, Min(1f)]
    private float deathSeconds = 10f;

    // How far the body has sunk when the timer runs out (hunters 1.2 m, monkeys 0.6 m).
    [SerializeField, Min(0f)]
    private float sinkDepth = 1.2f;

    [SerializeField, Min(0.1f)]
    private float helpSeconds = 5f;

    [SerializeField, Min(0.5f)]
    private float helpReach = 2.5f;

    // A pulled-out body lands this far to the side of the helper.
    [SerializeField]
    private float pullOutSide = 0.9f;

    // After being pulled out the server won't trap the body again for this long (its position needs a
    // moment to arrive from its owner).
    [SerializeField]
    private float retrapImmunity = 1.5f;

    // Root height above the ground when standing (bodies float on their spring). A body trapped in the
    // air (e.g. dropping onto the sand) is first set down to this height, then sinks from there.
    [SerializeField]
    private float standHeight = 0.5f;

    [SerializeField]
    private LayerMask groundLayers = ~0;

    [SerializeField]
    private SyncVar<bool> trapped = new(false);

    [SerializeField]
    private SyncVar<float> timeLeft = new(0f, 0.25f);

    [SerializeField]
    private SyncVar<bool> helped = new(false);

    // The help this machine is doing right now (one at a time).
    private static QuicksandTrap activeHelp;
    private static PlayerNetworkController activeHelper;
    private static float helpProgress;

    // Every peer: counted down locally between syncs.
    private float localTimeLeft;

    // Server only.
    private float timeLeftServer;
    private float immuneUntilServer;
    private PlayerNetworkController helperServer;
    private MonkeyRide ownRide;
    private HunterCarry ownCarry;

    // Owner only.
    private bool glued;
    private Vector3 trapPoint;

    public bool IsTrapped => trapped.value;
    public bool IsBeingHelped => helped.value;
    public float TimeLeft => localTimeLeft;
    public float DeathSeconds => deathSeconds;

    // This machine's help in progress, if any, and how far along it is (0-1).
    public static QuicksandTrap ActiveHelp => activeHelp;
    public static float ActiveHelpProgress => activeHelp ? Mathf.Clamp01(helpProgress / activeHelp.helpSeconds) : 0f;

    private void Awake() {
        TryGetComponent(out ownRide);
        TryGetComponent(out ownCarry);
        timeLeft.onChanged += TimeSynced;
        trapped.onChanged += TrappedChanged;
    }

    protected override void OnDestroy() {
        base.OnDestroy();
        timeLeft.onChanged -= TimeSynced;
        trapped.onChanged -= TrappedChanged;
        if (activeHelp == this) {
            EndLocalHelp();
        }
    }

    // Server: called by Quicksand while this body is in its trigger.
    public void TryTrapServer() {
        if (!isServer || trapped.value || Time.time < immuneUntilServer || (health && health.IsDead)) {
            return;
        }
        if (net && (net.State == NetState.Caged || net.State == NetState.Carried)) {
            return;
        }
        if (ownRide && ownRide.IsRiding) {
            return;
        }
        timeLeftServer = deathSeconds;
        timeLeft.value = deathSeconds;
        localTimeLeft = deathSeconds;
        helperServer = null;
        helped.value = false;
        trapped.value = true;
        // A trapped carrier drops the monkey it carries (still netted).
        if (ownCarry) {
            ownCarry.DropServer();
        }
    }

    private void Update() {
        if (isServer) {
            UpdateServer();
        } else if (trapped.value && !helped.value) {
            localTimeLeft = Mathf.Max(0f, localTimeLeft - Time.deltaTime);
        }
        if (activeHelp == this) {
            UpdateLocalHelp();
        }
        if (isOwner) {
            player.SetStatusBlock(this, trapped.value
                ? PlayerNetworkController.StatusBlock.Movement | PlayerNetworkController.StatusBlock.Interact
                : PlayerNetworkController.StatusBlock.None);
        }
    }

    private void UpdateServer() {
        if (!trapped.value) {
            return;
        }
        // The helper let go, left the game or can't help any more (a destroyed helper reads as null).
        if (helped.value && (!helperServer || !HelperStillValid(helperServer))) {
            ClearHelperServer();
        }
        if (!helped.value) {
            timeLeftServer = Mathf.Max(0f, timeLeftServer - Time.deltaTime);
            timeLeft.value = timeLeftServer;
        }
        localTimeLeft = timeLeftServer;
        if (timeLeftServer <= 0f && health) {
            health.Kill();
        }
    }

    // Owner: hold the body still and lower it as the timer runs out.
    private void LateUpdate() {
        if (!isOwner || !body) {
            return;
        }
        if (trapped.value) {
            if (!glued) {
                glued = true;
                trapPoint = GroundedTrapPoint(body.position);
                if (!body.isKinematic) {
                    body.linearVelocity = Vector3.zero;
                }
                body.isKinematic = true;
            }
            var sunk = sinkDepth * Mathf.Clamp01(1f - localTimeLeft / deathSeconds);
            var point = trapPoint + Vector3.down * sunk;
            body.position = point;
            transform.position = point;
        } else if (glued) {
            // Freed without the pull-out message (e.g. debug): let go at the surface, not sunk.
            Unglue(trapPoint);
        }
    }

    // No higher than standing height above the ground below (other bodies and triggers ignored).
    private Vector3 GroundedTrapPoint(Vector3 position) {
        var mask = groundLayers & ~(1 << gameObject.layer);
        if (Physics.Raycast(position + Vector3.up * 0.5f, Vector3.down, out var hit, 8f, mask,
                QueryTriggerInteraction.Ignore)) {
            position.y = Mathf.Min(position.y, hit.point.y + standHeight);
        }
        return position;
    }

    private void Unglue(Vector3 point) {
        glued = false;
        body.position = point;
        transform.position = point;
        body.isKinematic = false;
        body.linearVelocity = Vector3.zero;
    }

    // Helper's machine: E on this trapped body (from NetTrap.Interact). True when it's a teammate's help
    // (started or already running), so the E isn't used for anything else.
    public bool TryBeginHelp(PlayerNetworkController helper) {
        if (!trapped.value || !helper || helper == player || !info
            || !helper.TryGetComponent(out PlayerInfo helperInfo) || helperInfo.Team != info.Team) {
            return false;
        }
        if (activeHelp) {
            return true;
        }
        activeHelp = this;
        activeHelper = helper;
        helpProgress = 0f;
        StartHelp(helper);
        return true;
    }

    // Helper's machine: keep holding E, looking at the body, within reach and unblocked.
    private void UpdateLocalHelp() {
        var helper = activeHelper;
        if (!helper || !trapped.value || helper.ActionsBlocked || !helper.TryGetComponent(out PlayerInputReader input)
            || !input.InteractHeld || !IsLookedAtBy(helper)) {
            StopHelp();
            EndLocalHelp();
            return;
        }
        helpProgress += Time.deltaTime;
        if (helpProgress >= helpSeconds) {
            FinishHelp();
            EndLocalHelp();
        }
    }

    private bool IsLookedAtBy(PlayerNetworkController helper) {
        if (Vector3.Distance(helper.transform.position, transform.position) > helpReach
            || !helper.TryGetComponent(out MouseAimController aim)) {
            return false;
        }
        if (!Physics.Raycast(aim.GetCameraRay(), out var hit, helpReach + 1.5f, ~0, QueryTriggerInteraction.Ignore)) {
            return false;
        }
        return hit.collider.GetComponentInParent<QuicksandTrap>() == this;
    }

    private static void EndLocalHelp() {
        activeHelp = null;
        activeHelper = null;
        helpProgress = 0f;
    }

    [ServerRpc(requireOwnership: false)]
    private void StartHelp(NetworkIdentity helperIdentity, RPCInfo rpc = default) {
        var helper = helperIdentity ? helperIdentity.GetComponent<PlayerNetworkController>() : null;
        if (!trapped.value || helperServer || !helper || !helper.owner.HasValue || helper.owner.Value != rpc.sender
            || !HelperStillValid(helper)) {
            return;
        }
        helperServer = helper;
        helped.value = true;
    }

    [ServerRpc(requireOwnership: false)]
    private void StopHelp(RPCInfo rpc = default) {
        if (helperServer && helperServer.owner.HasValue && helperServer.owner.Value == rpc.sender) {
            ClearHelperServer();
        }
    }

    [ServerRpc(requireOwnership: false)]
    private void FinishHelp(RPCInfo rpc = default) {
        var helper = helperServer;
        if (!trapped.value || !helper || !helper.owner.HasValue || helper.owner.Value != rpc.sender) {
            return;
        }
        PullOutServer(PullOutSpot(helper));
    }

    // Server: a teammate, alive, close enough and not netted / asleep / tripped / ridden / trapped.
    private bool HelperStillValid(PlayerNetworkController helper) {
        if (!helper || helper == player || !helper.TryGetComponent(out PlayerInfo helperInfo) || !info
            || helperInfo.Team != info.Team) {
            return false;
        }
        if (Vector3.Distance(helper.transform.position, transform.position) > helpReach + 1f) {
            return false;
        }
        var down = (helper.TryGetComponent(out NetTrap helperNet) && helperNet.IsTrapped)
                   || (helper.TryGetComponent(out TranqStatus helperTranq) && helperTranq.IsAsleep)
                   || (helper.TryGetComponent(out TripStatus helperTrip) && helperTrip.IsTrippedServer)
                   || (helper.TryGetComponent(out PlayerHealth helperHealth) && helperHealth.IsDead)
                   || (helper.TryGetComponent(out QuicksandTrap helperSand) && helperSand.IsTrapped)
                   || (helper.owner.HasValue && helper.GetComponent<RideMount>() && MonkeyRide.FindRiderOf(helper.owner.Value));
        return !down;
    }

    private void ClearHelperServer() {
        helperServer = null;
        helped.value = false;
    }

    // Server: next to the helper, on whichever side isn't quicksand (behind them as a last resort).
    private Vector3 PullOutSpot(PlayerNetworkController helper) {
        var toBody = Vector3.ProjectOnPlane(transform.position - helper.transform.position, Vector3.up);
        if (toBody.sqrMagnitude < 0.0001f) {
            toBody = Vector3.forward;
        }
        toBody.Normalize();
        var side = Vector3.Cross(Vector3.up, toBody);
        var origin = helper.transform.position;
        var candidates = new[] { origin + side * pullOutSide, origin - side * pullOutSide, origin - toBody * pullOutSide };
        foreach (var spot in candidates) {
            if (!Quicksand.IsOverAny(spot)) {
                return spot;
            }
        }
        return origin - toBody * pullOutSide;
    }

    // Server: out of the sand (and out of a net), placed at the spot by the owner.
    private void PullOutServer(Vector3 spot) {
        ClearHelperServer();
        trapped.value = false;
        immuneUntilServer = Time.time + retrapImmunity;
        if (net && net.IsInNet) {
            net.Release();
        }
        if (owner.HasValue) {
            PulledOut(owner.Value, spot);
        }
    }

    [TargetRpc]
    private void PulledOut(PlayerID target, Vector3 spot) {
        if (body && body.isKinematic && glued) {
            Unglue(spot);
        } else if (body) {
            // The "freed" sync arrived first and we already let go at the surface: move to the spot.
            body.position = spot;
            transform.position = spot;
            if (!body.isKinematic) {
                body.linearVelocity = Vector3.zero;
            }
        }
    }

    private void TimeSynced(float value) {
        if (!isServer) {
            localTimeLeft = value;
        }
    }

    private void TrappedChanged(bool value) {
        if (value && !isServer) {
            localTimeLeft = timeLeft.value > 0f ? timeLeft.value : deathSeconds;
        }
    }

    private void OnDisable() {
        if (glued && body) {
            glued = false;
            body.isKinematic = false;
        }
        if (isOwner && player) {
            player.SetStatusBlock(this, PlayerNetworkController.StatusBlock.None);
        }
    }
}
