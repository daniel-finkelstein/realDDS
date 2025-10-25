using System;
using Shin_Megami_Tensei_Models;
using Shin_Megami_Tensei.Utils;
using Shin_Megami_Tensei.Actions;

namespace Shin_Megami_Tensei.Combat
{
    internal static class SkillFactory
    {
        internal static Skill FromModel(Shin_Megami_Tensei_Models.Skill chosen)
        {
            if (string.Equals(chosen.Name, "Sabbatma", StringComparison.OrdinalIgnoreCase))
                return new ConcreteSkill(chosen, new NoTargetTargeting(), new SummonEffect(isSabbatma: true));

            if (string.Equals(chosen.Name, "Invitation", StringComparison.OrdinalIgnoreCase))
                return new ConcreteSkill(chosen, new NoTargetTargeting(), new SummonEffect(isSabbatma: false));
            
            if (string.Equals(chosen.Type, "Heal", StringComparison.OrdinalIgnoreCase))
            {
                bool revive =
                    string.Equals(chosen.Name, "Recarm",      StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(chosen.Name, "Samarecarm",  StringComparison.OrdinalIgnoreCase);

                return revive
                    ? new ConcreteSkill(chosen, new SingleAllyDeadTargeting(),  new ReviveEffect())
                    : new ConcreteSkill(chosen, new SingleAllyAliveTargeting(), new HealEffect());
            }
            
            bool isClaw = (chosen.Name ?? "").Trim().ToLowerInvariant().EndsWith(" claw");
            IHitPolicy hitPolicy = isClaw ? new TeamKDeterministicHits(1, 3) : new SingleHitPolicy();

            return new ConcreteSkill(chosen, new SingleEnemyTargeting(), new DamageEffect(hitPolicy));
        }
        
        private sealed class ConcreteSkill : Skill
        {
            public ConcreteSkill(Shin_Megami_Tensei_Models.Skill model, Targeting targeting, Effect effect)
            {
                Legacy    = model;
                Targeting = targeting;
                Effect    = effect;
            }
        }
    }
    
    internal abstract class Skill
    {
        protected Shin_Megami_Tensei_Models.Skill Legacy { get; init; } = null!;

        public string Name  => Legacy.Name;
        public int    Cost  => Legacy.Cost;
        public string Type  => Legacy.Type;
        public int    Power => Legacy.Power;

        protected Targeting Targeting { get; init; } = null!;
        protected Effect    Effect    { get; init; } = null!;

        internal bool TryExecute(in ActionHandler.ActionContext ctx, out ActionHandler.ActionEffect effect)
        {
            effect = default;

            // 1) Separador inicial
            ctx.View.WriteLine(CombatLogic.TextSeparator);

            // 2) Targeting opcional
            IReadOnlyList<Unit> targets = Array.Empty<Unit>();
            if (Targeting.RequiresSelection)
            {
                targets = Targeting.Select(in ctx, ctx.Actor);
                if (targets.Count == 0) return false;

                // 3) Segundo separador (comportamiento histórico)
                ctx.View.WriteLine(CombatLogic.TextSeparator);
            }
            
            bool payHere = !(Effect is IPaysCostInternally);
            if (payHere)
            {
                var stats = ctx.Actor.Stats;
                if (stats.ManaPoints < Legacy.Cost) return false;
                stats.ManaPoints = Math.Max(0, stats.ManaPoints - Legacy.Cost);
            }

            // 5) Ejecutar
            return Effect.Apply(in ctx, ctx.Actor, targets, Legacy, out effect);
        }
    }
}
