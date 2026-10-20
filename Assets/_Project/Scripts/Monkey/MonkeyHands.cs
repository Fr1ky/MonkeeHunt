using UnityEngine;

// Head-ride view block: the rider's real hands (placeholder shapes built in code: palm, four fingers and a
// thumb each, with arms reaching down from the rider) cover the ridden hunter's eyes. Everyone sees them.
// - On the ridden hunter's own machine they sit right in front of the camera and cover most of the view
//   (sized for the player's field of view); each finger slowly spreads and closes, so gaps open and shut.
// - Everyone else sees them at monkey-hand size on the outside of the hunter's face.
public class MonkeyHands : MonoBehaviour {

    private const int FingerCount = 4;

    [SerializeField]
    private MonkeyRide ride;

    // The rider's facing; the arms start at its shoulders.
    [SerializeField]
    private Transform visualisation;

    [SerializeField]
    private Material furMaterial;

    [Header("Ridden hunter's view (head space, metres, tuned for a 75° field of view)")]
    [SerializeField]
    private float closeDistance = 0.13f;

    [SerializeField]
    private float closeSpacing = 0.066f;

    [SerializeField]
    private float closeDrop = 0.03f;

    [Header("Seen by others")]
    [SerializeField]
    private float faceDistance = 0.27f;

    [SerializeField]
    private float faceScale = 0.5f;

    [SerializeField]
    private float faceSpacing = 0.045f;

    [Header("Fingers")]
    // Extra fan-out per finger step when a finger is fully spread.
    [SerializeField]
    private float spreadDegrees = 7f;

    // Each finger spreads and closes over a period picked from this range (seconds).
    [SerializeField]
    private Vector2 rhythmSeconds = new Vector2(1f, 2f);

    [Header("Arms")]
    // Shoulder position on the rider (rider space, x mirrored per side).
    [SerializeField]
    private Vector3 shoulderOffset = new Vector3(0.1f, 0.2f, 0.05f);

    [SerializeField]
    private float armThickness = 0.035f;

    private class Hand {
        public float Side;
        public Transform Root;
        public Transform Thumb;
        public Transform Arm;
        public readonly Transform[] Fingers = new Transform[FingerCount];
        public readonly float[] Periods = new float[FingerCount];
        public readonly float[] Phases = new float[FingerCount];
    }

    private Transform rig;
    private Hand left;
    private Hand right;

    private void Awake() {
        rig = new GameObject("RiderHands").transform;
        rig.SetParent(transform, false);
        left = BuildHand(-1f);
        right = BuildHand(1f);
        rig.gameObject.SetActive(false);
    }

    private void LateUpdate() {
        var mount = ride && ride.IsRiding ? RideMount.FindByPlayer(ride.MountPlayer) : null;
        var head = mount ? mount.Head : null;
        if (!head) {
            if (rig.gameObject.activeSelf) {
                rig.gameObject.SetActive(false);
            }
            return;
        }
        if (!rig.gameObject.activeSelf) {
            rig.gameObject.SetActive(true);
        }
        rig.SetPositionAndRotation(head.position, head.rotation);

        // The ridden hunter's own view: close up, scaled to the field of view so the cover stays the same.
        var ownView = mount.isOwner;
        var scale = ownView ? FovScale(mount) : faceScale;
        var distance = ownView ? closeDistance : faceDistance;
        var spacing = ownView ? closeSpacing * scale : faceSpacing;
        var drop = ownView ? closeDrop * scale : 0f;
        Pose(left, distance, spacing, drop, scale);
        Pose(right, distance, spacing, drop, scale);
    }

    private static float FovScale(RideMount mount) {
        var cam = mount.Head ? mount.Head.GetComponentInChildren<Camera>() : null;
        var fov = cam ? cam.fieldOfView : 75f;
        return Mathf.Tan(fov * 0.5f * Mathf.Deg2Rad) / Mathf.Tan(37.5f * Mathf.Deg2Rad);
    }

    private void Pose(Hand hand, float distance, float spacing, float drop, float scale) {
        hand.Root.localPosition = new Vector3(hand.Side * spacing, -drop, distance);
        // Palms towards the face, fingers up and leaning in a little.
        hand.Root.localRotation = Quaternion.Euler(0f, 0f, -hand.Side * 8f);
        hand.Root.localScale = Vector3.one * scale;
        for (int i = 0; i < FingerCount; i++) {
            var spread = 0.5f + 0.5f * Mathf.Sin(Time.time * Mathf.PI * 2f / hand.Periods[i] + hand.Phases[i]);
            // Outer fingers fan outwards, inner ones inwards.
            var fan = (i - 1.5f) * spreadDegrees * spread;
            hand.Fingers[i].localRotation = Quaternion.Euler(0f, 0f, -hand.Side * fan);
        }
        // Arm from the rider's shoulder to the wrist.
        var shoulder = visualisation.TransformPoint(new Vector3(hand.Side * shoulderOffset.x, shoulderOffset.y, shoulderOffset.z));
        var wrist = hand.Root.TransformPoint(new Vector3(0f, -0.06f, 0f));
        var along = wrist - shoulder;
        var length = along.magnitude;
        if (length > 0.001f) {
            hand.Arm.position = (shoulder + wrist) * 0.5f;
            hand.Arm.up = along / length;
            // Unity cylinder is 2 units tall.
            hand.Arm.localScale = new Vector3(armThickness, length * 0.5f, armThickness);
        }
    }

    // Hand at "close up" size (~0.17 m wide): palm, four fingers on pivots along its top edge, a thumb
    // on the inner side. Pivots sit at the finger bases so spreading rotates them like real fingers.
    private Hand BuildHand(float side) {
        var hand = new Hand { Side = side };
        hand.Root = new GameObject(side < 0f ? "LeftHand" : "RightHand").transform;
        hand.Root.SetParent(rig, false);
        Part(PrimitiveType.Sphere, "Palm", hand.Root, Vector3.zero, new Vector3(0.125f, 0.115f, 0.035f));
        for (int i = 0; i < FingerCount; i++) {
            var pivot = new GameObject("Finger" + i).transform;
            pivot.SetParent(hand.Root, false);
            pivot.localPosition = new Vector3((i - 1.5f) * 0.03f, 0.045f, 0f);
            // Middle fingers a bit longer.
            var length = i == 1 || i == 2 ? 0.105f : 0.09f;
            Part(PrimitiveType.Capsule, "Bone", pivot, new Vector3(0f, length * 0.5f, 0f), new Vector3(0.036f, length * 0.5f, 0.036f));
            hand.Fingers[i] = pivot;
            hand.Periods[i] = Random.Range(rhythmSeconds.x, rhythmSeconds.y);
            hand.Phases[i] = Random.Range(0f, Mathf.PI * 2f);
        }
        hand.Thumb = new GameObject("Thumb").transform;
        hand.Thumb.SetParent(hand.Root, false);
        hand.Thumb.localPosition = new Vector3(-side * 0.05f, -0.01f, 0f);
        hand.Thumb.localRotation = Quaternion.Euler(0f, 0f, side * 55f);
        Part(PrimitiveType.Capsule, "Bone", hand.Thumb, new Vector3(0f, 0.035f, 0f), new Vector3(0.034f, 0.035f, 0.034f));
        hand.Arm = Part(PrimitiveType.Cylinder, side < 0f ? "LeftArm" : "RightArm", rig, Vector3.zero, Vector3.one);
        return hand;
    }

    private Transform Part(PrimitiveType type, string partName, Transform parent, Vector3 position, Vector3 scale) {
        var go = GameObject.CreatePrimitive(type);
        go.name = partName;
        Destroy(go.GetComponent<Collider>());
        go.layer = gameObject.layer;
        var t = go.transform;
        t.SetParent(parent, false);
        t.localPosition = position;
        t.localScale = scale;
        var r = go.GetComponent<MeshRenderer>();
        if (furMaterial) {
            r.sharedMaterial = furMaterial;
        }
        r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        return t;
    }
}
