using UnityEngine;

// Marks a tree / rock as climbable by monkeys (put it on the object with the collider or a parent).
public class Climbable : MonoBehaviour {

    // Per-surface speed scale (e.g. slippery rock < 1).
    [Min(0.1f)]
    public float climbSpeedMultiplier = 1f;
}
