using System;
using System.Collections.Generic;
using System.Linq;
using Shin_Megami_Tensei.Actions;  // ActionHandler, SummonAction
using Shin_Megami_Tensei_Models;   // Unit, Team, Stats, Skill (modelo)
using Shin_Megami_Tensei_View;     // Menus / View
using Shin_Megami_Tensei.Utils;

namespace Shin_Megami_Tensei.Combat
{

    internal interface IHitPolicy { int Hits(in ActionHandler.ActionContext ctx); }

    internal sealed class SingleHitPolicy : IHitPolicy
    {
        public int Hits(in ActionHandler.ActionContext ctx) => 1;
    }
    
    internal sealed class TeamKDeterministicHits : IHitPolicy
    {
        private readonly int _minHitAmount, _maxHitAmount;
        public TeamKDeterministicHits(int minHitAmount, int maxHitAmount) { _minHitAmount = minHitAmount; _maxHitAmount = maxHitAmount; }

        public int Hits(in ActionHandler.ActionContext ctx)
        {
            int len = _maxHitAmount - _minHitAmount + 1;
            int k = SkillRouter.GetTeamSkillK(ctx.AttackingTeam);
            int offset = ((k % len) + len) % len;
            return _minHitAmount + offset;
        }
    }
    

    internal abstract class Effect
    {
        protected static string VerbCast(Element e) => e switch
        {
            Element.Force => "lanza viento",
            Element.Fire  => "lanza fuego",
            Element.Ice   => "lanza hielo",
            Element.Elec  => "lanza electricidad",
            Element.Gun   => "dispara",
            Element.Phys  => "ataca",
            _             => "usa una habilidad sobre"
        };

        internal abstract bool Apply(
            in ActionHandler.ActionContext ctx, Unit actor, IReadOnlyList<Unit> targets,
            Shin_Megami_Tensei_Models.Skill modelSkill,
            out ActionHandler.ActionEffect effect
        );

        protected static decimal BaseSkill(int offensiveStat, int powerPercent)
        {
            decimal raw = (decimal)Math.Sqrt((double)((decimal)offensiveStat * powerPercent));
            return raw < 0 ? 0 : raw;
        }
    }
    
    internal interface IPaysCostInternally { }

    internal sealed class DamageEffect : Effect
    {
        private readonly IHitPolicy _hits;
        public DamageEffect(IHitPolicy hits) { _hits = hits; }

        internal override bool Apply(in ActionHandler.ActionContext ctx, Unit actor, IReadOnlyList<Unit> targets,
                                     Shin_Megami_Tensei_Models.Skill modelSkill, out ActionHandler.ActionEffect effect)
        {
            var target = targets.FirstOrDefault();
            if (target is null) { effect = default; return false; }

            var element  = MapSkillTypeToElement(modelSkill.Type);
            var castVerb = VerbCast(element);

            var resolver = new DefaultAffinityResolver();
            var affSvc   = new AffinityService();

            int offensive   = GetOffensiveStat(actor, modelSkill.Type);
            decimal baseVal = BaseSkill(offensive, modelSkill.Power);

            int times = Math.Max(1, _hits.Hits(in ctx));
            AffinityService.Outcome last = default;
            Affinity finalAffinity = Affinity.Neutral;

            for (int i = 0; i < times; i++)
            {
                ctx.View.WriteLine($"{actor.Name} {castVerb} a {target.Name}");
                var affinity = element == Element.Almighty ? Affinity.Neutral : resolver.Resolve(actor, target, element);
                finalAffinity = affinity;
                last = affSvc.Apply(affinity, baseVal);

                int amountForText = affinity switch
                {
                    Affinity.Repel => last.DamageToAttacker,
                    Affinity.Drain => last.HealOnDefender,
                    _              => 0
                };
                ViewRenderer.ReportAffinity(ctx.View, actor, target, affinity, amountForText);

                if (last.DamageToDefender > 0)
                {
                    DamageSystem.ApplyDamage(ctx.DefendingTeam, target, last.DamageToDefender);
                    ctx.View.WriteLine($"{target.Name} recibe {last.DamageToDefender} de daño");
                }
                else if (last.DamageToAttacker > 0)
                {
                    DamageSystem.ApplyDamage(ctx.AttackingTeam, actor, last.DamageToAttacker);
                }
                else if (last.HealOnDefender > 0)
                {
                    DamageSystem.Heal(target, last.HealOnDefender);
                }
            }

            if (last.DamageToAttacker > 0)
                ctx.View.WriteLine($"{actor.Name} termina con HP:{actor.Stats.HealthPoints}/{actor.Stats.MaximumHealthPoints}");
            else
                ctx.View.WriteLine($"{target.Name} termina con HP:{target.Stats.HealthPoints}/{target.Stats.MaximumHealthPoints}");

            effect = ActionHandler.BuildEffectForAffinity(finalAffinity, ActionHandler.ActionKind.Skill);
            return true;
        }

        private static Element MapSkillTypeToElement(string type) =>
            type.ToLowerInvariant() switch
            {
                "phys"     => Element.Phys,
                "gun"      => Element.Gun,
                "fire"     => Element.Fire,
                "ice"      => Element.Ice,
                "elec"     => Element.Elec,
                "force"    => Element.Force,
                "almighty" => Element.Almighty,
                _          => Element.Neutral
            };

        private static int GetOffensiveStat(Unit attacker, string skillType)
        {
            var s = attacker.Stats;
            return skillType.Equals("Phys", StringComparison.OrdinalIgnoreCase) ? s.PhysicalAttackPower :
                   skillType.Equals("Gun",  StringComparison.OrdinalIgnoreCase) ? s.ShootingPower :
                                  s.MagicalAttackPower;
        }
    }

    internal sealed class HealEffect : Effect
    {
        internal override bool Apply(in ActionHandler.ActionContext ctx, Unit actor, IReadOnlyList<Unit> targets,
                                     Shin_Megami_Tensei_Models.Skill modelSkill, out ActionHandler.ActionEffect effect)
        {
            var target = targets.FirstOrDefault();
            if (target is null) { effect = default; return false; }

            ctx.View.WriteLine($"{actor.Name} cura a {target.Name}");
            int amount = (int)((decimal)target.Stats.MaximumHealthPoints * modelSkill.Power / 100m);
            DamageSystem.Heal(target, amount);
            ctx.View.WriteLine($"{target.Name} recibe {amount} de HP");
            ctx.View.WriteLine($"{target.Name} termina con HP:{target.Stats.HealthPoints}/{target.Stats.MaximumHealthPoints}");

            effect = new ActionHandler.ActionEffect(0, 0, 1, ActionHandler.ActionKind.Skill);
            return true;
        }
    }

    internal sealed class ReviveEffect : Effect
    {
        internal override bool Apply(in ActionHandler.ActionContext ctx, Unit actor, IReadOnlyList<Unit> targets,
                                     Shin_Megami_Tensei_Models.Skill modelSkill, out ActionHandler.ActionEffect effect)
        {
            var team = ctx.AttackingTeam;
            var target = targets.FirstOrDefault();
            if (target is null) { effect = default; return false; }

            int amount = Math.Max(1, (int)((decimal)target.Stats.MaximumHealthPoints * modelSkill.Power / 100m));
            DamageSystem.Heal(target, amount);

            ctx.View.WriteLine($"{actor.Name} revive a {target.Name}");
            ctx.View.WriteLine($"{target.Name} recibe {amount} de HP");
            ctx.View.WriteLine($"{target.Name} termina con HP:{target.Stats.HealthPoints}/{target.Stats.MaximumHealthPoints}");

            EnsureFourFieldSlots(team);
            if (TeamUtils.IsSamurai(target))
            {
                RemoveFromNonLeaderFieldSlots(team, target);
                team.TeamUnits[0] = target;
                UpdateOrderForRevivedSamurai(ctx.Order, actor, target);
            }
            else
            {
                ClearFromFieldSlots(team, target);
                EnsureOnBench(team, target);
            }

            effect = new ActionHandler.ActionEffect(0, 0, 1, ActionHandler.ActionKind.Skill);
            return true;
        }

        private static void EnsureFourFieldSlots(Team team)
        {
            while (team.TeamUnits.Count < 4) team.TeamUnits.Add(null);
        }
        private static void RemoveFromNonLeaderFieldSlots(Team team, Unit revived)
        {
            for (int i = team.TeamUnits.Count - 1; i >= 1; i--)
                if (ReferenceEquals(team.TeamUnits[i], revived))
                    team.TeamUnits.RemoveAt(i);
        }
        private static void ClearFromFieldSlots(Team team, Unit revived)
        {
            for (int pos = 1; pos <= 3; pos++)
                if (pos < team.TeamUnits.Count && ReferenceEquals(team.TeamUnits[pos], revived))
                    team.TeamUnits[pos] = null;
        }
        private static void EnsureOnBench(Team team, Unit revived)
        {
            for (int i = 4; i < team.TeamUnits.Count; i++)
                if (ReferenceEquals(team.TeamUnits[i], revived)) return;
            team.TeamUnits.Add(revived);
        }
        private static void UpdateOrderForRevivedSamurai(List<Unit> order, Unit actor, Unit target)
        {
            order.Remove(target);
            int actorIdx = order.FindIndex(u => ReferenceEquals(u, actor));
            if (actorIdx < 0) actorIdx = order.Count;
            order.Insert(actorIdx, target);
        }
    }

    internal sealed class SummonEffect : Effect, IPaysCostInternally
    {
        private readonly bool _isSabbatma;
        public SummonEffect(bool isSabbatma) { _isSabbatma = isSabbatma; }

        internal override bool Apply(in ActionHandler.ActionContext ctx, Unit actor, IReadOnlyList<Unit> _,
                                     Shin_Megami_Tensei_Models.Skill modelSkill, out ActionHandler.ActionEffect effect)
        {
            var view = ctx.View;
            var team = ctx.AttackingTeam;

            var onFieldAlive = new HashSet<Unit>(ctx.Order);
            var candidates = _isSabbatma
                ? SummonAction.GetAliveBenchOrdered(in ctx)
                : ctx.OriginalRoster.Where(u => !TeamUtils.IsSamurai(u) && !onFieldAlive.Contains(u)).ToList();

            view.WriteLine("Seleccione un monstruo para invocar");
            if (candidates.Count == 0)
            {
                view.WriteLine("1-Cancelar");
                Menus.ReadIndexAllowCancel(view, 1);
                effect = default;
                return false;
            }

            Menus.PrintBenchOptions(view, candidates);
            int pick = Menus.ReadIndexAllowCancel(view, candidates.Count + 1);
            if (pick == candidates.Count + 1) { effect = default; return false; }

            var summoned = candidates[pick - 1];

            view.WriteLine(CombatLogic.TextSeparator);
            view.WriteLine("Seleccione una posición para invocar");

            EnsureFourFieldSlots(team);
            var slots = TeamUtils.GetTeamSlots(team);
            for (int i = 0; i < 3; i++)
            {
                int boardPos = i + 2;
                var occ = slots[boardPos - 1];
                if (occ != null)
                    view.WriteLine($"{i + 1}-{occ.Name} HP:{occ.Stats.HealthPoints}/{occ.Stats.MaximumHealthPoints} MP:{occ.Stats.ManaPoints}/{occ.Stats.MaximumManaPoints} (Puesto {boardPos})");
                else
                    view.WriteLine($"{i + 1}-Vacío (Puesto {boardPos})");
            }
            view.WriteLine("4-Cancelar");

            int posChoice = Menus.ReadIndexAllowCancel(view, 4);
            if (posChoice == 4) { effect = default; return false; }
            int boardSlot = posChoice + 1;

            // Costo de MP aquí (comportamiento histórico)
            var stats = actor.Stats;
            if (stats.ManaPoints < modelSkill.Cost) { effect = default; return false; }
            stats.ManaPoints = Math.Max(0, stats.ManaPoints - modelSkill.Cost);

            var previous    = team.TeamUnits[boardSlot - 1];
            int idxSummoned = team.TeamUnits.IndexOf(summoned);

            team.TeamUnits[boardSlot - 1] = summoned;
            if (previous != null)
            {
                if (idxSummoned >= 0) team.TeamUnits[idxSummoned] = previous;
                else PlacePreviousOnBench(team, previous);
            }
            else
            {
                if (idxSummoned >= 0) team.TeamUnits.RemoveAt(idxSummoned);
            }

            if (previous != null)
            {
                TeamUtils.ReplaceUnitInOrder(ctx.Order, previous, summoned);
            }
            else
            {
                int actorIdx = ctx.Order.FindIndex(u => ReferenceEquals(u, actor));
                if (actorIdx < 0) actorIdx = ctx.Order.Count;
                ctx.Order.Remove(summoned);
                ctx.Order.Insert(actorIdx, summoned);
            }

            view.WriteLine(CombatLogic.TextSeparator);
            view.WriteLine($"{summoned.Name} ha sido invocado");

            if (summoned.Stats.HealthPoints <= 0)
            {
                view.WriteLine($"{actor.Name} revive a {summoned.Name}");
                summoned.Stats.HealthPoints = summoned.Stats.MaximumHealthPoints;
                view.WriteLine($"{summoned.Name} recibe {summoned.Stats.MaximumHealthPoints} de HP");
                view.WriteLine($"{summoned.Name} termina con HP:{summoned.Stats.HealthPoints}/{summoned.Stats.MaximumHealthPoints}");
            }

            bool isMenuSummon = modelSkill.Cost == 0;
            int  blinkGain    = isMenuSummon ? 1 : 0;
            var  kind         = isMenuSummon ? ActionHandler.ActionKind.Summon
                                             : ActionHandler.ActionKind.Skill;

            effect = new ActionHandler.ActionEffect(0, blinkGain, 1, kind);
            return true;
        }

        private static void EnsureFourFieldSlots(Team team)
        {
            while (team.TeamUnits.Count < 4) team.TeamUnits.Add(null);
        }
        private static void PlacePreviousOnBench(Team team, Unit previous)
        {
            int benchHole = team.TeamUnits.FindIndex(4, u => u == null);
            if (benchHole >= 0) team.TeamUnits[benchHole] = previous;
            else team.TeamUnits.Add(previous);
        }
    }
}
