
using System;
using Shin_Megami_Tensei_Models;

namespace Shin_Megami_Tensei.Combat
{
    public interface ISkillDamageService
    {
        int Compute(Unit attacker, Skill skill);
    }

    public sealed class SkillDamageService : ISkillDamageService
    {
        public int Compute(Unit attacker, Skill skill)
        {
            int stat = skill.Type.ToLower() switch
            {
                "phys"      => attacker.Stats.PhysicalAttackPower,
                "gun"       => attacker.Stats.ShootingPower,
                "fire" or "ice" or "elec" or "force" or "almighty"
                    => attacker.Stats.MagicalAttackPower,
                _           => attacker.Stats.PhysicalAttackPower
            };
            
            double v = Math.Sqrt(stat * skill.Power);
            int dmg = (int)Math.Floor(v);
            return Math.Max(0, dmg);
        }
    }
}