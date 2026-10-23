using System.Collections.Generic;
using UnityEngine;

// Box area on the ground where one team spawns. Picks a random point that is clear of other players.
public class SpawnArea : MonoBehaviour {

    [SerializeField]
    private Team team;

    [SerializeField]
    private Vector3 size = new Vector3(16f, 0f, 8f);

    [SerializeField]
    private float spawnHeight = 0.6f;

    [SerializeField]
    private float minSpacing = 1.2f;

    [SerializeField]
    private int maxAttempts = 30;

    [SerializeField]
    private LayerMask blockingLayers;

    // Uneven ground (terrain): each point is dropped onto whatever is below it (within groundSearch).
    [SerializeField]
    private LayerMask groundLayers = 1;

    [SerializeField]
    private float groundSearch = 20f;

    public Team Team => team;

    // usedPoints: points already handed out this spawn wave (bodies there may not have moved into physics yet).
    public Vector3 PickPoint(List<Vector3> usedPoints) {
        var best = transform.position + Vector3.up * spawnHeight;
        for (int i = 0; i < maxAttempts; i++) {
            var local = new Vector3(Random.Range(-0.5f, 0.5f) * size.x, 0f, Random.Range(-0.5f, 0.5f) * size.z);
            var point = OnGround(transform.TransformPoint(local)) + Vector3.up * spawnHeight;
            best = point;
            if (IsClear(point, usedPoints)) {
                return point;
            }
        }
        return best;
    }

    private Vector3 OnGround(Vector3 point) {
        if (Physics.Raycast(point + Vector3.up * groundSearch, Vector3.down, out var hit, groundSearch * 2f, groundLayers,
                QueryTriggerInteraction.Ignore)) {
            return hit.point;
        }
        return point;
    }

    private bool IsClear(Vector3 point, List<Vector3> usedPoints) {
        foreach (var used in usedPoints) {
            if (Vector3.Distance(used, point) < minSpacing) {
                return false;
            }
        }
        return !Physics.CheckCapsule(point + Vector3.up * 0.4f, point + Vector3.up * 1.4f, 0.4f, blockingLayers, QueryTriggerInteraction.Ignore);
    }

    private void OnDrawGizmos() {
        Gizmos.color = team == Team.Hunters ? new Color(1f, 0.55f, 0.1f, 0.4f) : new Color(0.5f, 0.3f, 0.1f, 0.4f);
        Gizmos.matrix = transform.localToWorldMatrix;
        Gizmos.DrawCube(Vector3.up * 0.05f, new Vector3(size.x, 0.1f, size.z));
    }
}
