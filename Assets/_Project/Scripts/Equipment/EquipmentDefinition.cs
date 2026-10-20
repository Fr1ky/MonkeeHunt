using UnityEngine;

// Anything a hunter can hold in a loadout slot (guns, binoculars, later cages / walkie-talkie).
public abstract class EquipmentDefinition : ScriptableObject {

    public string displayName;

    // Placeholder model shown in the hand socket (first person and on remote players).
    public GameObject viewModelPrefab;

    [Min(0f)]
    public float equipTime = 0.25f;

    // Scroll-adjustable zoom while aiming (binoculars, later scoped guns). Magnification is
    // relative to the base FOV: 4 = 4x. Each press starts at defaultZoom.
    [Header("Adjustable zoom")]
    public bool adjustableZoom;

    [Min(1f)]
    public float minZoom = 2f;

    [Min(1f)]
    public float maxZoom = 8f;

    [Min(1f)]
    public float defaultZoom = 4f;

    // Zoom is multiplied / divided by this per wheel notch.
    [Min(1.01f)]
    public float zoomStep = 1.25f;
}
