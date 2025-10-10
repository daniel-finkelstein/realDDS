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
                    return new Outcome(DamageToDefender: 0, DamageToAttacker: F(baseDamage), HealOnDefender: 0);

                case Affinity.Drain:
                    return new Outcome(DamageToDefender: 0, DamageToAttacker: 0, HealOnDefender: F(baseDamage));

                default:
                    return new Outcome(F(baseDamage), 0, 0);
            }
        }
        
        public Outcome Apply(Affinity affinity, double baseDamage)
        {
            if (baseDamage < 0) baseDamage = 0;
            return Apply(affinity, (decimal)baseDamage);
        }
    }
}
