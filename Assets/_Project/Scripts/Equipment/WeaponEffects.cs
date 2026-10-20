using UnityEngine;

// Implemented by the tranq status (M3 step 3) and the net trap (M3 step 4).
public interface ITranqTarget {
    void AddTranqStacks(int stacks);
}

public interface INetTarget {
    void TrapInNet();
}

// Server: applies a confirmed hit's effect to the target player.
public static class WeaponEffects {

    public static void ApplyServer(WeaponDefinition weapon, GameObject target) {
        switch (weapon.effect) {
            case WeaponEffect.Damage:
                if (target.TryGetComponent(out PlayerHealth health)) {
                    health.ApplyDamage(weapon.effectAmount);
                }
                break;
            case WeaponEffect.Tranq:
                if (target.TryGetComponent(out ITranqTarget tranq)) {
                    tranq.AddTranqStacks(Mathf.Max(1, weapon.effectAmount));
                }
                break;
            case WeaponEffect.Net:
                if (target.TryGetComponent(out INetTarget net)) {
                    net.TrapInNet();
                }
                break;
        }
        // Any gun hit knocks a head-rider off, on top of its normal effect.
        if (target && target.TryGetComponent(out MonkeyRide ride)) {
            ride.ThrowOffServer();
        }
    }
}
