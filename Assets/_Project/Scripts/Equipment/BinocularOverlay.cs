using UnityEngine;
using UnityEngine.UI;

// Local: black "two lenses" mask over the screen while the hunter zooms with binoculars
// (texture: Settings/Ui/T_BinocularMask, from MonkeeHunt > Generate Placeholder Textures).
public class BinocularOverlay : MonoBehaviour {

    [SerializeField]
    private RawImage mask;

    private void Awake() {
        mask.enabled = false;
    }

    private void Update() {
        var local = PlayerNetworkController.Local;
        var aim = local && local.gameObject.scene == gameObject.scene ? local.GetComponent<HunterAim>() : null;
        mask.enabled = aim && aim.IsBinocularsZoomed;
    }
}
