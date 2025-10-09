using System;
using Shin_Megami_Tensei_Models;
using Shin_Megami_Tensei_View;
using Shin_Megami_Tensei.Combat;

namespace Shin_Megami_Tensei.Actions;

internal sealed class SkillMultiHitAction : ICombatAction
{
    private readonly Skill _skill;
    private readonly int _hits;

    public SkillMultiHitAction(Skill skill, int hits)
    {
        _skill = skill;
        _hits  = Math.Max(2, hits);
    }

    public bool IsExecuteActionSuccess(in ActionHandler.ActionContext ctx, out ActionHandler.ActionEffect effect)
    {
        ctx.View.WriteLine(CombatLogic.TextSeparator);

        if (!IsSelectTarget(ctx, out var target)) { effect = default; return false; }

        ctx.View.WriteLine(CombatLogic.TextSeparator);

        if (!IsPayManaCost(ctx.Actor.Stats, _skill.Cost)) { effect = default; return false; }

        var element      = Skillhandler.MapSkillTypeToElement(_skill.Type);
        var castVerb     = Skillhandler.DescribeElementCast(element);
        int offensiveStat = Skillhandler.GetSkillOffensiveStat(ctx.Actor, _skill.Type);
        decimal basePerHit = ComputeBasePerHit(offensiveStat, _skill.Power);
        var affinity       = ResolveAffinityForSkill(ctx.Actor, target, element);

        var lastOutcome = ExecuteHits(ctx, target, element, castVerb, affinity, basePerHit, _hits);

        ReportFinalHp(ctx, target, lastOutcome);

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
    
    private static decimal ComputeBasePerHit(int offensiveStat, int powerPercent)
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

    private static AffinityService.Outcome ExecuteHits(
        in ActionHandler.ActionContext ctx,
        Unit target,
        Element element,
        string castVerb,
        Affinity affinity,
        decimal basePerHit,
        int hits)
    {
        var affSvc = new AffinityService();
        AffinityService.Outcome last = default;

        for (int i = 0; i < hits; i++)
        {
            WriteCastLine(ctx.View, ctx.Actor, target, castVerb);
            last = affSvc.Apply(affinity, basePerHit);
            ReportAffinityOnce(ctx.View, ctx.Actor, target, affinity, last);
            ApplyOutcomeNumbers(ctx, target, last);
        }

        return last;
    }

    private static void WriteCastLine(View view, Unit actor, Unit target, string verb)
    {
        view.WriteLine($"{actor.Name} {verb} a {target.Name}");
    }

    private static void ReportAffinityOnce(View view, Unit actor, Unit target, Affinity affinity, AffinityService.Outcome outcome)
    {
        int amountForText = affinity switch
        {
            Affinity.Repel => outcome.DamageToAttacker,
            Affinity.Drain => outcome.HealOnDefender,
            _              => 0
        };
        ViewRenderer.ReportAffinity(view, actor, target, affinity, amountForText);
    }

    private static void ApplyOutcomeNumbers(in ActionHandler.ActionContext ctx, Unit target, AffinityService.Outcome outcome)
    {
        if (outcome.DamageToDefender > 0)
        {
            DamageSystem.ApplyDamage(ctx.DefendingTeam, target, outcome.DamageToDefender);
            ctx.View.WriteLine($"{target.Name} recibe {outcome.DamageToDefender} de daño");
            return;
        }

        if (outcome.DamageToAttacker > 0)
        {
            DamageSystem.ApplyDamage(ctx.AttackingTeam, ctx.Actor, outcome.DamageToAttacker);
            return;
        }

        if (outcome.HealOnDefender > 0)
        {
            DamageSystem.Heal(target, outcome.HealOnDefender);
            return;
        }
    }

    private static void ReportFinalHp(in ActionHandler.ActionContext ctx, Unit target, AffinityService.Outcome last)
    {
        if (last.DamageToAttacker > 0)
        {
            ctx.View.WriteLine($"{ctx.Actor.Name} termina con HP:{ctx.Actor.Stats.HealthPoints}/{ctx.Actor.Stats.MaximumHealthPoints}");
        }
        else
        {
            ctx.View.WriteLine($"{target.Name} termina con HP:{target.Stats.HealthPoints}/{target.Stats.MaximumHealthPoints}");
        }
    }
    
    
}
