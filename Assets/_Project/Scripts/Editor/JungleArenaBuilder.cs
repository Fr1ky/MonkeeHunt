using System.Collections.Generic;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

// Builds the jungle arena (M8) into the JungleArena scene from JungleArenaLayout: terrain (plateaus, ramps,
// riverbed, paint), cliff rocks, boundary wall, river water + wading zones, bridges, ladders, trees, vines,
// ruins, camp, marshes and pickups, and moves the scene's spawn areas, overview point and kill zone.
// Everything it makes lives under the scene's "Generated" root, which is rebuilt each run; anything placed
// outside that root is left alone. Same layout + seed = same map.
public static class JungleArenaBuilder {

    private const string ScenePath = "Assets/_Project/Scenes/JungleArena.unity";
    private const string SourceScenePath = "Assets/_Project/Scenes/Blockout.unity";
    private const string MapFolder = "Assets/_Project/Settings/Map";
    private const string LayoutPath = MapFolder + "/JungleArenaLayout.asset";
    private const string TerrainDataPath = MapFolder + "/JungleArenaTerrain.asset";
    private const string RiverMeshPath = MapFolder + "/JungleArenaRiver.asset";
    private const string MaterialFolder = MapFolder + "/Materials";
    private const string LayerFolder = MapFolder + "/TerrainLayers";
    private const string GroundTextureFolder = MapFolder + "/Textures";
    private const string PrefabFolder = "Assets/_Project/Prefabs/World/Arena";
    private const string LadderModelPath = "Assets/_Project/Models/Ladder.fbx";
    private const string TerrainMaterialPath = "Packages/com.unity.render-pipelines.universal/Runtime/Materials/TerrainLit.mat";

    private const float TerrainSize = 440f;
    private const float TerrainBase = -2f;
    private const float TerrainHeight = 32f;
    private const int HeightRes = 513;
    private const int AlphaRes = 512;

    // Half the width of the steep band under the cliff rocks.
    private const float CliffHalf = 1.5f;

    // Ladder.fbx: one 1.2 m segment whose mesh sits 0.88 m below its pivot.
    private const float LadderSegment = 1.2f;
    private const float LadderPivotOffset = 0.88f;

    private static readonly string[] GroundLayers = { "Grass", "Jungle", "Mud", "Sand", "Rock" };

    [MenuItem("MonkeeHunt/Build Jungle Arena")]
    public static void BuildMenu() {
        EnsureAssets();
        Debug.Log(Build());
    }

    // ---------- Assets (scene, layout, materials, terrain layers, prefabs) ----------

    // Creates whatever is missing. Safe to run any time.
    public static string EnsureAssets() {
        var log = new StringBuilder();
        Folder(MapFolder);
        Folder(MaterialFolder);
        Folder(LayerFolder);
        Folder(PrefabFolder);

        if (!AssetDatabase.LoadAssetAtPath<JungleArenaLayout>(LayoutPath)) {
            AssetDatabase.CreateAsset(ScriptableObject.CreateInstance<JungleArenaLayout>(), LayoutPath);
            log.AppendLine("layout asset");
        }

        if (!AssetDatabase.LoadAssetAtPath<Texture2D>(GroundTextureFolder + "/T_Ground_Grass.png")) {
            PlaceholderTextures.GenerateGroundTiles();
            AssetDatabase.Refresh();
            log.AppendLine("ground textures");
        }
        foreach (var name in GroundLayers) {
            var path = LayerFolder + "/TL_" + name + ".terrainlayer";
            if (AssetDatabase.LoadAssetAtPath<TerrainLayer>(path)) {
                continue;
            }
            var layer = new TerrainLayer {
                diffuseTexture = AssetDatabase.LoadAssetAtPath<Texture2D>(GroundTextureFolder + "/T_Ground_" + name + ".png"),
                tileSize = new Vector2(6f, 6f),
                smoothness = 0.05f
            };
            AssetDatabase.CreateAsset(layer, path);
            log.AppendLine("terrain layer " + name);
        }

        EnsureMaterial(log, "M_Leaves", new Color(0.18f, 0.4f, 0.14f));
        EnsureMaterial(log, "M_LeavesLight", new Color(0.27f, 0.5f, 0.19f));
        EnsureMaterial(log, "M_RockDark", new Color(0.33f, 0.31f, 0.28f));
        EnsureMaterial(log, "M_Ruin", new Color(0.6f, 0.58f, 0.52f));
        EnsureMaterial(log, "M_Wood", new Color(0.45f, 0.3f, 0.17f));
        EnsureMaterial(log, "M_Tent", new Color(0.86f, 0.46f, 0.18f));
        EnsureMaterial(log, "M_Flag", new Color(0.9f, 0.16f, 0.1f));
        EnsureMaterial(log, "M_Water", new Color(0.22f, 0.45f, 0.62f, 0.62f), true);

        Prefab(log, "Tree_Jungle", BuildJungleTree);
        Prefab(log, "Tree_Canopy", BuildCanopyTree);
        Prefab(log, "Tree_Banana", BuildBananaTree);
        Prefab(log, "Bush", BuildBush);
        Prefab(log, "Rock_Cliff", root => BuildRock(root, "M_Rock", true));
        Prefab(log, "Rock_Boundary", root => BuildRock(root, "M_RockDark", false));
        Prefab(log, "Boulder", BuildBoulder);

        if (!AssetDatabase.LoadAssetAtPath<SceneAsset>(ScenePath)) {
            CreateScene();
            log.AppendLine("scene");
        }
        AssetDatabase.SaveAssets();
        return log.Length > 0 ? "created: " + log.ToString().Replace("\n", ", ") : "assets ok";
    }

    // Copy of Blockout with its round setup (round controller, spawner, spawn areas, spectator, HUD,
    // Esc menu, kill zone) and without its test geometry.
    private static void CreateScene() {
        AssetDatabase.CopyAsset(SourceScenePath, ScenePath);
        var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
        foreach (var root in scene.GetRootGameObjects()) {
            if (root.name == "Environment" || root.name == "QuicksandPatches") {
                Object.DestroyImmediate(root);
            }
        }
        EditorSceneManager.SaveScene(scene);
        var scenes = new List<EditorBuildSettingsScene>(EditorBuildSettings.scenes);
        if (!scenes.Exists(s => s.path == ScenePath)) {
            scenes.Add(new EditorBuildSettingsScene(ScenePath, true));
            EditorBuildSettings.scenes = scenes.ToArray();
        }
    }

    private static void Folder(string path) {
        if (AssetDatabase.IsValidFolder(path)) {
            return;
        }
        var parent = System.IO.Path.GetDirectoryName(path).Replace('\\', '/');
        Folder(parent);
        AssetDatabase.CreateFolder(parent, System.IO.Path.GetFileName(path));
    }

    private static void EnsureMaterial(StringBuilder log, string name, Color color, bool transparent = false) {
        var path = MaterialFolder + "/" + name + ".mat";
        if (AssetDatabase.LoadAssetAtPath<Material>(path)) {
            return;
        }
        var mat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
        mat.SetColor("_BaseColor", color);
        mat.SetFloat("_Smoothness", transparent ? 0.85f : 0.1f);
        if (transparent) {
            mat.SetFloat("_Surface", 1f);
            mat.SetFloat("_Blend", 0f);
            mat.SetFloat("_ZWrite", 0f);
            mat.SetFloat("_SrcBlend", (float)UnityEngine.Rendering.BlendMode.SrcAlpha);
            mat.SetFloat("_DstBlend", (float)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            mat.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            mat.SetOverrideTag("RenderType", "Transparent");
            mat.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent;
        }
        mat.enableInstancing = true;
        AssetDatabase.CreateAsset(mat, path);
        log.AppendLine(name);
    }

    private static Material Mat(string name) {
        var mat = AssetDatabase.LoadAssetAtPath<Material>(MaterialFolder + "/" + name + ".mat");
        if (!mat) {
            mat = AssetDatabase.LoadAssetAtPath<Material>("Assets/_Project/Settings/" + name + ".mat");
        }
        if (!mat) {
            throw new System.Exception("Missing material " + name);
        }
        return mat;
    }

    private static void Prefab(StringBuilder log, string name, System.Action<GameObject> build) {
        var path = PrefabFolder + "/" + name + ".prefab";
        if (AssetDatabase.LoadAssetAtPath<GameObject>(path)) {
            return;
        }
        var root = new GameObject(name);
        build(root);
        PrefabUtility.SaveAsPrefabAsset(root, path);
        Object.DestroyImmediate(root);
        log.AppendLine(name);
    }

    private static GameObject LoadPrefab(string path) {
        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
        if (!prefab) {
            throw new System.Exception("Missing prefab " + path);
        }
        return prefab;
    }

    // Part of a placeholder prefab: a primitive with a material, optionally without its collider.
    private static GameObject Part(GameObject parent, string name, PrimitiveType type, Vector3 position, Vector3 scale,
        string material, bool collider) {
        var go = GameObject.CreatePrimitive(type);
        go.name = name;
        go.transform.SetParent(parent.transform, false);
        go.transform.localPosition = position;
        go.transform.localScale = scale;
        go.GetComponent<MeshRenderer>().sharedMaterial = Mat(material);
        if (!collider) {
            Object.DestroyImmediate(go.GetComponent<Collider>());
        }
        return go;
    }

    // ~8 m climbable trunk with a leaf cluster (leaves hide, don't block).
    private static void BuildJungleTree(GameObject root) {
        var trunk = Part(root, "Trunk", PrimitiveType.Cylinder, new Vector3(0f, 4f, 0f), new Vector3(0.8f, 4f, 0.8f), "M_TreeTrunk", true);
        trunk.AddComponent<Climbable>();
        Part(root, "Leaves", PrimitiveType.Sphere, new Vector3(0f, 7.6f, 0f), new Vector3(5.4f, 3.2f, 5.4f), "M_Leaves", false);
        Part(root, "LeavesTop", PrimitiveType.Sphere, new Vector3(0.6f, 9.1f, -0.4f), new Vector3(3.6f, 2.4f, 3.6f), "M_LeavesLight", false);
    }

    // ~14 m climbable trunk, two walkable perches at ~9 m (Perch_A points along +x) inside a big canopy.
    private static void BuildCanopyTree(GameObject root) {
        var trunk = Part(root, "Trunk", PrimitiveType.Cylinder, new Vector3(0f, 7f, 0f), new Vector3(1.2f, 7f, 1.2f), "M_TreeTrunk", true);
        trunk.AddComponent<Climbable>();
        Part(root, "Perch_A", PrimitiveType.Cube, new Vector3(2.1f, 9f, 0f), new Vector3(3.4f, 0.35f, 1f), "M_TreeTrunk", true);
        Part(root, "Perch_B", PrimitiveType.Cube, new Vector3(-1.9f, 9.6f, 0f), new Vector3(3f, 0.35f, 1f), "M_TreeTrunk", true);
        Part(root, "Canopy", PrimitiveType.Sphere, new Vector3(0f, 11.2f, 0f), new Vector3(9.5f, 5f, 9.5f), "M_Leaves", false);
        Part(root, "CanopyA", PrimitiveType.Sphere, new Vector3(2.4f, 13.2f, 1.2f), new Vector3(6.5f, 4f, 6.5f), "M_LeavesLight", false);
        Part(root, "CanopyB", PrimitiveType.Sphere, new Vector3(-2.6f, 12.8f, -1.6f), new Vector3(6f, 3.6f, 6f), "M_Leaves", false);
    }

    // The Blockout banana tree, with its two banana pickups.
    private static void BuildBananaTree(GameObject root) {
        Part(root, "Trunk", PrimitiveType.Cylinder, new Vector3(0f, 2f, 0f), new Vector3(0.5f, 2f, 0.5f), "M_TreeTrunk", true);
        Part(root, "Leaves", PrimitiveType.Sphere, new Vector3(0f, 4.2f, 0f), new Vector3(3f, 1.4f, 3f), "M_Bush", false);
        var banana = LoadPrefab("Assets/_Project/Prefabs/World/Pickup_Banana.prefab");
        foreach (var offset in new[] { new Vector3(1f, 0f, 0.6f), new Vector3(-0.8f, 0f, -0.9f) }) {
            var pickup = (GameObject)PrefabUtility.InstantiatePrefab(banana);
            pickup.transform.SetParent(root.transform, false);
            pickup.transform.localPosition = offset;
        }
    }

    // Low cover: hides a monkey, no collider.
    private static void BuildBush(GameObject root) {
        Part(root, "Bush", PrimitiveType.Sphere, new Vector3(0f, 0.45f, 0f), new Vector3(1.8f, 1.1f, 1.8f), "M_Bush", false);
    }

    // A unit rock block, scaled per placement (pivot in the middle).
    private static void BuildRock(GameObject root, string material, bool climbable) {
        root.AddComponent<MeshFilter>().sharedMesh = Resources.GetBuiltinResource<Mesh>("Cube.fbx");
        root.AddComponent<MeshRenderer>().sharedMaterial = Mat(material);
        root.AddComponent<BoxCollider>();
        if (climbable) {
            root.AddComponent<Climbable>();
        }
    }

    // Lumpy low-poly rock (80 faces, so it also works as a convex collider), radius 0.5 like a unit sphere.
    private static Mesh BoulderMesh() {
        var path = MapFolder + "/BoulderMesh.asset";
        var existing = AssetDatabase.LoadAssetAtPath<Mesh>(path);
        if (existing) {
            return existing;
        }
        var t = (1f + Mathf.Sqrt(5f)) * 0.5f;
        var vertices = new List<Vector3> {
            new(-1, t, 0), new(1, t, 0), new(-1, -t, 0), new(1, -t, 0), new(0, -1, t), new(0, 1, t),
            new(0, -1, -t), new(0, 1, -t), new(t, 0, -1), new(t, 0, 1), new(-t, 0, -1), new(-t, 0, 1)
        };
        var faces = new List<int> {
            0, 11, 5, 0, 5, 1, 0, 1, 7, 0, 7, 10, 0, 10, 11, 1, 5, 9, 5, 11, 4, 11, 10, 2, 10, 7, 6, 7, 1, 8,
            3, 9, 4, 3, 4, 2, 3, 2, 6, 3, 6, 8, 3, 8, 9, 4, 9, 5, 2, 4, 11, 6, 2, 10, 8, 6, 7, 9, 8, 1
        };
        // One subdivision: each triangle becomes four.
        var midpoints = new Dictionary<long, int>();
        int Mid(int a, int b) {
            var key = ((long)Mathf.Min(a, b) << 32) | (uint)Mathf.Max(a, b);
            if (!midpoints.TryGetValue(key, out var index)) {
                index = vertices.Count;
                vertices.Add((vertices[a] + vertices[b]) * 0.5f);
                midpoints[key] = index;
            }
            return index;
        }
        var subdivided = new List<int>();
        for (int i = 0; i < faces.Count; i += 3) {
            int a = faces[i], b = faces[i + 1], c = faces[i + 2];
            int ab = Mid(a, b), bc = Mid(b, c), ca = Mid(c, a);
            subdivided.AddRange(new[] { a, ab, ca, b, bc, ab, c, ca, bc, ab, bc, ca });
        }
        var rng = new System.Random(7);
        for (int i = 0; i < vertices.Count; i++) {
            vertices[i] = vertices[i].normalized * (0.5f * (0.85f + 0.3f * (float)rng.NextDouble()));
        }
        var mesh = new Mesh { name = "Boulder" };
        mesh.SetVertices(vertices);
        mesh.SetTriangles(subdivided, 0);
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();
        AssetDatabase.CreateAsset(mesh, path);
        return mesh;
    }

    private static void BuildBoulder(GameObject root) {
        var mesh = BoulderMesh();
        root.AddComponent<MeshFilter>().sharedMesh = mesh;
        root.AddComponent<MeshRenderer>().sharedMaterial = Mat("M_Rock");
        var collider = root.AddComponent<MeshCollider>();
        collider.sharedMesh = mesh;
        collider.convex = true;
        root.AddComponent<Climbable>();
    }

    // ---------- Build ----------

    public static string Build() {
        var layout = AssetDatabase.LoadAssetAtPath<JungleArenaLayout>(LayoutPath);
        if (!layout) {
            return "Run EnsureAssets first (no layout asset).";
        }
        var scene = EditorSceneManager.GetActiveScene();
        if (scene.path != ScenePath) {
            scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
        }
        foreach (var existing in scene.GetRootGameObjects()) {
            if (existing.name == "Generated") {
                Object.DestroyImmediate(existing);
            }
        }
        var root = new GameObject("Generated");
        var arena = new Arena(layout, root.transform);

        arena.BuildTerrain();
        arena.BuildCliffs();
        arena.BuildBoundary();
        arena.BuildRiver();
        arena.BuildLadders();
        arena.BuildRuins();
        arena.BuildCamp();
        arena.BuildTreesAndVines();
        arena.BuildProps();
        arena.PlaceSceneObjects(scene);
        ApplyLook(layout);

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        AssetDatabase.SaveAssets();
        return arena.Log.ToString();
    }

    private static void ApplyLook(JungleArenaLayout layout) {
        RenderSettings.fog = layout.fog;
        RenderSettings.fogMode = FogMode.Linear;
        RenderSettings.fogStartDistance = layout.fogStart;
        RenderSettings.fogEndDistance = layout.fogEnd;
        RenderSettings.fogColor = layout.fogColor;
    }

    // One build: the layout, derived geometry (smoothed river), placement bookkeeping and the parents.
    private class Arena {

        public readonly StringBuilder Log = new();

        private readonly JungleArenaLayout L;
        private readonly Transform root;
        private readonly System.Random rng;
        private readonly Vector2 noiseOffset;
        private readonly List<Vector2> river;
        private readonly List<float> riverAlong = new();
        private readonly float fordAlong;
        private readonly List<Vector2> trunks = new();
        private readonly List<Vector2> bushes = new();
        private readonly List<(Vector2 center, float radius)> keepClear = new();

        public Arena(JungleArenaLayout layout, Transform root) {
            L = layout;
            this.root = root;
            rng = new System.Random(layout.seed);
            noiseOffset = new Vector2((float)rng.NextDouble() * 1000f, (float)rng.NextDouble() * 1000f);
            river = SmoothPath(layout.river, 2f);
            var total = 0f;
            for (int i = 0; i < river.Count; i++) {
                if (i > 0) {
                    total += Vector2.Distance(river[i - 1], river[i]);
                }
                riverAlong.Add(total);
            }
            RiverDistance(layout.ford, out fordAlong, out _);
            foreach (var b in layout.boulders) {
                keepClear.Add((b.position, b.size * 0.5f + 1.5f));
            }
            foreach (var m in layout.marshes) {
                keepClear.Add((m.position, m.size * 0.5f + 1.5f));
            }
            foreach (var p in layout.rockPiles) {
                keepClear.Add((p, 2f));
            }
            foreach (var p in layout.fruitBushes) {
                keepClear.Add((p, 2f));
            }
            foreach (var p in layout.bananaTrees) {
                keepClear.Add((p, 3f));
            }
        }

        private float Rand(float min, float max) => min + (float)rng.NextDouble() * (max - min);

        private Transform Group(string name) {
            var go = new GameObject(name);
            go.transform.SetParent(root, false);
            return go.transform;
        }

        private static Vector3 At(Vector2 p, float y) => new Vector3(p.x, y, p.y);

        // ----- Height field (also used to place everything on the ground) -----

        public float Height(Vector2 p) {
            var h = 0f;
            foreach (var plateau in L.plateaus) {
                var d = PolylineDistance(p, plateau.cliffLine, out _, out _);
                var signed = InPolygon(p, plateau.outline) ? d : -d;
                h = Mathf.Max(h, plateau.height * Smooth(-CliffHalf, CliffHalf, signed));
            }
            h += Bumps(p) * BumpMask(p);
            foreach (var ramp in L.ramps) {
                var dir = ramp.high - ramp.low;
                var length = dir.magnitude;
                dir /= length;
                var t = Vector2.Dot(p - ramp.low, dir) / length;
                if (t < -0.2f || t > 1.2f) {
                    continue;
                }
                var side = Mathf.Abs(dir.x * (p.y - ramp.low.y) - dir.y * (p.x - ramp.low.x));
                var weight = (1f - Smooth(ramp.width * 0.5f, ramp.width * 0.5f + 3f, side))
                    * Smooth(-0.2f, 0f, t) * (1f - Smooth(1f, 1.2f, t));
                h = Mathf.Lerp(h, Mathf.Lerp(ramp.lowHeight, ramp.highHeight, Mathf.Clamp01(t)), weight);
            }
            var riverDistance = RiverDistance(p, out var along, out _);
            var halfWidth = L.riverWidth * 0.5f;
            if (riverDistance < halfWidth + L.bankWidth) {
                var bed = -(IsFord(along) ? L.fordDepth : L.riverDepth);
                h = Mathf.Lerp(bed, h, Smooth(halfWidth, halfWidth + L.bankWidth, riverDistance));
            }
            return Mathf.Lerp(h, L.wallHeight + 1f, Smooth(L.radius - 2f, L.radius + 6f, p.magnitude));
        }

        private float Bumps(Vector2 p) {
            var n = Mathf.PerlinNoise(p.x * L.bumpScale + noiseOffset.x, p.y * L.bumpScale + noiseOffset.y);
            return (n - 0.5f) * 2f * L.bumpHeight;
        }

        // No bumps in clearings, marshes, by the river, on ramps or near cliffs.
        private float BumpMask(Vector2 p) {
            var m = Smooth(L.riverWidth * 0.5f + L.bankWidth, L.riverWidth * 0.5f + L.bankWidth + 8f, RiverDistance(p, out _, out _));
            foreach (var plateau in L.plateaus) {
                m *= Smooth(4f, 10f, PolylineDistance(p, plateau.cliffLine, out _, out _));
            }
            foreach (var ramp in L.ramps) {
                m *= Smooth(ramp.width * 0.5f, ramp.width * 0.5f + 6f, SegmentDistance(p, ramp.low, ramp.high, out _));
            }
            m *= Smooth(L.ruinsRadius - 5f, L.ruinsRadius, Vector2.Distance(p, L.ruinsCenter));
            m *= Smooth(L.campRadius - 5f, L.campRadius, Vector2.Distance(p, L.campCenter));
            m *= Smooth(L.denRadius - 5f, L.denRadius, Vector2.Distance(p, L.denCenter));
            foreach (var marsh in L.marshes) {
                m *= Smooth(marsh.size * 0.5f, marsh.size * 0.5f + 4f, Vector2.Distance(p, marsh.position));
            }
            return m;
        }

        private bool IsFord(float along) => Mathf.Abs(along - fordAlong) < L.fordLength * 0.5f;

        private float RiverDistance(Vector2 p, out float along, out Vector2 direction) {
            var best = float.MaxValue;
            along = 0f;
            direction = Vector2.right;
            for (int i = 0; i < river.Count - 1; i++) {
                var d = SegmentDistance(p, river[i], river[i + 1], out var t);
                if (d < best) {
                    best = d;
                    along = Mathf.Lerp(riverAlong[i], riverAlong[i + 1], t);
                    direction = (river[i + 1] - river[i]).normalized;
                }
            }
            return best;
        }

        // ----- Terrain -----

        public void BuildTerrain() {
            var data = AssetDatabase.LoadAssetAtPath<TerrainData>(TerrainDataPath);
            if (!data) {
                data = new TerrainData();
                AssetDatabase.CreateAsset(data, TerrainDataPath);
            }
            data.heightmapResolution = HeightRes;
            data.size = new Vector3(TerrainSize, TerrainHeight, TerrainSize);
            var heights = new float[HeightRes, HeightRes];
            for (int z = 0; z < HeightRes; z++) {
                for (int x = 0; x < HeightRes; x++) {
                    var p = new Vector2(-TerrainSize * 0.5f + x * TerrainSize / (HeightRes - 1),
                        -TerrainSize * 0.5f + z * TerrainSize / (HeightRes - 1));
                    heights[z, x] = Mathf.Clamp01((Height(p) - TerrainBase) / TerrainHeight);
                }
            }
            data.SetHeights(0, 0, heights);

            var layers = new TerrainLayer[GroundLayers.Length];
            for (int i = 0; i < layers.Length; i++) {
                layers[i] = AssetDatabase.LoadAssetAtPath<TerrainLayer>(LayerFolder + "/TL_" + GroundLayers[i] + ".terrainlayer");
            }
            data.terrainLayers = layers;
            data.alphamapResolution = AlphaRes;
            var maps = new float[AlphaRes, AlphaRes, layers.Length];
            var halfWidth = L.riverWidth * 0.5f;
            for (int z = 0; z < AlphaRes; z++) {
                for (int x = 0; x < AlphaRes; x++) {
                    var u = (x + 0.5f) / AlphaRes;
                    var v = (z + 0.5f) / AlphaRes;
                    var p = new Vector2(-TerrainSize * 0.5f + u * TerrainSize, -TerrainSize * 0.5f + v * TerrainSize);
                    var riverDistance = RiverDistance(p, out var along, out _);
                    int layer;
                    if (p.magnitude > L.radius - 1f || data.GetSteepness(u, v) > 38f) {
                        layer = 4;
                    } else if (InMarsh(p, 2f) || (IsFord(along) && riverDistance < halfWidth + L.bankWidth + 3f)) {
                        layer = 3;
                    } else if (riverDistance < halfWidth + L.bankWidth + 2f) {
                        layer = 2;
                    } else if (InForest(p, 1.1f)) {
                        layer = 1;
                    } else {
                        layer = 0;
                    }
                    maps[z, x, layer] = 1f;
                }
            }
            data.SetAlphamaps(0, 0, maps);
            EditorUtility.SetDirty(data);

            var go = Terrain.CreateTerrainGameObject(data);
            go.name = "Terrain";
            go.transform.SetParent(root, false);
            go.transform.position = new Vector3(-TerrainSize * 0.5f, TerrainBase, -TerrainSize * 0.5f);
            var terrain = go.GetComponent<Terrain>();
            var terrainMaterial = AssetDatabase.LoadAssetAtPath<Material>(TerrainMaterialPath);
            if (terrainMaterial) {
                terrain.materialTemplate = terrainMaterial;
            }
            terrain.drawInstanced = true;
            terrain.heightmapPixelError = 4f;
            terrain.basemapDistance = 250f;
            MakeStatic(go);
            Log.AppendLine("terrain " + HeightRes + "^2, material=" + (terrainMaterial ? terrainMaterial.name : "default"));
        }

        private bool InMarsh(Vector2 p, float margin) {
            foreach (var marsh in L.marshes) {
                if (Vector2.Distance(p, marsh.position) < marsh.size * 0.5f + margin) {
                    return true;
                }
            }
            return false;
        }

        private bool InForest(Vector2 p, float scale) {
            foreach (var forest in L.forests) {
                var d = p - forest.center;
                var nx = d.x / (forest.radii.x * scale);
                var ny = d.y / (forest.radii.y * scale);
                if (nx * nx + ny * ny < 1f) {
                    return true;
                }
            }
            return false;
        }

        // ----- Cliffs and boundary -----

        public void BuildCliffs() {
            var parent = Group("Cliffs");
            var prefab = LoadPrefab(PrefabFolder + "/Rock_Cliff.prefab");
            var count = 0;
            for (int pi = 0; pi < L.plateaus.Length; pi++) {
                var plateau = L.plateaus[pi];
                foreach (var (point, low) in AlongCliff(plateau, 4f)) {
                    if (NearRamp(point, 2.5f)) {
                        continue;
                    }
                    var nearLadder = NearLadder(point, 5f);
                    var top = plateau.height + 0.3f + (nearLadder ? 0f : Rand(0f, 0.25f));
                    const float bottom = -1f;
                    var rock = Place(prefab, parent, At(point, (top + bottom) * 0.5f),
                        Quaternion.LookRotation(new Vector3(low.x, 0f, low.y))
                        * (nearLadder ? Quaternion.identity : Quaternion.Euler(Rand(-3f, 3f), Rand(-6f, 6f), Rand(-2f, 2f))));
                    rock.transform.localScale = new Vector3(Rand(4.6f, 5.4f), top - bottom, nearLadder ? 3f : Rand(3f, 3.6f));
                    MakeStatic(rock);
                    count++;
                }
            }
            Log.AppendLine("cliff rocks " + count);
        }

        // Points every `step` metres along a plateau's cliff line, with the horizontal normal pointing to the low side.
        private IEnumerable<(Vector2 point, Vector2 low)> AlongCliff(JungleArenaLayout.Plateau plateau, float step) {
            var line = plateau.cliffLine;
            for (int i = 0; i < line.Length - 1; i++) {
                var a = line[i];
                var b = line[i + 1];
                var length = Vector2.Distance(a, b);
                var dir = (b - a) / length;
                var normal = new Vector2(-dir.y, dir.x);
                for (var s = i == 0 ? 0f : step * 0.5f; s < length; s += step) {
                    var p = a + dir * s;
                    var low = InPolygon(p + normal * 3f, plateau.outline) ? -normal : normal;
                    yield return (p, low);
                }
            }
        }

        private bool NearRamp(Vector2 p, float margin) {
            foreach (var ramp in L.ramps) {
                if (SegmentDistance(p, ramp.low, ramp.high, out _) < ramp.width * 0.5f + margin) {
                    return true;
                }
            }
            return false;
        }

        private bool NearLadder(Vector2 p, float distance) {
            foreach (var ladder in L.ladders) {
                if (Vector2.Distance(p, ladder.position) < distance) {
                    return true;
                }
            }
            return false;
        }

        public void BuildBoundary() {
            var parent = Group("Boundary");
            var prefab = LoadPrefab(PrefabFolder + "/Rock_Boundary.prefab");
            var ring = L.radius + 2f;
            var rocks = Mathf.CeilToInt(2f * Mathf.PI * ring / 5.5f);
            for (int i = 0; i < rocks; i++) {
                var angle = i * Mathf.PI * 2f / rocks;
                var outward = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));
                var inside = Height(outward * (L.radius - 6f));
                var top = Mathf.Max(L.wallHeight, inside + 8f) + Rand(-0.5f, 1.5f);
                const float bottom = -2f;
                var rock = Place(prefab, parent, At(outward * ring, (top + bottom) * 0.5f),
                    Quaternion.LookRotation(new Vector3(-outward.x, 0f, -outward.y)) * Quaternion.Euler(Rand(-4f, 4f), Rand(-8f, 8f), 0f));
                rock.transform.localScale = new Vector3(Rand(6.5f, 7.5f), top - bottom, Rand(4f, 5f));
                MakeStatic(rock);
            }
            // Invisible cap: nobody climbs, jumps or flies out over the wall.
            const int caps = 48;
            var capParent = new GameObject("Cap").transform;
            capParent.SetParent(parent, false);
            for (int i = 0; i < caps; i++) {
                var angle = (i + 0.5f) * Mathf.PI * 2f / caps;
                var outward = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));
                var cap = new GameObject("Cap_" + i);
                cap.transform.SetParent(capParent, false);
                cap.transform.position = At(outward * (ring + 1f), L.capHeight * 0.5f);
                cap.transform.rotation = Quaternion.LookRotation(new Vector3(outward.x, 0f, outward.y));
                var box = cap.AddComponent<BoxCollider>();
                box.size = new Vector3(2f * Mathf.PI * (ring + 1f) / caps + 1.5f, L.capHeight + 4f, 3f);
            }
            Log.AppendLine("boundary rocks " + rocks + ", cap " + caps);
        }

        // ----- River, wading, crossings -----

        public void BuildRiver() {
            var parent = Group("River");
            var halfWidth = L.riverWidth * 0.5f;

            // Water surface strip, a little wider than the bed so it meets the banks.
            var vertices = new List<Vector3>();
            var uvs = new List<Vector2>();
            var triangles = new List<int>();
            for (int i = 0; i < river.Count; i++) {
                var dir = (river[Mathf.Min(i + 1, river.Count - 1)] - river[Mathf.Max(i - 1, 0)]).normalized;
                var normal = new Vector2(-dir.y, dir.x);
                var left = river[i] + normal * (halfWidth + 0.8f);
                var right = river[i] - normal * (halfWidth + 0.8f);
                vertices.Add(new Vector3(left.x, -0.03f, left.y));
                vertices.Add(new Vector3(right.x, -0.03f, right.y));
                uvs.Add(new Vector2(0f, riverAlong[i] / 10f));
                uvs.Add(new Vector2(1f, riverAlong[i] / 10f));
                if (i > 0) {
                    var b = (i - 1) * 2;
                    triangles.AddRange(new[] { b, b + 2, b + 1, b + 1, b + 2, b + 3 });
                }
            }
            var mesh = AssetDatabase.LoadAssetAtPath<Mesh>(RiverMeshPath);
            if (!mesh) {
                mesh = new Mesh { name = "JungleArenaRiver" };
                AssetDatabase.CreateAsset(mesh, RiverMeshPath);
            }
            mesh.Clear();
            mesh.SetVertices(vertices);
            mesh.SetUVs(0, uvs);
            mesh.SetTriangles(triangles, 0);
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            EditorUtility.SetDirty(mesh);
            var water = new GameObject("Water");
            water.transform.SetParent(parent, false);
            water.layer = 4;
            water.AddComponent<MeshFilter>().sharedMesh = mesh;
            var waterRenderer = water.AddComponent<MeshRenderer>();
            waterRenderer.sharedMaterial = Mat("M_Water");
            waterRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;

            // Wading zones, every 8 m, except at the ford.
            var zones = new GameObject("WaterZones").transform;
            zones.SetParent(parent, false);
            var zoneCount = 0;
            var total = riverAlong[riverAlong.Count - 1];
            for (var s = 4f; s < total; s += 8f) {
                if (Mathf.Abs(s - fordAlong) < L.fordLength * 0.5f + 1f) {
                    continue;
                }
                var (point, dir) = RiverAt(s);
                if (point.magnitude > L.radius + 2f) {
                    continue;
                }
                var zone = new GameObject("Water_" + zoneCount++);
                zone.transform.SetParent(zones, false);
                zone.layer = 2;
                // The zone's transform is the water surface; it reaches well above it, and WaterZone only
                // counts bodies whose root is down in the water.
                zone.transform.position = At(point, 0f);
                zone.transform.rotation = Quaternion.LookRotation(new Vector3(dir.x, 0f, dir.y));
                var box = zone.AddComponent<BoxCollider>();
                box.isTrigger = true;
                box.center = new Vector3(0f, 0.3f, 0f);
                box.size = new Vector3(L.riverWidth + 1f, 3f, 8.8f);
                zone.AddComponent<WaterZone>();
            }

            BuildStoneBridge(parent);
            BuildLogBridge(parent);
            BuildFordStones(parent);
            Log.AppendLine("river " + river.Count + " pts, " + zoneCount + " wading zones");
        }

        private (Vector2 point, Vector2 dir) RiverAt(float along) {
            for (int i = 0; i < river.Count - 1; i++) {
                if (along <= riverAlong[i + 1]) {
                    var t = Mathf.InverseLerp(riverAlong[i], riverAlong[i + 1], along);
                    return (Vector2.Lerp(river[i], river[i + 1], t), (river[i + 1] - river[i]).normalized);
                }
            }
            return (river[river.Count - 1], (river[river.Count - 1] - river[river.Count - 2]).normalized);
        }

        // Crossing axis at a river spot: across the water.
        private Vector2 CrossingAt(Vector2 spot, out Vector2 center) {
            RiverDistance(spot, out var along, out _);
            var (point, dir) = RiverAt(along);
            center = point;
            return new Vector2(-dir.y, dir.x);
        }

        private Vector2 bridgeCenter;
        private Vector2 bridgeAxis;

        private void BuildStoneBridge(Transform parent) {
            bridgeAxis = CrossingAt(L.stoneBridge, out bridgeCenter);
            var rotation = Quaternion.LookRotation(new Vector3(bridgeAxis.x, 0f, bridgeAxis.y));
            var bridge = new GameObject("StoneBridge").transform;
            bridge.SetParent(parent, false);
            bridge.position = At(bridgeCenter, 0f);
            bridge.rotation = rotation;
            // Deck top at 0.35 m: one step up from the banks.
            Block(bridge, "Deck", new Vector3(0f, 0.05f, 0f), new Vector3(L.stoneBridgeWidth, 0.6f, L.stoneBridgeLength), "M_Ruin", false);
            var rail = L.stoneBridgeWidth * 0.5f - 0.25f;
            Block(bridge, "RailL", new Vector3(-rail, 0.65f, 0f), new Vector3(0.5f, 0.6f, L.stoneBridgeLength - 4f), "M_Ruin", false);
            Block(bridge, "RailR", new Vector3(rail, 0.65f, 0f), new Vector3(0.5f, 0.6f, L.stoneBridgeLength - 4f), "M_Ruin", false);
            MakeStatic(bridge.gameObject);
        }

        private void BuildLogBridge(Transform parent) {
            var axis = CrossingAt(L.logBridge, out var center);
            var log = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            log.name = "LogBridge";
            log.transform.SetParent(parent, false);
            log.transform.position = At(center, 0.45f - 0.7f);
            log.transform.rotation = Quaternion.FromToRotation(Vector3.up, new Vector3(axis.x, 0f, axis.y));
            log.transform.localScale = new Vector3(1.4f, L.logBridgeLength * 0.5f, 1.4f);
            log.GetComponent<MeshRenderer>().sharedMaterial = Mat("M_TreeTrunk");
            MakeStatic(log);
        }

        private void BuildFordStones(Transform parent) {
            var axis = CrossingAt(L.ford, out var center);
            var along = new Vector2(axis.y, -axis.x);
            var stones = new GameObject("FordStones").transform;
            stones.SetParent(parent, false);
            for (int i = 0; i < 9; i++) {
                var p = center + axis * Rand(-6f, 6f) + along * Rand(-L.fordLength * 0.4f, L.fordLength * 0.4f);
                var stone = Block(stones, "Stone_" + i, At(p, -0.05f), new Vector3(Rand(0.8f, 1.4f), 0.3f, Rand(0.7f, 1.2f)), "M_Rock", false, true);
                stone.transform.rotation = Quaternion.Euler(0f, Rand(0f, 360f), 0f);
                Object.DestroyImmediate(stone.GetComponent<Collider>());
            }
            MakeStatic(stones.gameObject);
        }

        // ----- Ladders (the ported 3dPixelZombies ladder: trigger + LadderController) -----

        public void BuildLadders() {
            var parent = Group("Ladders");
            var model = LoadPrefab(LadderModelPath);
            for (int i = 0; i < L.ladders.Length; i++) {
                var ladder = L.ladders[i];
                var plateau = L.plateaus[ladder.plateau];
                PolylineDistance(ladder.position, plateau.cliffLine, out var segment, out var t);
                var a = plateau.cliffLine[segment];
                var b = plateau.cliffLine[segment + 1];
                var c = Vector2.Lerp(a, b, t);
                var dir = (b - a).normalized;
                var low = new Vector2(-dir.y, dir.x);
                if (InPolygon(c + low * 3f, plateau.outline)) {
                    low = -low;
                }
                // Against the low-side face of the 3 m deep rock under it.
                var foot = c + low * 1.62f;
                var ground = Height(c + low * 2.5f);
                var top = plateau.height + 0.3f;
                var go = new GameObject("Ladder_" + i);
                go.transform.SetParent(parent, false);
                go.transform.position = At(foot, ground);
                go.transform.rotation = Quaternion.LookRotation(new Vector3(-low.x, 0f, -low.y));
                var segments = Mathf.CeilToInt((top - ground) / LadderSegment);
                for (int s = 0; s < segments; s++) {
                    var piece = (GameObject)PrefabUtility.InstantiatePrefab(model);
                    piece.name = "Segment_" + s;
                    piece.transform.SetParent(go.transform, false);
                    piece.transform.localPosition = new Vector3(0f, LadderPivotOffset + s * LadderSegment, 0f);
                    piece.transform.localScale = new Vector3(2.2f, 1f, 2f);
                    foreach (var r in piece.GetComponentsInChildren<MeshRenderer>()) {
                        r.sharedMaterial = Mat("M_Ladder");
                    }
                }
                // Trigger in front of the ladder, ending a little above the cliff top so climbing on steps you off.
                var trigger = new GameObject("Trigger");
                trigger.transform.SetParent(go.transform, false);
                var height = top - ground + 0.9f;
                var box = trigger.AddComponent<BoxCollider>();
                box.isTrigger = true;
                box.size = new Vector3(1.1f, height, 1f);
                box.center = new Vector3(0f, height * 0.5f, -0.5f);
                trigger.AddComponent<LadderController>();
            }
            Log.AppendLine("ladders " + L.ladders.Length);
        }

        // ----- Ruins around the stone bridge -----

        public void BuildRuins() {
            var parent = Group("Ruins");
            var across = new Vector2(bridgeAxis.y, -bridgeAxis.x);
            for (int side = -1; side <= 1; side += 2) {
                var axis = bridgeAxis * side;
                var center = bridgeCenter + axis * (L.stoneBridgeLength * 0.5f + 8f);
                var frame = new GameObject(side > 0 ? "RuinsNorth" : "RuinsSouth").transform;
                frame.SetParent(parent, false);
                frame.position = At(center, 0f);
                frame.rotation = Quaternion.LookRotation(new Vector3(axis.x, 0f, axis.y));
                // Low platform: one step up.
                Block(frame, "Platform", new Vector3(0f, -0.025f, 0f), new Vector3(34f, 0.75f, 14f), "M_Ruin", false);
                // Broken walls (far edge, sides) and pillars: cover for hunters, climbable for monkeys.
                Block(frame, "Wall_0", new Vector3(-11f, 1.6f, 6.5f), new Vector3(8f, 3.2f, 0.8f), "M_Ruin", true);
                Block(frame, "Wall_1", new Vector3(5f, 1.3f, 6.5f), new Vector3(6f, 2.6f, 0.8f), "M_Ruin", true);
                Block(frame, "Wall_2", new Vector3(13.5f, 1.75f, 6.5f), new Vector3(4f, 3.5f, 0.8f), "M_Ruin", true);
                Block(frame, "Wall_3", new Vector3(-16.6f, 1.4f, 0f), new Vector3(0.8f, 2.8f, 8f), "M_Ruin", true);
                Block(frame, "Wall_4", new Vector3(16.6f, 1.5f, -2f), new Vector3(0.8f, 3f, 6f), "M_Ruin", true);
                Block(frame, "Pillar_0", new Vector3(-10f, 2.25f, -4.5f), new Vector3(1.2f, 4.5f, 1.2f), "M_Ruin", true);
                Block(frame, "Pillar_1", new Vector3(10f, 2f, -4.5f), new Vector3(1.2f, 4f, 1.2f), "M_Ruin", true);
                Block(frame, "Pillar_2", new Vector3(-4f, 1.5f, 3f), new Vector3(1.2f, 3f, 1.2f), "M_Ruin", true);
                Block(frame, "Pillar_3", new Vector3(4f, 2.5f, 3f), new Vector3(1.2f, 5f, 1.2f), "M_Ruin", true);
                // Arch over the bridge end.
                var arch = bridgeCenter + axis * (L.stoneBridgeLength * 0.5f + 0.5f);
                var archFrame = new GameObject("Arch").transform;
                archFrame.SetParent(frame, false);
                archFrame.position = At(arch, 0f);
                archFrame.rotation = frame.rotation;
                var x = L.stoneBridgeWidth * 0.5f + 0.8f;
                Block(archFrame, "PillarL", new Vector3(-x, 2.1f, 0f), new Vector3(1.2f, 4.2f, 1.2f), "M_Ruin", true);
                Block(archFrame, "PillarR", new Vector3(x, 2.1f, 0f), new Vector3(1.2f, 4.2f, 1.2f), "M_Ruin", true);
                Block(archFrame, "Lintel", new Vector3(0f, 4.6f, 0f), new Vector3(2f * x + 1.4f, 0.8f, 1.2f), "M_Ruin", true);
                MakeStatic(frame.gameObject);
            }
            // Rubble scattered around (cover, no climbing).
            var rubble = new GameObject("Rubble").transform;
            rubble.SetParent(parent, false);
            for (int i = 0, placed = 0; i < 200 && placed < 16; i++) {
                var p = L.ruinsCenter + Random2(L.ruinsRadius * 0.85f);
                if (RiverDistance(p, out _, out _) < L.riverWidth * 0.5f + L.bankWidth + 1f || Mathf.Abs(Vector2.Dot(p - bridgeCenter, across)) < 18f && Mathf.Abs(Vector2.Dot(p - bridgeCenter, bridgeAxis)) < 30f) {
                    continue;
                }
                var size = Rand(0.5f, 1.3f);
                var stone = Block(rubble, "Rubble_" + placed++, At(p, Height(p) + size * 0.3f), new Vector3(size * Rand(0.8f, 1.6f), size, size * Rand(0.8f, 1.4f)), "M_Ruin", false, true);
                stone.transform.rotation = Quaternion.Euler(Rand(-10f, 10f), Rand(0f, 360f), Rand(-10f, 10f));
            }
            MakeStatic(rubble.gameObject);
            keepClear.Add((bridgeCenter + bridgeAxis * (L.stoneBridgeLength * 0.5f + 8f), 20f));
            keepClear.Add((bridgeCenter - bridgeAxis * (L.stoneBridgeLength * 0.5f + 8f), 20f));
            Log.AppendLine("ruins");
        }

        // ----- Hunter camp -----

        public void BuildCamp() {
            var parent = Group("HunterCamp");
            var c = L.campCenter;
            var ground = Height(c);
            var tents = new[] { new Vector2(-13f, -11f), new Vector2(-4.5f, -14f), new Vector2(4.5f, -14f), new Vector2(13f, -11f) };
            for (int i = 0; i < tents.Length; i++) {
                var p = c + tents[i];
                var tent = Block(parent, "Tent_" + i, At(p, ground + 0.45f), new Vector3(2.6f, 2.6f, 3.4f), "M_Tent", false, true);
                var face = (c - p).normalized;
                tent.transform.rotation = Quaternion.LookRotation(new Vector3(face.x, 0f, face.y)) * Quaternion.Euler(0f, 0f, 45f);
            }
            var crates = new[] { new Vector2(9f, 5f), new Vector2(10.2f, 5.4f), new Vector2(9.6f, 6.6f), new Vector2(-9f, 5f), new Vector2(-10f, 6.2f), new Vector2(-14f, -2f) };
            for (int i = 0; i < crates.Length; i++) {
                var crate = Block(parent, "Crate_" + i, At(c + crates[i], ground + 0.45f), Vector3.one * 0.9f, "M_Crate", false, true);
                crate.transform.rotation = Quaternion.Euler(0f, Rand(0f, 90f), 0f);
            }
            Block(parent, "FirePit", At(c + new Vector2(0f, -7f), ground + 0.05f), new Vector3(1.4f, 0.12f, 1.4f), "M_RockDark", false, true);
            Block(parent, "FlagPole", At(c + new Vector2(0f, 9f), ground + 3f), new Vector3(0.12f, 6f, 0.12f), "M_Wood", false, true);
            Block(parent, "Flag", At(c + new Vector2(0.65f, 9f), ground + 5.5f), new Vector3(1.2f, 0.7f, 0.05f), "M_Flag", false, true);
            MakeStatic(parent.gameObject);
            Log.AppendLine("camp");
        }

        // ----- Trees, bushes, vines -----

        public void BuildTreesAndVines() {
            var trees = Group("Trees");
            var vines = Group("Vines");
            var jungle = LoadPrefab(PrefabFolder + "/Tree_Jungle.prefab");
            var canopy = LoadPrefab(PrefabFolder + "/Tree_Canopy.prefab");
            var bush = LoadPrefab(PrefabFolder + "/Bush.prefab");
            int big = 0, small = 0, swings = 0;

            // Vine chains first: canopy trees along them, swing points between.
            for (int c = 0; c < L.vines.Length; c++) {
                var chain = SmoothPath(L.vines[c].points, 1f);
                var length = 0f;
                var along = new List<float> { 0f };
                for (int i = 1; i < chain.Count; i++) {
                    length += Vector2.Distance(chain[i - 1], chain[i]);
                    along.Add(length);
                }
                for (var s = 0f; s <= length; s += L.vineTreeSpacing) {
                    var (p, dir) = PathAt(chain, along, s);
                    if (!CanGrow(p, 6f, ignoreDen: false)) {
                        continue;
                    }
                    var tree = Place(canopy, trees, At(p, Height(p) - 0.15f),
                        Quaternion.LookRotation(new Vector3(dir.x, 0f, dir.y)) * Quaternion.Euler(0f, -90f, 0f));
                    MakeStatic(tree);
                    trunks.Add(p);
                    big++;
                }
                var chainParent = new GameObject("Chain_" + c).transform;
                chainParent.SetParent(vines, false);
                Vector3? previous = null;
                for (var s = 0f; s <= length; s += L.swingSpacing) {
                    var (p, _) = PathAt(chain, along, s);
                    var ground = 0f;
                    for (int k = -2; k <= 2; k++) {
                        ground += Height(PathAt(chain, along, Mathf.Clamp(s + k * 4f, 0f, length)).point);
                    }
                    var anchor = At(p, ground / 5f + L.swingHeight);
                    var point = new GameObject("Swing_" + swings++);
                    point.transform.SetParent(chainParent, false);
                    point.transform.position = anchor;
                    point.AddComponent<SwingPoint>();
                    Strand(point.transform, anchor + Vector3.down * 1.2f, anchor, 0.05f);
                    if (previous.HasValue) {
                        Strand(chainParent, previous.Value, anchor, 0.04f);
                    }
                    previous = anchor;
                }
            }

            // Den: a ring of canopy trees around the monkey spawn.
            for (int i = 0; i < 4; i++) {
                var angle = (45f + i * 90f) * Mathf.Deg2Rad;
                var p = L.denCenter + new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * 14f;
                if (!CanGrow(p, 6f, ignoreDen: true)) {
                    continue;
                }
                MakeStatic(Place(canopy, trees, At(p, Height(p) - 0.15f), Quaternion.Euler(0f, Rand(0f, 360f), 0f)));
                trunks.Add(p);
                big++;
            }

            // Forests.
            foreach (var forest in L.forests) {
                var placed = 0;
                for (int attempt = 0; attempt < forest.trees * 30 && placed < forest.trees; attempt++) {
                    var p = forest.center + InEllipse(forest.radii);
                    var isBig = rng.NextDouble() < forest.bigShare;
                    if (!CanGrow(p, isBig ? 6f : L.treeSpacing, ignoreDen: false)) {
                        continue;
                    }
                    var tree = Place(isBig ? canopy : jungle, trees, At(p, Height(p) - 0.15f), Quaternion.Euler(0f, Rand(0f, 360f), 0f));
                    tree.transform.localScale = Vector3.one * (isBig ? Rand(0.95f, 1.08f) : Rand(0.85f, 1.2f));
                    MakeStatic(tree);
                    trunks.Add(p);
                    placed++;
                    if (isBig) {
                        big++;
                    } else {
                        small++;
                    }
                }
            }
            // A few lone trees in the open.
            for (int attempt = 0, placed = 0; attempt < 2000 && placed < L.loneTrees; attempt++) {
                var p = Random2(L.radius - 12f);
                if (InForest(p, 1.2f) || !CanGrow(p, 12f, ignoreDen: false)) {
                    continue;
                }
                MakeStatic(Place(jungle, trees, At(p, Height(p) - 0.15f), Quaternion.Euler(0f, Rand(0f, 360f), 0f)));
                trunks.Add(p);
                placed++;
                small++;
            }

            // Bushes: monkey-height cover in the forests, a few in the open.
            var bushParent = Group("Bushes");
            var bushCount = 0;
            foreach (var forest in L.forests) {
                for (int attempt = 0, placed = 0; attempt < forest.bushes * 30 && placed < forest.bushes; attempt++) {
                    var p = forest.center + InEllipse(forest.radii * 1.1f);
                    if (!CanBush(p)) {
                        continue;
                    }
                    PlaceBush(bush, bushParent, p);
                    placed++;
                    bushCount++;
                }
            }
            for (int attempt = 0, placed = 0; attempt < 3000 && placed < L.loneBushes; attempt++) {
                var p = Random2(L.radius - 10f);
                if (!CanBush(p)) {
                    continue;
                }
                PlaceBush(bush, bushParent, p);
                placed++;
                bushCount++;
            }
            Log.AppendLine("trees " + (big + small) + " (" + big + " canopy), bushes " + bushCount + ", swing points " + swings);
        }

        private void PlaceBush(GameObject prefab, Transform parent, Vector2 p) {
            var go = Place(prefab, parent, At(p, Height(p) - 0.1f), Quaternion.Euler(0f, Rand(0f, 360f), 0f));
            go.transform.localScale = new Vector3(Rand(0.8f, 1.4f), Rand(0.7f, 1.2f), Rand(0.8f, 1.4f));
            MakeStatic(go);
            bushes.Add(p);
        }

        // Thin vine cylinder from a to b (no collider).
        private void Strand(Transform parent, Vector3 a, Vector3 b, float thickness) {
            var strand = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            strand.name = "Vine";
            Object.DestroyImmediate(strand.GetComponent<Collider>());
            strand.transform.SetParent(parent, true);
            strand.transform.position = (a + b) * 0.5f;
            strand.transform.up = (b - a).normalized;
            strand.transform.localScale = new Vector3(thickness, Vector3.Distance(a, b) * 0.5f, thickness);
            var r = strand.GetComponent<MeshRenderer>();
            r.sharedMaterial = Mat("M_Vine");
            r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        }

        private bool CanGrow(Vector2 p, float spacing, bool ignoreDen) {
            if (p.magnitude > L.radius - 8f || !Clear(p, spacing)) {
                return false;
            }
            if (RiverDistance(p, out _, out _) < L.riverWidth * 0.5f + L.bankWidth + 1.5f) {
                return false;
            }
            if (Vector2.Distance(p, L.ruinsCenter) < L.ruinsRadius || Vector2.Distance(p, L.campCenter) < L.campRadius) {
                return false;
            }
            if (!ignoreDen && Vector2.Distance(p, L.denCenter) < L.denRadius) {
                return false;
            }
            foreach (var plateau in L.plateaus) {
                if (PolylineDistance(p, plateau.cliffLine, out _, out _) < 4.5f) {
                    return false;
                }
            }
            if (NearRamp(p, 2.5f) || NearLadder(p, 6f) || InSpawn(p, 3f)) {
                return false;
            }
            foreach (var trunk in trunks) {
                if (Vector2.Distance(p, trunk) < spacing) {
                    return false;
                }
            }
            return true;
        }

        private bool CanBush(Vector2 p) {
            if (p.magnitude > L.radius - 6f || !Clear(p, 0f) || InSpawn(p, 2f)) {
                return false;
            }
            if (RiverDistance(p, out _, out _) < L.riverWidth * 0.5f + L.bankWidth) {
                return false;
            }
            if (Vector2.Distance(p, L.campCenter) < L.campRadius - 6f || NearRamp(p, 0.5f) || NearLadder(p, 3f)) {
                return false;
            }
            foreach (var plateau in L.plateaus) {
                if (PolylineDistance(p, plateau.cliffLine, out _, out _) < 3f) {
                    return false;
                }
            }
            foreach (var trunk in trunks) {
                if (Vector2.Distance(p, trunk) < 1.6f) {
                    return false;
                }
            }
            foreach (var other in bushes) {
                if (Vector2.Distance(p, other) < 2f) {
                    return false;
                }
            }
            return true;
        }

        private bool Clear(Vector2 p, float extra) {
            foreach (var (center, radius) in keepClear) {
                if (Vector2.Distance(p, center) < radius + extra * 0.5f) {
                    return false;
                }
            }
            return true;
        }

        private bool InSpawn(Vector2 p, float margin) {
            return InRect(p, L.hunterSpawn, L.hunterSpawnSize, margin) || InRect(p, L.monkeySpawn, L.monkeySpawnSize, margin);
        }

        private static bool InRect(Vector2 p, Vector2 center, Vector2 size, float margin) {
            return Mathf.Abs(p.x - center.x) < size.x * 0.5f + margin && Mathf.Abs(p.y - center.y) < size.y * 0.5f + margin;
        }

        private Vector2 InEllipse(Vector2 radii) {
            var angle = Rand(0f, Mathf.PI * 2f);
            var r = Mathf.Sqrt((float)rng.NextDouble());
            return new Vector2(Mathf.Cos(angle) * radii.x * r, Mathf.Sin(angle) * radii.y * r);
        }

        private Vector2 Random2(float radius) => InEllipse(new Vector2(radius, radius));

        // ----- Boulders, marshes, pickups -----

        public void BuildProps() {
            var parent = Group("Props");
            var boulder = LoadPrefab(PrefabFolder + "/Boulder.prefab");
            foreach (var b in L.boulders) {
                var go = Place(boulder, parent, At(b.position, Height(b.position) + b.size * 0.15f), Quaternion.Euler(0f, Rand(0f, 360f), 0f));
                go.transform.localScale = new Vector3(b.size, b.size * Rand(0.6f, 0.8f), b.size * Rand(0.8f, 1f));
                MakeStatic(go);
            }
            var quicksand = LoadPrefab("Assets/_Project/Prefabs/World/Quicksand.prefab");
            foreach (var m in L.marshes) {
                var go = Place(quicksand, parent, At(m.position, Height(m.position)), Quaternion.identity);
                go.transform.localScale = new Vector3(m.size / 4f, 1f, m.size / 4f);
            }
            var rock = LoadPrefab("Assets/_Project/Prefabs/World/Pickup_Rock.prefab");
            foreach (var p in L.rockPiles) {
                Place(rock, parent, At(p, Height(p)), Quaternion.Euler(0f, Rand(0f, 360f), 0f));
            }
            var fruit = LoadPrefab("Assets/_Project/Prefabs/World/Pickup_Fruit.prefab");
            foreach (var p in L.fruitBushes) {
                Place(fruit, parent, At(p, Height(p)), Quaternion.Euler(0f, Rand(0f, 360f), 0f));
            }
            var banana = LoadPrefab(PrefabFolder + "/Tree_Banana.prefab");
            foreach (var p in L.bananaTrees) {
                Place(banana, parent, At(p, Height(p) - 0.05f), Quaternion.Euler(0f, Rand(0f, 360f), 0f));
            }
            Log.AppendLine("boulders " + L.boulders.Length + ", marshes " + L.marshes.Length + ", rocks " + L.rockPiles.Length
                + ", fruit " + L.fruitBushes.Length + ", banana trees " + L.bananaTrees.Length);
        }

        // ----- Scene objects outside Generated (kept between rebuilds) -----

        public void PlaceSceneObjects(UnityEngine.SceneManagement.Scene scene) {
            foreach (var go in scene.GetRootGameObjects()) {
                switch (go.name) {
                    case "SpawnArea_Hunters":
                        PlaceSpawn(go, L.hunterSpawn, L.hunterSpawnSize);
                        break;
                    case "SpawnArea_Monkeys":
                        PlaceSpawn(go, L.monkeySpawn, L.monkeySpawnSize);
                        break;
                    case "SpectatorOverview":
                        go.transform.position = new Vector3(0f, 150f, -250f);
                        go.transform.rotation = Quaternion.LookRotation(-go.transform.position);
                        break;
                    case "CatchAllKillZone":
                        go.transform.position = new Vector3(0f, -30f, 0f);
                        go.GetComponent<BoxCollider>().size = new Vector3(700f, 2f, 700f);
                        break;
                }
            }
            Log.AppendLine("spawns / overview / kill zone placed");
        }

        private void PlaceSpawn(GameObject go, Vector2 center, Vector2 size) {
            go.transform.position = At(center, Height(center));
            go.transform.rotation = Quaternion.identity;
            var so = new SerializedObject(go.GetComponent<SpawnArea>());
            so.FindProperty("size").vector3Value = new Vector3(size.x, 0f, size.y);
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        // ----- Helpers -----

        private GameObject Place(GameObject prefab, Transform parent, Vector3 position, Quaternion rotation) {
            var go = (GameObject)PrefabUtility.InstantiatePrefab(prefab, parent);
            go.transform.SetPositionAndRotation(position, rotation);
            return go;
        }

        private GameObject Block(Transform parent, string name, Vector3 position, Vector3 scale, string material, bool climbable,
            bool worldPosition = false) {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.name = name;
            go.transform.SetParent(parent, false);
            if (worldPosition) {
                go.transform.position = position;
            } else {
                go.transform.localPosition = position;
            }
            go.transform.localScale = scale;
            go.GetComponent<MeshRenderer>().sharedMaterial = Mat(material);
            if (climbable) {
                go.AddComponent<Climbable>();
            }
            return go;
        }

        private (Vector2 point, Vector2 dir) PathAt(List<Vector2> path, List<float> along, float s) {
            for (int i = 0; i < path.Count - 1; i++) {
                if (s <= along[i + 1]) {
                    var t = Mathf.InverseLerp(along[i], along[i + 1], s);
                    return (Vector2.Lerp(path[i], path[i + 1], t), (path[i + 1] - path[i]).normalized);
                }
            }
            return (path[path.Count - 1], (path[path.Count - 1] - path[path.Count - 2]).normalized);
        }
    }

    // ---------- Geometry ----------

    private static void MakeStatic(GameObject go) {
        foreach (var t in go.GetComponentsInChildren<Transform>(true)) {
            GameObjectUtility.SetStaticEditorFlags(t.gameObject, StaticEditorFlags.BatchingStatic | StaticEditorFlags.OccludeeStatic);
        }
    }

    private static float Smooth(float a, float b, float x) {
        var t = Mathf.Clamp01((x - a) / (b - a));
        return t * t * (3f - 2f * t);
    }

    private static float SegmentDistance(Vector2 p, Vector2 a, Vector2 b, out float t) {
        var ab = b - a;
        var lengthSq = ab.sqrMagnitude;
        t = lengthSq > 0f ? Mathf.Clamp01(Vector2.Dot(p - a, ab) / lengthSq) : 0f;
        return Vector2.Distance(p, a + ab * t);
    }

    private static float PolylineDistance(Vector2 p, IList<Vector2> line, out int segment, out float t) {
        var best = float.MaxValue;
        segment = 0;
        t = 0f;
        for (int i = 0; i < line.Count - 1; i++) {
            var d = SegmentDistance(p, line[i], line[i + 1], out var ti);
            if (d < best) {
                best = d;
                segment = i;
                t = ti;
            }
        }
        return best;
    }

    private static bool InPolygon(Vector2 p, IList<Vector2> polygon) {
        var inside = false;
        for (int i = 0, j = polygon.Count - 1; i < polygon.Count; j = i++) {
            var a = polygon[i];
            var b = polygon[j];
            if ((a.y > p.y) != (b.y > p.y) && p.x < (b.x - a.x) * (p.y - a.y) / (b.y - a.y) + a.x) {
                inside = !inside;
            }
        }
        return inside;
    }

    // Catmull-Rom through the points, sampled about every `spacing` metres.
    private static List<Vector2> SmoothPath(IList<Vector2> points, float spacing) {
        var result = new List<Vector2>();
        for (int i = 0; i < points.Count - 1; i++) {
            var p0 = points[Mathf.Max(i - 1, 0)];
            var p1 = points[i];
            var p2 = points[i + 1];
            var p3 = points[Mathf.Min(i + 2, points.Count - 1)];
            var steps = Mathf.Max(1, Mathf.CeilToInt(Vector2.Distance(p1, p2) / spacing));
            for (int s = 0; s < steps; s++) {
                var t = s / (float)steps;
                var t2 = t * t;
                var t3 = t2 * t;
                result.Add(0.5f * (2f * p1 + (-p0 + p2) * t + (2f * p0 - 5f * p1 + 4f * p2 - p3) * t2 + (-p0 + 3f * p1 - 3f * p2 + p3) * t3));
            }
        }
        result.Add(points[points.Count - 1]);
        return result;
    }
}
