using System;
using System.Collections.Generic;
using Shin_Megami_Tensei_Models;

public static class UnitFactory
{
    public static Unit Clone(Unit u)
    {
        var stats  = CloneStats(u.Stats);
        IEnumerable<Skill>? skills = u.Skills;
        var affinities = u.Affinities;
        return u switch
        {
            Samurai => new Samurai(u.Name, stats, skills, affinities),
            Monster => new Monster(u.Name, stats, skills, affinities),
            _ => throw new NotSupportedException($"No sé clonar {u.GetType().Name}")
        };
    }

    private static Stats CloneStats(Stats s)
    {
        var copy = new Stats(
            s.HealthPoints,
            s.ManaPoints,
            s.PhysicalAttackPower,
            s.ShootingPower,
            s.MagicalAttackPower,
            s.AttackOrder,
            s.AbilityEffectiveness
        );
        copy.MaximumHealthPoints = s.MaximumHealthPoints;
        copy.MaximumManaPoints   = s.MaximumManaPoints;
        return copy;
    }
}