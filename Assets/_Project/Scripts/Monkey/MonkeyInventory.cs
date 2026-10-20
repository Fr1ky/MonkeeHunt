using System;
using UnityEngine;

// Monkey slots (1 Poop, 2 Rock, 3 Banana peel, 4 Decoy) and carried food.
public enum MonkeyItem : byte {
    Poop,
    Rock,
    Peel,
    Decoy
}

public enum PickupType : byte {
    Rock,
    Fruit,
    Banana
}

// Owner-side counts (like hunter ammo). Keys 1-4 / wheel select the throwable; F eats food:
// +3 poop, and a banana also gives a peel. Eats a banana first when there is room for its peel.
public class MonkeyInventory : MonoBehaviour {

    public const int SlotCount = 4;

    [SerializeField]
    private PlayerNetworkController player;

    [SerializeField]
    private PlayerInputReader input;

    // Caged: poop stays selected (eating still works).
    [SerializeField]
    private NetTrap net;

    [Header("Caps")]
    [SerializeField, Min(1)]
    private int poopMax = 5;

    [SerializeField, Min(0)]
    private int rockMax = 3;

    [SerializeField, Min(0)]
    private int peelMax = 2;

    [SerializeField, Min(0)]
    private int decoyMax = 3;

    [SerializeField, Min(0)]
    private int foodMax = 3;

    [SerializeField, Min(1)]
    private int poopPerFood = 3;

    private readonly int[] counts = new int[SlotCount];
    private int fruits;
    private int bananas;
    private float pickupPendingUntil;

    // Safety net in case the server never answers a pickup request.
    private const float PickupTimeout = 2f;

    public int SelectedSlot { get; private set; }
    public MonkeyItem Selected => (MonkeyItem)SelectedSlot;
    public int Fruits => fruits;
    public int Bananas => bananas;
    public int Food => fruits + bananas;
    public int FoodMax => foodMax;

    // Fires on the owner whenever counts or the selection change.
    public event Action OnChanged;

    private void Awake() {
        counts[(int)MonkeyItem.Poop] = poopMax;
        counts[(int)MonkeyItem.Decoy] = decoyMax;
    }

    public int Count(MonkeyItem item) => counts[(int)item];

    public int Max(MonkeyItem item) {
        return item switch {
            MonkeyItem.Poop => poopMax,
            MonkeyItem.Rock => rockMax,
            MonkeyItem.Peel => peelMax,
            _ => decoyMax
        };
    }

    // A pickup request is on its way to the server; block new ones until it is granted or denied,
    // so two quick requests near the carry cap can't use up a spot and give nothing.
    public bool PickupPending => Time.time < pickupPendingUntil;

    public void BeginPickup() {
        pickupPendingUntil = Time.time + PickupTimeout;
    }

    public void EndPickup() {
        pickupPendingUntil = 0f;
    }

    public bool CanPickUp(PickupType type) {
        return type == PickupType.Rock ? Count(MonkeyItem.Rock) < rockMax : Food < foodMax;
    }

    public void AddPickup(PickupType type) {
        EndPickup();
        if (!CanPickUp(type)) {
            return;
        }
        switch (type) {
            case PickupType.Rock:
                counts[(int)MonkeyItem.Rock]++;
                break;
            case PickupType.Fruit:
                fruits++;
                break;
            case PickupType.Banana:
                bananas++;
                break;
        }
        OnChanged?.Invoke();
    }

    // Owner: uses one of the item (throwing). False if none left.
    public bool TryConsume(MonkeyItem item) {
        if (counts[(int)item] <= 0) {
            return false;
        }
        counts[(int)item]--;
        OnChanged?.Invoke();
        return true;
    }

    private void Update() {
        if (!player.isOwner || player.ActionsBlocked) {
            return;
        }
        if (net && net.State == NetState.Caged) {
            Select((int)MonkeyItem.Poop);
            if (input.EatPressed) {
                Eat();
            }
            return;
        }
        var pressed = input.SlotPressed;
        if (pressed >= 0 && pressed < SlotCount) {
            Select(pressed);
        } else {
            var step = input.CycleStep;
            if (step != 0) {
                Select(((SelectedSlot + step) % SlotCount + SlotCount) % SlotCount);
            }
        }
        if (input.EatPressed) {
            Eat();
        }
    }

    private void Select(int slot) {
        if (slot == SelectedSlot) {
            return;
        }
        SelectedSlot = slot;
        OnChanged?.Invoke();
    }

    private void Eat() {
        if (Food <= 0) {
            return;
        }
        var peelRoom = Count(MonkeyItem.Peel) < peelMax;
        if (bananas > 0 && (peelRoom || fruits == 0)) {
            bananas--;
            if (peelRoom) {
                counts[(int)MonkeyItem.Peel]++;
            }
        } else {
            fruits--;
        }
        counts[(int)MonkeyItem.Poop] = Mathf.Min(poopMax, Count(MonkeyItem.Poop) + poopPerFood);
        OnChanged?.Invoke();
    }
}
