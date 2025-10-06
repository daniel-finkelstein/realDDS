using System;
using Shin_Megami_Tensei_Models;

namespace Shin_Megami_Tensei.Combat
{
    public interface IAttackStatSelector
    {
        int GetOffensiveStat(Unit attacker, AttackKind kind);
    }

    public sealed class DefaultAttackStatSelector : IAttackStatSelector
    {
        public int GetOffensiveStat(Unit attacker, AttackKind kind)
        {
            var s = attacker.Stats;
            return kind switch
            {
                AttackKind.Ranged => s.ShootingPower,       // SKL
                AttackKind.Magic  => s.MagicalAttackPower,  // MAG  ✅
                AttackKind.Melee  => s.PhysicalAttackPower, // STR
                _                 => s.PhysicalAttackPower
            };
        }
    }

    public interface IDamageService
    {
        int Compute(AttackCommand cmd);
    }

    public sealed class DamageService : IDamageService
    {
        private readonly IAttackStatSelector _statSelector;

        public DamageService(IAttackStatSelector? statSelector = null)
        {
            _statSelector = statSelector ?? new DefaultAttackStatSelector();
        }

        public int Compute(AttackCommand cmd)
        {
            int stat = _statSelector.GetOffensiveStat(cmd.Attacker, cmd.Kind);
            int modifier = GetModifier(cmd.Kind);

            long raw = (long)stat * modifier * 114;
            int dmg = (int)(raw / 10000);

            return Math.Max(1, dmg);
        }

        private static int GetModifier(AttackKind kind) => kind switch
        {
            AttackKind.Ranged => 80,
            AttackKind.Magic  => 70,
            AttackKind.Melee  => 54,
            _                 => 54
        };
    }
}