using System;
using Shin_Megami_Tensei_Models;

namespace Shin_Megami_Tensei.Combat
{
    public sealed class AffinityService
    {
        public readonly record struct Outcome(
            int DamageToDefender,
            int DamageToAttacker,
            int HealOnDefender
        );
        
        public Outcome Apply(Affinity affinity, int baseDamageInt)
            => Apply(affinity, (decimal)baseDamageInt);

        // Cambia: ahora hacemos todo en DECIMAL
        public Outcome Apply(Affinity affinity, decimal baseDamage)
        {
            if (baseDamage < 0) baseDamage = 0;

            static int F(decimal x) => (int)decimal.Floor(x);

            switch (affinity)
            {
                case Affinity.Weak:
                    return new Outcome(DamageToDefender: F(baseDamage * 1.5m), DamageToAttacker: 0, HealOnDefender: 0);

                case Affinity.Neutral:
                    return new Outcome(DamageToDefender: F(baseDamage),      DamageToAttacker: 0, HealOnDefender: 0);

                case Affinity.Resist:
                    return new Outcome(DamageToDefender: F(baseDamage * 0.5m), DamageToAttacker: 0, HealOnDefender: 0);

                case Affinity.Null:
                    return new Outcome(DamageToDefender: 0, DamageToAttacker: 0, HealOnDefender: 0);

                case Affinity.Repel:
                    // Refleja sin modificar y con floor
                    return new Outcome(DamageToDefender: 0, DamageToAttacker: F(baseDamage), HealOnDefender: 0);

                case Affinity.Drain:
                    // Cura por el “daño base”, con floor
                    return new Outcome(DamageToDefender: 0, DamageToAttacker: 0, HealOnDefender: F(baseDamage));

                default: // por si acaso
                    return new Outcome(F(baseDamage), 0, 0);
            }
        }

        // Mantengo tu overload double SOLO como puente, convirtiendo a decimal
        public Outcome Apply(Affinity affinity, double baseDamage)
        {
            if (baseDamage < 0) baseDamage = 0;
            // Convertimos a decimal y volvemos a llamar al core decimal:
            // (La conversión elimina la imprecisión binaria antes del floor)
            return Apply(affinity, (decimal)baseDamage);
        }
    }
}
