using System;
using Shin_Megami_Tensei_Models;
using Shin_Megami_Tensei_View;
using Shin_Megami_Tensei.Combat;

namespace Shin_Megami_Tensei.Actions;

internal sealed class SkillSingleHitAction : ICombatAction
{
    private readonly Skill _skill;
    public SkillSingleHitAction(Skill skill) => _skill = skill;

    public bool IsExecuteActionSuccess(in ActionHandler.ActionContext ctx, out ActionHandler.ActionEffect effect)
    {
        ctx.View.WriteLine(CombatLogic.TextSeparator);

        if (!IsSelectTarget(ctx, out var target)) { effect = default; return false; }

        ctx.View.WriteLine(CombatLogic.TextSeparator);

        if (!IsPayManaCost(ctx.Actor.Stats, _skill.Cost)) { effect = default; return false; }

        var element  = Skillhandler.MapSkillTypeToElement(_skill.Type);
        var castVerb = Skillhandler.DescribeElementCast(element);
        WriteCastLine(ctx.View, ctx.Actor, target, castVerb);

        decimal baseSkill = ComputeBaseSkill(
            offensiveStat: Skillhandler.GetSkillOffensiveStat(ctx.Actor, _skill.Type),
            powerPercent : _skill.Power
        );

        var affinity = ResolveAffinityForSkill(ctx.Actor, target, element);

        var outcome = ApplyAffinityAndComputeOutcome(affinity, baseSkill);

        ReportAffinityAmount(ctx.View, ctx.Actor, target, affinity, outcome);
        ApplyOutcomeAndPrint(ctx, target, outcome);

        effect = ActionHandler.BuildEffectForAffinity(affinity, ActionHandler.ActionKind.Skill);
        return true;
    }
    

    private static bool IsSelectTarget(in ActionHandler.ActionContext ctx, out Unit target)
    {
        target = Menus.SelectTarget(ctx.View, ctx.Actor.Name, ctx.DefendingTeam);
        return target is not null;
    }

    private static bool IsPayManaCost(Stats stats, int cost)
    {
        if (stats.ManaPoints < cost) return false;
        stats.ManaPoints = Math.Max(0, stats.ManaPoints - cost);
        return true;
    }

    private static void WriteCastLine(View view, Unit actor, Unit target, string verb)
    {
        view.WriteLine($"{actor.Name} {verb} a {target.Name}");
    }

    private static decimal ComputeBaseSkill(int offensiveStat, int powerPercent)
    {
        decimal raw = (decimal)Math.Sqrt((double)((decimal)offensiveStat * powerPercent));
        return raw < 0 ? 0 : raw;
    }

    private static Affinity ResolveAffinityForSkill(Unit attacker, Unit target, Element element)
    {
        if (element == Element.Almighty) return Affinity.Neutral;
        var resolver = new DefaultAffinityResolver();
        return resolver.Resolve(attacker, target, element);
    }

    private static AffinityService.Outcome ApplyAffinityAndComputeOutcome(Affinity affinity, decimal baseSkill)
    {
        var affSvc = new AffinityService();
        return affSvc.Apply(affinity, baseSkill);
    }

    private static void ReportAffinityAmount(View view, Unit actor, Unit target, Affinity affinity, AffinityService.Outcome outcome)
    {
        int amountForText = affinity switch
        {
            Affinity.Repel => outcome.DamageToAttacker,
            Affinity.Drain => outcome.HealOnDefender,
            _              => 0
        };
        ViewRenderer.ReportAffinity(view, actor, target, affinity, amountForText);
    }

    private static void ApplyOutcomeAndPrint(in ActionHandler.ActionContext ctx, Unit target, AffinityService.Outcome outcome)
    {
        if (outcome.DamageToDefender > 0)
        {
            DamageSystem.ApplyDamage(ctx.DefendingTeam, target, outcome.DamageToDefender);
            ctx.View.WriteLine($"{target.Name} recibe {outcome.DamageToDefender} de daño");
            ctx.View.WriteLine($"{target.Name} termina con HP:{target.Stats.HealthPoints}/{target.Stats.MaximumHealthPoints}");
            return;
        }

        if (outcome.DamageToAttacker > 0)
        {
            DamageSystem.ApplyDamage(ctx.AttackingTeam, ctx.Actor, outcome.DamageToAttacker);
            ctx.View.WriteLine($"{ctx.Actor.Name} termina con HP:{ctx.Actor.Stats.HealthPoints}/{ctx.Actor.Stats.MaximumHealthPoints}");
            return;
        }

        if (outcome.HealOnDefender > 0)
        {
            DamageSystem.Heal(target, outcome.HealOnDefender);

        }

        ctx.View.WriteLine($"{target.Name} termina con HP:{target.Stats.HealthPoints}/{target.Stats.MaximumHealthPoints}");
    }
    
}
