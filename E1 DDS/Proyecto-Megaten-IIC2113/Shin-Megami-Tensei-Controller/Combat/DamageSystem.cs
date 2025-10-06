using System;
using Shin_Megami_Tensei_Models;
using Shin_Megami_Tensei.Utils;

namespace Shin_Megami_Tensei.Combat
{
    internal static class DamageSystem
    {
        public static void ApplyDamage(Team defenderTeam, Unit target, int damage)
        {
            target.Stats.HealthPoints = Math.Max(0, target.Stats.HealthPoints - damage);
            RemoveCorpseIfNonSamurai(defenderTeam, target);
        }

        public static void Heal(Unit unit, int amount)
        {
            if (amount <= 0) return;
            unit.Stats.HealthPoints = Math.Min(unit.Stats.MaximumHealthPoints, unit.Stats.HealthPoints + amount);
        }

        private static void RemoveCorpseIfNonSamurai(Team team, Unit unit)
        {
            if (unit.Stats.HealthPoints > 0) return;
            if (TeamUtils.IsSamurai(unit)) return;

            for (int i = 0; i < team.TeamUnits.Count; i++)
                if (ReferenceEquals(team.TeamUnits[i], unit)) { team.TeamUnits[i] = null!; return; }
        }
    }
}