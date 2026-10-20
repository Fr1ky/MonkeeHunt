using System;
using UnityEngine;

public enum SupplyGroup : byte {
    Healing,
    Capture,
    Damage
}

// Walkie-talkie loadout item: calls in a helicopter supply drop. With the slot selected, LMB opens the
// code list; typing a group's arrow code calls it. Each slot has a few calls per round.
[CreateAssetMenu(menuName = "MonkeeHunt/Walkie-Talkie Definition", fileName = "WalkieTalkie")]
public class WalkieTalkieDefinition : EquipmentDefinition {

    [Serializable]
    public class SupplyCode {
        public SupplyGroup group;
        public string label;
        public Color color = Color.white;

        // Directions: 0 Up, 1 Right, 2 Down, 3 Left.
        public int[] arrows = new int[8];
    }

    [Min(1)]
    public int callsPerSlot = 2;

    // Networked SupplyDrop the server spawns where the caller stood.
    public GameObject dropPrefab;

    // From finishing the code to the crate landing.
    [Min(1f)]
    public float arrivalSeconds = 15f;

    // How long the crate stays after landing.
    [Min(1f)]
    public float crateSeconds = 60f;

    public SupplyCode[] codes = {
        new SupplyCode { group = SupplyGroup.Healing, label = "HEALING", color = new Color(0.35f, 0.85f, 0.4f), arrows = new[] { 0, 0, 2, 2, 3, 1, 3, 1 } },
        new SupplyCode { group = SupplyGroup.Capture, label = "CAPTURE", color = new Color(0.35f, 0.6f, 1f), arrows = new[] { 3, 3, 1, 1, 0, 2, 0, 2 } },
        new SupplyCode { group = SupplyGroup.Damage, label = "DAMAGE", color = new Color(1f, 0.55f, 0.2f), arrows = new[] { 2, 1, 2, 1, 0, 3, 0, 3 } }
    };

    public SupplyCode CodeFor(SupplyGroup group) {
        foreach (var code in codes) {
            if (code.group == group) {
                return code;
            }
        }
        return null;
    }
}
