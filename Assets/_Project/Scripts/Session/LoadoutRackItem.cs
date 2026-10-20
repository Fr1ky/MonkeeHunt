using TMPro;
using UnityEngine;

// An item on the hub rack. A hunter looks at it and presses E: their selected slot takes this item
// (a gun already in another slot swaps places; see HunterLoadout.TryPick). Others are told to pick the
// Hunters side first (HubHint).
public class LoadoutRackItem : MonoBehaviour, IInteractable {

    [SerializeField]
    private LoadoutItem item;

    [SerializeField]
    private TMP_Text label;

    public LoadoutItem Item => item;

    private void Awake() {
        if (label) {
            label.text = LoadoutCode.DisplayName(item);
        }
    }

    public void Interact(PlayerNetworkController player) {
        if (!player.TryGetComponent(out PlayerInfo info) || info.Team != Team.Hunters) {
            return;
        }
        if (player.TryGetComponent(out HunterLoadout loadout) && loadout.enabled) {
            loadout.TryPick(item);
        }
    }
}
