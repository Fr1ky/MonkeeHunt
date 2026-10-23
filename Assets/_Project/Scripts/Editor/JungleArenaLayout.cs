using System;
using UnityEngine;

// Every number of the jungle arena (M8), from the agreed top-down draft. Positions are metres on the
// ground plane around the arena centre: x = east, y = north (world z). Tune it in the Inspector, then run
// MonkeeHunt > Build Jungle Arena. Editor-only (the built scene doesn't need it).
[CreateAssetMenu(menuName = "MonkeeHunt/Jungle Arena Layout", fileName = "JungleArenaLayout")]
public class JungleArenaLayout : ScriptableObject {

    [Serializable]
    public class Plateau {
        public string name;
        public float height = 10f;

        // The whole raised area (may reach outside the arena).
        public Vector2[] outline;

        // Where the cliff runs (rocks are placed along it).
        public Vector2[] cliffLine;
    }

    [Serializable]
    public class Ramp {
        public Vector2 low;
        public Vector2 high;
        public float lowHeight;
        public float highHeight = 10f;
        public float width = 8f;
    }

    [Serializable]
    public class Forest {
        public string name;
        public Vector2 center;
        public Vector2 radii;
        public int trees = 30;
        public int bushes = 30;

        [Range(0f, 1f)]
        public float bigShare = 0.25f;
    }

    [Serializable]
    public class VineChain {
        public Vector2[] points;
    }

    [Serializable]
    public class Ladder {
        public Vector2 position;

        // Index into plateaus (its cliff line).
        public int plateau;
    }

    [Serializable]
    public class Spot {
        public Vector2 position;
        public float size = 1f;
    }

    [Header("Arena")]
    public int seed = 1234;
    public float radius = 200f;
    public float wallHeight = 15f;
    public float capHeight = 40f;

    [Header("Ground")]
    public float bumpHeight = 0.6f;
    public float bumpScale = 0.025f;

    public Plateau[] plateaus = {
        new Plateau {
            name = "North plateau", height = 10f,
            cliffLine = new[] { V(-169, 107), V(-117, 90), V(-62, 79), V(-7, 93), V(55, 83), V(110, 97), V(169, 107) },
            outline = new[] {
                V(-169, 107), V(-117, 90), V(-62, 79), V(-7, 93), V(55, 83), V(110, 97), V(169, 107),
                V(260, 107), V(260, 260), V(-260, 260), V(-260, 107)
            }
        },
        new Plateau {
            name = "East ridge", height = 6f,
            cliffLine = new[] { V(124, -157), V(97, -117), V(79, -62), V(124, -48), V(188, -73) },
            outline = new[] {
                V(124, -157), V(97, -117), V(79, -62), V(124, -48), V(188, -73),
                V(260, -90), V(260, -260), V(140, -260)
            }
        }
    };

    public Ramp[] ramps = {
        new Ramp { low = V(-120, 62), high = V(-158, 112), highHeight = 10f },
        new Ramp { low = V(122, 62), high = V(160, 112), highHeight = 10f },
        new Ramp { low = V(66, -140), high = V(108, -118), highHeight = 6f }
    };

    [Header("River")]
    public Vector2[] river = {
        V(-225, 28), V(-180, 34), V(-140, 30), V(-105, 16), V(-70, 8), V(-35, 8), V(0, 0),
        V(35, -14), V(70, -24), V(110, -20), V(160, -10), V(225, -14)
    };
    public float riverWidth = 12f;
    public float riverDepth = 0.4f;
    public float bankWidth = 4f;

    public Vector2 ford = V(-110, 18);
    public float fordLength = 14f;
    public float fordDepth = 0.1f;

    public Vector2 stoneBridge = V(0, 0);
    public float stoneBridgeLength = 22f;
    public float stoneBridgeWidth = 6f;

    public Vector2 logBridge = V(118, -18);
    public float logBridgeLength = 20f;

    [Header("Clearings")]
    public Vector2 ruinsCenter = V(0, 0);
    public float ruinsRadius = 45f;
    public Vector2 campCenter = V(0, -160);
    public float campRadius = 36f;
    public Vector2 denCenter = V(0, 158);
    public float denRadius = 20f;

    [Header("Forests")]
    public Forest[] forests = {
        new Forest { name = "West jungle", center = V(-141, -52), radii = V(52, 61), trees = 55, bushes = 70 },
        new Forest { name = "North canopy west", center = V(-62, 145), radii = V(79, 34), trees = 45, bushes = 45 },
        new Forest { name = "North canopy east", center = V(72, 146), radii = V(72, 34), trees = 40, bushes = 40 },
        new Forest { name = "South-west grove", center = V(-76, -134), radii = V(50, 33), trees = 28, bushes = 30 },
        new Forest { name = "East forest", center = V(110, 41), radii = V(50, 31), trees = 28, bushes = 30 },
        new Forest { name = "South grove", center = V(45, -88), radii = V(29, 21), trees = 12, bushes = 15 }
    };
    public int loneTrees = 15;
    public int loneBushes = 60;
    public float treeSpacing = 4f;

    [Header("Vines (monkey highways)")]
    public VineChain[] vines = {
        new VineChain { points = new[] { V(-130, 140), V(-60, 150), V(0, 178), V(60, 150), V(130, 140) } },
        new VineChain { points = new[] { V(-66, 128), V(-54, 60), V(-60, 10), V(-90, -35) } },
        new VineChain { points = new[] { V(103, 124), V(114, 62), V(95, 10), V(75, -40), V(50, -80) } },
        new VineChain { points = new[] { V(-124, 0), V(-131, -76), V(-86, -117), V(-60, -140) } },
        new VineChain { points = new[] { V(110, 21), V(124, -34), V(152, -83) } }
    };
    public float vineTreeSpacing = 16f;
    public float swingSpacing = 4f;
    public float swingHeight = 11.5f;

    [Header("Ladders, rocks, hazards, pickups")]
    public Ladder[] ladders = {
        new Ladder { position = V(-56, 79), plateau = 0 },
        new Ladder { position = V(61, 83), plateau = 0 },
        new Ladder { position = V(86, -84), plateau = 1 }
    };

    public Spot[] boulders = {
        new Spot { position = V(-121, -102), size = 7f }, new Spot { position = V(-102, -91), size = 5f },
        new Spot { position = V(-103, -112), size = 6f }, new Spot { position = V(-132, -116), size = 4.5f },
        new Spot { position = V(-112, -125), size = 4f }, new Spot { position = V(-90, -104), size = 3.5f },
        new Spot { position = V(40, 60), size = 5f }, new Spot { position = V(-30, -70), size = 4f }
    };

    public Spot[] marshes = {
        new Spot { position = V(-93, -21), size = 14f }, new Spot { position = V(68, -43), size = 12f },
        new Spot { position = V(-45, 28), size = 10f }
    };

    public Vector2[] rockPiles = {
        V(-99, 72), V(90, 72), V(36, 36), V(138, -95), V(-90, -99), V(-30, -48), V(150, -25), V(-170, 30), V(0, 120),
        V(-60, 108)
    };

    public Vector2[] fruitBushes = {
        V(-112, -76), V(126, 23), V(48, 131), V(38, -97), V(-150, -10), V(-40, 140), V(100, 150), V(-60, -110),
        V(20, -130), V(160, -60)
    };

    public Vector2[] bananaTrees = {
        V(-146, -26), V(91, 47), V(-74, -123), V(-28, 150), V(50, -60), V(-100, 50), V(140, 120), V(120, -130)
    };

    [Header("Spawns (moves the scene's spawn areas)")]
    public Vector2 hunterSpawn = V(0, -158);
    public Vector2 hunterSpawnSize = V(16, 8);
    public Vector2 monkeySpawn = V(0, 156);
    public Vector2 monkeySpawnSize = V(14, 8);

    [Header("Look")]
    public bool fog = true;
    public float fogStart = 80f;
    public float fogEnd = 300f;
    public Color fogColor = new Color(0.72f, 0.78f, 0.7f);

    private static Vector2 V(float x, float y) => new Vector2(x, y);
}
