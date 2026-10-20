using TMPro;
using UnityEngine;
using UnityEngine.UI;

// Local HUD for monkeys: throwable slot bar with counts, poop pips, carried food and throw charge.
public class MonkeyHud : MonoBehaviour {

    [SerializeField]
    private GameObject monkeyRoot;

    [SerializeField]
    private Image[] slotBackgrounds = new Image[MonkeyInventory.SlotCount];

    [SerializeField]
    private TMP_Text[] slotLabels = new TMP_Text[MonkeyInventory.SlotCount];

    [SerializeField]
    private Image[] poopPips;

    [SerializeField]
    private TMP_Text foodText;

    [SerializeField]
    private GameObject chargeRoot;

    [SerializeField]
    private Image chargeFill;

    [SerializeField]
    private Color slotColor = new Color(0f, 0f, 0f, 0.5f);

    [SerializeField]
    private Color selectedSlotColor = new Color(0.8f, 0.55f, 0.3f, 0.85f);

    [SerializeField]
    private Color pipFull = new Color(0.55f, 0.35f, 0.15f, 1f);

    [SerializeField]
    private Color pipEmpty = new Color(0f, 0f, 0f, 0.4f);

    private static readonly string[] Names = { "Poop", "Rock", "Peel", "Decoy" };

    private void Update() {
        var local = PlayerNetworkController.Local;
        var inventory = local && local.gameObject.scene == gameObject.scene ? local.GetComponent<MonkeyInventory>() : null;
        monkeyRoot.SetActive(inventory);
        if (!inventory) {
            return;
        }

        for (int i = 0; i < MonkeyInventory.SlotCount; i++) {
            var item = (MonkeyItem)i;
            slotLabels[i].text = $"{i + 1}  {Names[i]}  {inventory.Count(item)}/{inventory.Max(item)}";
            slotBackgrounds[i].color = i == inventory.SelectedSlot ? selectedSlotColor : slotColor;
        }

        var poop = inventory.Count(MonkeyItem.Poop);
        for (int i = 0; i < poopPips.Length; i++) {
            poopPips[i].color = i < poop ? pipFull : pipEmpty;
        }

        var thrower = local.GetComponent<MonkeyThrow>();
        var charging = thrower && thrower.IsCharging;
        chargeRoot.SetActive(charging);
        if (charging) {
            chargeFill.fillAmount = thrower.Charge01;
        }

        foodText.text = $"Food {inventory.Food}/{inventory.FoodMax}  ({inventory.Bananas} banana)  F to eat";
    }
}
