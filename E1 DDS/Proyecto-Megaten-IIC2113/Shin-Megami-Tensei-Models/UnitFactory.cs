using System;
using System.Collections.Generic;
using Shin_Megami_Tensei_Models;

namespace Shin_Megami_Tensei;

public static class UnitFactory
{

    public static Unit Clone(Unit source)
    {
        var clonedStats   = CopyStats(source.Stats);
        IEnumerable<Skill>? skills = source.Skills;
        var affinities    = source.Affinities;

        return source switch
        {
            Samurai => new Samurai(source.Name, clonedStats, skills, affinities),
            Monster => new Monster(source.Name, clonedStats, skills, affinities),
            _       => throw new NotSupportedException($"No sé clonar {source.GetType().Name}")
        };
    }

    private static Stats CopyStats(Stats original)
    {
        var copy = new Stats(
            original.HealthPoints,
            original.ManaPoints,
            original.PhysicalAttackPower,
            original.ShootingPower,
            original.MagicalAttackPower,
            original.AttackOrder,
            original.AbilityEffectiveness
        );
        copy.MaximumHealthPoints = original.MaximumHealthPoints;
        copy.MaximumManaPoints   = original.MaximumManaPoints;
        return copy;
    }
}