using System.Collections.Generic;
using PurrNet;
using UnityEngine;

// Poop on the screen. The server tells the hit player's machine to add a blob (random spot / size);
// blobs never clear on their own. Holding Wipe (X) fades every blob together, fully clearing in
// ~wipeSeconds; letting go early leaves them partly faded. Owner-side only; others don't see it.
public class SplatStatus : NetworkBehaviour {

    public struct Blob {
        public Vector2 ViewportPosition;
        public float Size;
        public float Rotation;
        public float Alpha;
    }

    [SerializeField]
    private PlayerNetworkController player;

    [SerializeField]
    private PlayerInputReader input;

    [SerializeField, Min(1)]
    private int maxBlobs = 8;

    [SerializeField, Min(0.1f)]
    private float wipeSeconds = 2f;

    // Blob size as a fraction of screen height.
    [SerializeField]
    private Vector2 sizeRange = new Vector2(0.3f, 0.55f);

    private readonly List<Blob> blobs = new();

    public IReadOnlyList<Blob> Blobs => blobs;
    public bool IsWiping { get; private set; }

    // 0 = fully covered by the strongest blob, 1 = clean.
    public float WipeProgress {
        get {
            var max = 0f;
            foreach (var blob in blobs) {
                max = Mathf.Max(max, blob.Alpha);
            }
            return 1f - max;
        }
    }

    // Server.
    public void AddSplatServer() {
        if (isServer && owner.HasValue) {
            AddSplat(owner.Value);
        }
    }

    [TargetRpc]
    private void AddSplat(PlayerID target) {
        if (blobs.Count >= maxBlobs) {
            blobs.RemoveAt(0);
        }
        blobs.Add(new Blob {
            ViewportPosition = new Vector2(Random.Range(0.2f, 0.8f), Random.Range(0.2f, 0.8f)),
            Size = Random.Range(sizeRange.x, sizeRange.y),
            Rotation = Random.Range(0f, 360f),
            Alpha = 1f
        });
    }

    private void Update() {
        if (!isOwner) {
            return;
        }
        IsWiping = blobs.Count > 0 && input.WipeHeld && !player.WipeBlocked;
        if (!IsWiping) {
            return;
        }
        var fade = Time.deltaTime / wipeSeconds;
        for (int i = blobs.Count - 1; i >= 0; i--) {
            var blob = blobs[i];
            blob.Alpha -= fade;
            if (blob.Alpha <= 0f) {
                blobs.RemoveAt(i);
            } else {
                blobs[i] = blob;
            }
        }
    }
}
