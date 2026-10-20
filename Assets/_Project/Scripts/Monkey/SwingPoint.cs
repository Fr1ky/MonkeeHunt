using System.Collections.Generic;
using UnityEngine;

// A vine anchor monkeys can grab (Jump within range) and swing from. The anchor is this transform.
public class SwingPoint : MonoBehaviour {

    private static readonly List<SwingPoint> all = new();

    public static IReadOnlyList<SwingPoint> All => all;

    public Vector3 Anchor => transform.position;

    private void OnEnable() {
        all.Add(this);
    }

    private void OnDisable() {
        all.Remove(this);
    }

    private void OnDrawGizmos() {
        Gizmos.color = new Color(0.3f, 0.8f, 0.3f);
        Gizmos.DrawWireSphere(transform.position, 0.25f);
    }
}
