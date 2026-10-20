using UnityEngine;

// Cage loadout item: one slot holds a stack of cages; LMB places one (with the carried monkey inside, or empty).
[CreateAssetMenu(menuName = "MonkeeHunt/Cage Definition", fileName = "Cage")]
public class CageDefinition : EquipmentDefinition {

    // Networked cage the server spawns when placing.
    public GameObject cagePrefab;

    [Min(1)]
    public int perSlot = 2;

    // How far in front of the hunter a cage is placed.
    [Min(0.5f)]
    public float placeDistance = 1.6f;
}
