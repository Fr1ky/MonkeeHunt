using System.Collections.Generic;

// What a hunter can put in a slot (picked on the hub racks). Values are stored, so only append.
public enum LoadoutItem : byte {
    Empty,
    Binoculars,
    Cage,
    WalkieTalkie,
    HuntingRifle,
    TranqGun,
    NetLauncher
}

// A hunter's 5 slots packed into one int (4 bits per slot), so a loadout syncs and saves as one value.
// Rule: each gun at most once; gear (binoculars, cage, walkie-talkie) any number of times; empty is fine.
public static class LoadoutCode {

    public const int SlotCount = 5;

    private const int BitsPerSlot = 4;
    private const int SlotMask = 0xF;
    private const LoadoutItem LastItem = LoadoutItem.NetLauncher;

    // First time / nothing saved: the old fixed loadout.
    public static readonly int Default = Pack(new[] {
        LoadoutItem.HuntingRifle, LoadoutItem.TranqGun, LoadoutItem.NetLauncher, LoadoutItem.Binoculars,
        LoadoutItem.Empty
    });

    public static bool IsGun(LoadoutItem item) {
        return item == LoadoutItem.HuntingRifle || item == LoadoutItem.TranqGun || item == LoadoutItem.NetLauncher;
    }

    public static int Pack(IReadOnlyList<LoadoutItem> items) {
        var packed = 0;
        for (int i = 0; i < SlotCount && i < items.Count; i++) {
            packed = With(packed, i, items[i]);
        }
        return packed;
    }

    public static LoadoutItem Get(int packed, int slot) {
        return (LoadoutItem)((packed >> (slot * BitsPerSlot)) & SlotMask);
    }

    public static int With(int packed, int slot, LoadoutItem item) {
        var shift = slot * BitsPerSlot;
        return (packed & ~(SlotMask << shift)) | (((int)item & SlotMask) << shift);
    }

    // Puts the item in the slot. A gun that is already in another slot swaps places with the slot's old item.
    public static int Assign(int packed, int slot, LoadoutItem item) {
        if (IsGun(item)) {
            for (int j = 0; j < SlotCount; j++) {
                if (j != slot && Get(packed, j) == item) {
                    packed = With(packed, j, Get(packed, slot));
                }
            }
        }
        return With(packed, slot, item);
    }

    // Known items only, nothing outside the 5 slots, each gun at most once.
    public static bool IsValid(int packed) {
        if (packed < 0 || packed >> (SlotCount * BitsPerSlot) != 0) {
            return false;
        }
        var seenGuns = new HashSet<LoadoutItem>();
        for (int i = 0; i < SlotCount; i++) {
            var item = Get(packed, i);
            if (item > LastItem || (IsGun(item) && !seenGuns.Add(item))) {
                return false;
            }
        }
        return true;
    }

    public static string DisplayName(LoadoutItem item) {
        return item switch {
            LoadoutItem.Empty => "Empty",
            LoadoutItem.Binoculars => "Binoculars",
            LoadoutItem.Cage => "Cage (x2)",
            LoadoutItem.WalkieTalkie => "Walkie-talkie (x2)",
            LoadoutItem.HuntingRifle => "Hunting Rifle",
            LoadoutItem.TranqGun => "Tranq Gun",
            LoadoutItem.NetLauncher => "Net Launcher",
            _ => item.ToString()
        };
    }
}
