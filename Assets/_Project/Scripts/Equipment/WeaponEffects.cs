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

    // True when this hit took the target down: killed it, put it to sleep or netted it.
    public static bool ApplyServer(WeaponDefinition weapon, GameObject target) {
        var downed = false;
        switch (weapon.effect) {
            case WeaponEffect.Damage:
                if (target.TryGetComponent(out PlayerHealth health)) {
                    var wasAlive = !health.IsDead;
                    health.ApplyDamage(weapon.effectAmount);
                    downed = wasAlive && health.IsDead;
                }
                break;
            case WeaponEffect.Tranq:
                if (target.TryGetComponent(out ITranqTarget tranq)) {
                    var wasAwake = tranq is not TranqStatus status || !status.IsAsleep;
                    tranq.AddTranqStacks(Mathf.Max(1, weapon.effectAmount));
                    downed = wasAwake && tranq is TranqStatus after && after.IsAsleep;
                }
                break;
            case WeaponEffect.Net:
                if (target.TryGetComponent(out INetTarget net)) {
                    var wasFree = net is not NetTrap trap || !trap.IsTrapped;
                    net.TrapInNet();
                    downed = wasFree && net is NetTrap after && after.IsTrapped;
                }
                break;
        }
        // Any gun hit knocks a head-rider off, on top of its normal effect.
        if (target && target.TryGetComponent(out MonkeyRide ride)) {
            ride.ThrowOffServer();
        }
        return downed;
    }
}
