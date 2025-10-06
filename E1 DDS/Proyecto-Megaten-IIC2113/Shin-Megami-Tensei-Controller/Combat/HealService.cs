using System;
using Shin_Megami_Tensei_Models;

namespace Shin_Megami_Tensei.Combat
{
    internal static class HealService
    {
        public static int ComputeAmount(Skill skill, Unit target)
        {
            int missing = target.Stats.MaximumHealthPoints - target.Stats.HealthPoints;
            if (missing <= 0) return 0;

            if (skill.Power >= 100) return missing;
            return Math.Min(skill.Power, missing);
        }
    }
}