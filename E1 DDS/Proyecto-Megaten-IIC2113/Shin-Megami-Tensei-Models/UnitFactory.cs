using System;
using System.Collections.Generic;
using Shin_Megami_Tensei_Models;

public static class UnitFactory
{
    public static Unit Clone(Unit u)
    {
        var s = CloneStats(u.Stats);

        // Skills: cualquier unidad puede tener skills; las reutilizamos (son inmutables)
        IEnumerable<Skill>? skills = u.Skills;

        return u switch
        {
            Samurai => new Samurai(u.Name, s, skills),
            Monster => new Monster(u.Name, s, skills),
            _ => throw new NotSupportedException($"No sé clonar {u.GetType().Name}")
        };
    }

    private static Stats CloneStats(Stats s)
    {
        // Copia los valores actuales
        var copy = new Stats(
            s.HealthPoints,          // hp actual
            s.ManaPoints,            // mp actual
            s.PhysicalAttackPower,   // str
            s.ShootingPower,         // skl
            s.MagicalAttackPower,    // mag
            s.AttackOrder,           // spd
            s.AbilityEffectiveness   // lck
        );

        // Preserva los máximos originales
        copy.MaximumHealthPoints = s.MaximumHealthPoints;
        copy.MaximumManaPoints   = s.MaximumManaPoints;

        return copy;
    }
}