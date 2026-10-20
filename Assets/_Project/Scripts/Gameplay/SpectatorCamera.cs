using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

// Local only, one per round scene. Takes over whenever this machine has no living body:
// - dead players follow living teammates,
// - players who never had a body this round (unassigned, late joiners) can follow anyone.
// Mouse orbits the target, left/right click cycles. No valid target -> fixed overview point.
public class SpectatorCamera : MonoBehaviour {

    [SerializeField]
    private Camera spectatorCamera;

    [SerializeField]
    private Transform overviewPoint;

    [SerializeField]
    private float distance = 4f;

    [SerializeField]
    private float pivotHeight = 1.5f;

    [SerializeField]
    private float orbitSensitivity = 0.15f;

    [SerializeField]
    private float collisionRadius = 0.2f;

    [SerializeField]
    private LayerMask collisionLayers;

    private readonly List<PlayerInfo> targets = new();
    private PlayerInfo current;
    private int currentIndex;
    private bool hadBody;
    private Team ownTeam = Team.Unassigned;
    private float yaw;
    private float pitch = 20f;

    public bool IsSpectating => spectatorCamera.enabled;

    private void Awake() {
        SetActive(false);
    }

    private void LateUpdate() {
        var local = PlayerNetworkController.Local;
        if (local && local.gameObject.scene == gameObject.scene) {
            // Remember which side this machine played on, for who it may follow after dying.
            hadBody = true;
            var info = local.GetComponent<PlayerInfo>();
            ownTeam = info ? info.Team : Team.Unassigned;
            if (IsSpectating) {
                SetActive(false);
            }
            return;
        }

        if (!IsSpectating) {
            SetActive(true);
        }

        RefreshTargets();
        HandleInput();

        if (current) {
            FollowCurrent();
        } else if (overviewPoint) {
            spectatorCamera.transform.SetPositionAndRotation(overviewPoint.position, overviewPoint.rotation);
        }
    }

    private void RefreshTargets() {
        targets.Clear();
        foreach (var info in FindObjectsByType<PlayerInfo>()) {
            if (info.gameObject.scene != gameObject.scene || info.isOwner) {
                continue;
            }
            var health = info.GetComponent<PlayerHealth>();
            if (health && health.IsDead) {
                continue;
            }
            if (hadBody && info.Team != ownTeam) {
                continue;
            }
            targets.Add(info);
        }
        // Stable order so click-to-cycle walks players predictably.
        targets.Sort((a, b) => OwnerKey(a).CompareTo(OwnerKey(b)));

        if (!current || !targets.Contains(current)) {
            current = targets.Count > 0 ? targets[Mathf.Clamp(currentIndex, 0, targets.Count - 1)] : null;
        }
        currentIndex = current ? targets.IndexOf(current) : 0;
    }

    private static ulong OwnerKey(PlayerInfo info) {
        return info.owner.HasValue ? (ulong)info.owner.Value.id : ulong.MaxValue;
    }

    private void HandleInput() {
        var mouse = Mouse.current;
        if (mouse == null || EscMenu.IsOpen) {
            return;
        }
        var delta = mouse.delta.ReadValue();
        yaw += delta.x * orbitSensitivity;
        pitch = Mathf.Clamp(pitch - delta.y * orbitSensitivity, -30f, 75f);

        if (targets.Count == 0) {
            return;
        }
        if (mouse.leftButton.wasPressedThisFrame) {
            currentIndex = (currentIndex + 1) % targets.Count;
            current = targets[currentIndex];
        } else if (mouse.rightButton.wasPressedThisFrame) {
            currentIndex = (currentIndex - 1 + targets.Count) % targets.Count;
            current = targets[currentIndex];
        }
    }

    private void FollowCurrent() {
        var pivot = current.transform.position + Vector3.up * pivotHeight;
        var rotation = Quaternion.Euler(pitch, yaw, 0f);
        var direction = rotation * Vector3.back;
        var dist = distance;
        if (Physics.SphereCast(pivot, collisionRadius, direction, out var hit, distance, collisionLayers, QueryTriggerInteraction.Ignore)) {
            dist = hit.distance;
        }
        spectatorCamera.transform.SetPositionAndRotation(pivot + direction * dist, rotation);
    }

    private void SetActive(bool active) {
        spectatorCamera.enabled = active;
        if (spectatorCamera.TryGetComponent(out AudioListener listener)) {
            listener.enabled = active;
        }
        // Dying with the Esc menu open keeps the cursor free for the menu (it locks again on close).
        if (active && !EscMenu.IsOpen) {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }
    }
}
