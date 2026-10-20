using UnityEngine;

// The net over a trapped body: a rope-net mesh draped from just above the head down to the feet, a bit
// gathered at the bottom, with a little sag so it doesn't look like a perfect tube. Built once in code to
// fit the body renderer it covers (hunter, monkey or test dummy). The material is a rope texture with
// holes (alpha clip, both sides), so the trapped player sees the ropes around them from inside.
[RequireComponent(typeof(MeshFilter))]
public class NetDrape : MonoBehaviour {

    // The visual body the net is sized to.
    [SerializeField]
    private Renderer fitTo;

    // Gap between the body and the net.
    [SerializeField]
    private float clearance = 0.06f;

    // Roughly the size of one net square (metres), so small and big bodies get the same mesh size.
    [SerializeField]
    private float cellSize = 0.12f;

    [SerializeField]
    private int segments = 24;

    [SerializeField]
    private int rings = 16;

    // Width at the feet relative to the widest part (gathered).
    [SerializeField, Range(0.3f, 1f)]
    private float bottomGather = 0.75f;

    [SerializeField]
    private float sag = 0.05f;

    private void Awake() {
        GetComponent<MeshFilter>().sharedMesh = Build();
    }

    // Body bounds in this object's parent space (the net hangs under the same parent as the body).
    private Bounds BodyBounds() {
        var space = transform.parent ? transform.parent : transform;
        var bounds = new Bounds(Vector3.up, Vector3.one);
        if (!fitTo || !fitTo.TryGetComponent(out MeshFilter filter) || !filter.sharedMesh) {
            return bounds;
        }
        var mesh = filter.sharedMesh.bounds;
        var first = true;
        for (int i = 0; i < 8; i++) {
            var corner = new Vector3(i % 2 == 0 ? mesh.min.x : mesh.max.x, (i / 2) % 2 == 0 ? mesh.min.y : mesh.max.y,
                i / 4 == 0 ? mesh.min.z : mesh.max.z);
            var local = space.InverseTransformPoint(fitTo.transform.TransformPoint(corner));
            if (first) {
                bounds = new Bounds(local, Vector3.zero);
                first = false;
            } else {
                bounds.Encapsulate(local);
            }
        }
        return bounds;
    }

    private Mesh Build() {
        // The mesh is built in this object's space; keep it aligned with the parent.
        transform.localPosition = Vector3.zero;
        transform.localRotation = Quaternion.identity;
        transform.localScale = Vector3.one;

        var body = BodyBounds();
        var top = body.max.y + clearance;
        var bottom = body.min.y + 0.02f;
        var radius = Mathf.Max(body.extents.x, body.extents.z) + clearance;
        var centre = new Vector3(body.center.x, 0f, body.center.z);
        var height = top - bottom;

        // Texture tiles: one square per ~cellSize around and down.
        var around = Mathf.Max(4f, Mathf.Round(2f * Mathf.PI * radius / cellSize));
        var down = Mathf.Max(2f, Mathf.Round((height + radius) / cellSize));

        var columns = segments + 1;
        var vertices = new Vector3[columns * (rings + 1)];
        var normals = new Vector3[vertices.Length];
        var uvs = new Vector2[vertices.Length];
        for (int r = 0; r <= rings; r++) {
            var t = (float)r / rings; // 0 = top, 1 = feet
            for (int s = 0; s < columns; s++) {
                var angle = (float)s / segments * Mathf.PI * 2f;
                var dir = new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle));
                var shape = Profile(t);
                var wobble = 1f + sag * Mathf.Sin(angle * 3f + t * 7f) * Mathf.Sin(t * Mathf.PI);
                var y = Mathf.Lerp(top, bottom, t);
                // A rounded top: the first rings curve in over the head.
                var index = r * columns + s;
                vertices[index] = centre + dir * (radius * shape * wobble) + Vector3.up * y;
                normals[index] = (dir + Vector3.up * (1f - shape)).normalized;
                uvs[index] = new Vector2((float)s / segments * around, t * down);
            }
        }
        var triangles = new int[segments * rings * 6];
        var k = 0;
        for (int r = 0; r < rings; r++) {
            for (int s = 0; s < segments; s++) {
                var a = r * columns + s;
                var b = a + 1;
                var c = a + columns;
                var d = c + 1;
                triangles[k++] = a;
                triangles[k++] = b;
                triangles[k++] = c;
                triangles[k++] = b;
                triangles[k++] = d;
                triangles[k++] = c;
            }
        }
        var meshOut = new Mesh { name = "NetDrape" };
        meshOut.vertices = vertices;
        meshOut.normals = normals;
        meshOut.uv = uvs;
        meshOut.triangles = triangles;
        meshOut.RecalculateBounds();
        return meshOut;
    }

    // Radius along the net (0 = top, 1 = feet): rounded over the head, full width, gathered at the feet.
    private float Profile(float t) {
        const float headPart = 0.18f;
        if (t < headPart) {
            var u = 1f - t / headPart;
            return Mathf.Sqrt(Mathf.Max(0f, 1f - u * u));
        }
        if (t > 0.8f) {
            return Mathf.Lerp(1f, bottomGather, (t - 0.8f) / 0.2f);
        }
        return 1f;
    }
}
