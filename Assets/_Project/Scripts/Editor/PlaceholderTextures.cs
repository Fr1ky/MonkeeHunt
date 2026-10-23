using System;
using System.IO;
using UnityEditor;
using UnityEngine;

// Generates the placeholder UI and effect textures as PNG files (white shapes with alpha, tinted by the
// Image / RawImage / particle colour). Re-running overwrites them; real art can replace the files
// (keep the names and the references stay).
public static class PlaceholderTextures {

    private const string UiFolder = "Assets/_Project/Settings/Ui";
    private const string FxFolder = "Assets/_Project/Settings/Fx";
    private const string GroundFolder = "Assets/_Project/Settings/Map/Textures";

    [MenuItem("MonkeeHunt/Generate Placeholder Textures")]
    public static void GenerateAll() {
        Write(UiFolder, "T_SplatBlob", 256, 256, SplatBlob(256), false, false);
        Write(UiFolder, "T_TranqVignette", 128, 128, TranqVignette, false, false);
        Write(UiFolder, "T_BinocularMask", 512, 256, BinocularMask, false, false);
        Write(UiFolder, "T_Arrow", 64, 64, Arrow, false, false);
        Write(UiFolder, "T_UiVignette", 256, 256, DamageVignette, true, false);
        Write(UiFolder, "T_UiHitArc", 256, 256, HitArc, true, false);
        Write(FxFolder, "T_FxSoftDot", 64, 64, SoftDot, false, true);
        GenerateGroundTiles();
        AssetDatabase.Refresh();
        Debug.Log("Placeholder textures written to " + UiFolder + ", " + FxFolder + " and " + GroundFolder);
    }

    // Tileable ground textures for the arena terrain layers (colour baked in).
    public static void GenerateGroundTiles() {
        Ground("T_Ground_Grass", new Color(0.40f, 0.56f, 0.25f), new Color(0.30f, 0.45f, 0.18f), 11);
        Ground("T_Ground_Jungle", new Color(0.26f, 0.33f, 0.16f), new Color(0.33f, 0.27f, 0.15f), 12);
        Ground("T_Ground_Mud", new Color(0.36f, 0.27f, 0.18f), new Color(0.27f, 0.2f, 0.13f), 13);
        Ground("T_Ground_Sand", new Color(0.78f, 0.69f, 0.47f), new Color(0.66f, 0.57f, 0.37f), 14);
        Ground("T_Ground_Rock", new Color(0.5f, 0.48f, 0.44f), new Color(0.36f, 0.35f, 0.32f), 15);
    }

    private static void Ground(string name, Color a, Color b, int seed) {
        var noise = TileNoise(seed);
        Write(GroundFolder, name, 128, 128, (u, v) => Color.Lerp(a, b, noise(u, v)), false, true, TextureWrapMode.Repeat);
    }

    // Two octaves of value noise on a wrapping grid, so the texture tiles seamlessly. Returns 0..1.
    private static Func<float, float, float> TileNoise(int seed) {
        var rng = new System.Random(seed);
        var coarse = new float[8, 8];
        var fine = new float[32, 32];
        for (int y = 0; y < 8; y++) {
            for (int x = 0; x < 8; x++) {
                coarse[x, y] = (float)rng.NextDouble();
            }
        }
        for (int y = 0; y < 32; y++) {
            for (int x = 0; x < 32; x++) {
                fine[x, y] = (float)rng.NextDouble();
            }
        }
        return (u, v) => Mathf.Clamp01(0.65f * Sample(coarse, 8, u, v) + 0.35f * Sample(fine, 32, u, v));
    }

    private static float Sample(float[,] grid, int n, float u, float v) {
        var x = u * n;
        var y = v * n;
        var x0 = Mathf.FloorToInt(x);
        var y0 = Mathf.FloorToInt(y);
        var tx = Mathf.SmoothStep(0f, 1f, x - x0);
        var ty = Mathf.SmoothStep(0f, 1f, y - y0);
        float At(int ix, int iy) => grid[((ix % n) + n) % n, ((iy % n) + n) % n];
        return Mathf.Lerp(Mathf.Lerp(At(x0, y0), At(x0 + 1, y0), tx), Mathf.Lerp(At(x0, y0 + 1), At(x0 + 1, y0 + 1), tx), ty);
    }

    // Opacity of a splat at its edge; the centre is fully opaque and it fades gradually in between.
    private const float SplatEdgeOpacity = 0.7f;

    // Lumpy round poop blob with a few drips running down.
    private static Func<float, float, Color> SplatBlob(int size) {
        var rng = new System.Random(1234);
        var lobes = new float[7];
        for (int i = 0; i < lobes.Length; i++) {
            lobes[i] = (float)rng.NextDouble() * Mathf.PI * 2f;
        }
        return (u, v) => {
            // -1..1 around the centre (pixel centres, like the original runtime texture).
            var dx = u * 2f - 1f;
            var dy = v * 2f - 1f;
            var angle = Mathf.Atan2(dy, dx);
            var radius = 0.62f;
            for (int i = 0; i < lobes.Length; i++) {
                radius += 0.05f * Mathf.Sin(angle * (i + 2) + lobes[i]);
            }
            var drip = Mathf.Max(0f, Mathf.Sin(dx * 9f + 1.3f)) * Mathf.Clamp01(-dy) * 0.35f;
            var d = Mathf.Sqrt(dx * dx + dy * dy);
            var edge = radius + (dy < 0f ? drip : 0f);
            var shape = Mathf.SmoothStep(1f, 0f, Mathf.InverseLerp(edge - 0.06f, edge + 0.02f, d));
            // Solid in the middle, see-through towards the (lumpy) edge.
            var opacity = Mathf.Lerp(1f, SplatEdgeOpacity, Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(d / edge)));
            return White(shape * opacity);
        };
    }

    // Tranq: transparent centre, opaque edges.
    private static Color TranqVignette(float u, float v) {
        var dx = u * 2f - 1f;
        var dy = v * 2f - 1f;
        var d = Mathf.Sqrt(dx * dx + dy * dy);
        return White(Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(0.35f, 1.15f, d)));
    }

    // Binoculars: opaque black except two soft-edged overlapping circles (2:1 texture).
    private static Color BinocularMask(float u, float v) {
        const float width = 2f;
        var p = new Vector2(u * width, v);
        var radius = 0.46f;
        var left = new Vector2(width * 0.5f - radius * 0.72f, 0.5f);
        var right = new Vector2(width * 0.5f + radius * 0.72f, 0.5f);
        var d = Mathf.Min(Vector2.Distance(p, left), Vector2.Distance(p, right)) / radius;
        return new Color(0f, 0f, 0f, Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(0.85f, 1f, d)));
    }

    // Minigame arrow pointing up (head + stem).
    private static Color Arrow(float u, float v) {
        var x = u - 0.5f;
        var head = v >= 0.5f && v <= 0.95f && Mathf.Abs(x) <= (0.95f - v) * 0.9f;
        var stem = v >= 0.08f && v < 0.55f && Mathf.Abs(x) <= 0.12f;
        return head || stem ? Color.white : Color.clear;
    }

    // Damage flash: clear centre, solid edges (stretched over the screen).
    private static Color DamageVignette(float u, float v) {
        var x = u * 2f - 1f;
        var y = v * 2f - 1f;
        var r = Mathf.Sqrt(x * x * 0.5f + y * y * 0.5f);
        var e = Mathf.Max(Mathf.Abs(x), Mathf.Abs(y));
        var d = Mathf.Max(r, e * 0.95f);
        return White(Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(0.55f, 1f, d)));
    }

    // Hit direction arc: a ring segment at the top (points "forward" = up on screen).
    private static Color HitArc(float u, float v) {
        var x = u * 2f - 1f;
        var y = v * 2f - 1f;
        var r = Mathf.Sqrt(x * x + y * y);
        var ring = Mathf.Clamp01(1f - Mathf.Abs(r - 0.86f) / 0.06f);
        var angle = Mathf.Abs(Mathf.Atan2(x, y) * Mathf.Rad2Deg);
        var span = Mathf.Clamp01((28f - angle) / 8f);
        return White(Mathf.SmoothStep(0f, 1f, ring) * span);
    }

    // Soft round particle.
    private static Color SoftDot(float u, float v) {
        var x = u * 2f - 1f;
        var y = v * 2f - 1f;
        var a = 1f - Mathf.Sqrt(x * x + y * y);
        return White(a <= 0f ? 0f : a * a * (3f - 2f * a));
    }

    private static Color White(float alpha) => new Color(1f, 1f, 1f, Mathf.Clamp01(alpha));

    // pixel(u, v) gets the pixel centre in 0..1 (v = 0 at the bottom).
    private static void Write(string folder, string name, int width, int height, Func<float, float, Color> pixel,
        bool sprite, bool mipmaps, TextureWrapMode wrap = TextureWrapMode.Clamp) {
        Directory.CreateDirectory(folder);
        var tex = new Texture2D(width, height, TextureFormat.RGBA32, false);
        for (int y = 0; y < height; y++) {
            for (int x = 0; x < width; x++) {
                tex.SetPixel(x, y, pixel((x + 0.5f) / width, (y + 0.5f) / height));
            }
        }
        var path = folder + "/" + name + ".png";
        File.WriteAllBytes(path, tex.EncodeToPNG());
        UnityEngine.Object.DestroyImmediate(tex);

        AssetDatabase.ImportAsset(path);
        var importer = (TextureImporter)AssetImporter.GetAtPath(path);
        importer.textureType = sprite ? TextureImporterType.Sprite : TextureImporterType.Default;
        if (sprite) {
            importer.spriteImportMode = SpriteImportMode.Single;
        }
        importer.alphaIsTransparency = true;
        importer.mipmapEnabled = mipmaps;
        importer.wrapMode = wrap;
        importer.textureCompression = TextureImporterCompression.Uncompressed;
        importer.SaveAndReimport();
    }
}
