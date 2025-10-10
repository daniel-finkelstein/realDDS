using Shin_Megami_Tensei_Models;

namespace Shin_Megami_Tensei.Combat
{
    public interface IAffinityResolver
    {
        Affinity Resolve(Unit attacker, Unit defender, AttackKind kind);
        Affinity Resolve(Unit attacker, Unit defender, Element element);
    }

    public sealed class DefaultAffinityResolver : IAffinityResolver
    {
        public Affinity Resolve(Unit attacker, Unit defender, AttackKind kind)
        {
            var element = kind switch
            {
                AttackKind.Ranged => Element.Gun,
                AttackKind.Melee  => Element.Phys,
                _ => Element.Neutral
            };
            return Resolve(attacker, defender, element);
        }

        public Affinity Resolve(Unit attacker, Unit defender, Element element)
        {
            return defender.Affinities.Get(element);
        }
    }
}