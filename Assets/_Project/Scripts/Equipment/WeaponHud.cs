using TMPro;
using UnityEngine;
using UnityEngine.UI;

// Local HUD for the round: crosshair for anyone with a body, plus the hunter's slot bar,
// ammo (magazine / reserve) and reload progress.
public class WeaponHud : MonoBehaviour {

    [SerializeField]
    private GameObject crosshair;

    [SerializeField]
    private GameObject hunterRoot;

    [SerializeField]
    private TMP_Text weaponNameText;

    [SerializeField]
    private TMP_Text ammoText;

    [SerializeField]
    private GameObject reloadRoot;

    [SerializeField]
    private Image reloadFill;

    [SerializeField]
    private Image[] slotBackgrounds = new Image[HunterLoadout.SlotCount];

    [SerializeField]
    private TMP_Text[] slotLabels = new TMP_Text[HunterLoadout.SlotCount];

    [SerializeField]
    private Color slotColor = new Color(0f, 0f, 0f, 0.5f);

    [SerializeField]
    private Color selectedSlotColor = new Color(1f, 0.65f, 0.3f, 0.85f);

    private void Update() {
        var local = PlayerNetworkController.Local;
        var hasBody = local && local.gameObject.scene == gameObject.scene;
        crosshair.SetActive(hasBody && !EscMenu.IsOpen);

        var loadout = hasBody ? local.GetComponent<HunterLoadout>() : null;
        var weapons = hasBody ? local.GetComponent<HunterWeapons>() : null;
        hunterRoot.SetActive(loadout && weapons);
        if (!loadout || !weapons) {
            return;
        }

        for (int i = 0; i < HunterLoadout.SlotCount; i++) {
            var item = loadout.GetSlot(i);
            var countSuffix = item is CageDefinition ? $" x{loadout.CageCount(i)}"
                : item is WalkieTalkieDefinition ? $" x{loadout.WalkieCalls(i)}"
                : "";
            slotLabels[i].text = $"{i + 1}  {(item ? item.displayName : "-")}{countSuffix}";
            slotBackgrounds[i].color = i == loadout.SelectedSlot ? selectedSlotColor : slotColor;
        }

        var weapon = weapons.CurrentWeapon;
        var selected = loadout.Selected;
        weaponNameText.text = selected ? selected.displayName : "";
        ammoText.text = weapons.TryGetAmmo(weapon, out var magazine, out var reserve) ? $"{magazine} / {reserve}"
            : selected is CageDefinition ? Count(loadout.CageCount(loadout.SelectedSlot), "cage")
            : selected is WalkieTalkieDefinition ? Count(loadout.WalkieCalls(loadout.SelectedSlot), "call")
            : "";

        reloadRoot.SetActive(weapons.IsReloading);
        if (weapons.IsReloading) {
            reloadFill.fillAmount = weapons.ReloadProgress;
        }
    }

    private static string Count(int n, string noun) => $"{n} {noun}{(n == 1 ? "" : "s")}";
}
