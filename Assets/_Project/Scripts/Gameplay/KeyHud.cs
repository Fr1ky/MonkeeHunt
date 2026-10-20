using TMPro;
using UnityEngine;

// Local HUD line "Keys: N" while the local body carries cage keys.
public class KeyHud : MonoBehaviour {

    [SerializeField]
    private TMP_Text keysText;

    private void Update() {
        var local = PlayerNetworkController.Local;
        var ring = local && local.gameObject.scene == gameObject.scene ? local.GetComponent<KeyRing>() : null;
        var count = ring ? ring.Count : 0;
        keysText.enabled = count > 0;
        if (count > 0) {
            keysText.text = $"Keys: {count}";
        }
    }
}
