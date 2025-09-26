using System;
using System.Collections.Generic;
using Shin_Megami_Tensei_Models;

namespace Shin_Megami_Tensei;

public static partial class CombatLogic
{
    private static void ApplyDamage(Team defenderTeam, Unit target, int damage)
    {
        DealDamageTo(target, damage);
        RemoveCorpseIfNonSamurai(defenderTeam, target);
    }

    private static void DealDamageTo(Unit target, int damage) =>
        target.Stats.HealthPoints = ClampHp(target.Stats.HealthPoints - damage);

    private static int ClampHp(int healthPoints) => Math.Max(0, healthPoints);

    private static void RemoveCorpseIfNonSamurai(Team team, Unit unit)
    {
        if (!IsDead(unit)) return;
        if (IsSamurai(unit)) return;
        RemoveUnitReference(team, unit);
    }

    private static bool IsDead(Unit unit) => unit.Stats.HealthPoints == 0;
    

    private static void RemoveUnitReference(Team team, Unit unit)
    {
        int index = FindUnitIndex(team, unit);
        if (index >= 0) team.TeamUnits[index] = null!;
    }

    private static int FindUnitIndex(Team team, Unit unit)
    {
        for (int i = 0; i < team.TeamUnits.Count; i++)
            if (ReferenceEquals(team.TeamUnits[i], unit)) return i;
        return -1;
    }


    private static int ComputeBasicDamage(Unit actor, bool isShoot)
    {
        int stat = isShoot ? actor.Stats.ShootingPower : actor.Stats.PhysicalAttackPower;
        int modifier = isShoot ? 80 : 54;

        long raw = (long)stat * modifier * 114;
        int dmg = (int)(raw / 10000);

        return Math.Max(1, dmg);
    }
}