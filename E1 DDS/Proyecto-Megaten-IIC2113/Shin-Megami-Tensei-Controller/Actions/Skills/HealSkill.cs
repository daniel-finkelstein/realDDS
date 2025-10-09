using System;
using Shin_Megami_Tensei_Models;
using Shin_Megami_Tensei_View;
using Shin_Megami_Tensei.Utils;
using Shin_Megami_Tensei.Combat;

namespace Shin_Megami_Tensei.Actions;

internal sealed class SkillHealAction : ICombatAction
{
    private readonly Skill _skill;
    public SkillHealAction(Skill skill) => _skill = skill;

    public bool IsExecuteActionSuccess(in ActionHandler.ActionContext ctx, out ActionHandler.ActionEffect effect)
    {
        if (!IsPrepareHeal(ctx, out bool revive, out Unit target))
        {
            effect = default;
            return false;
        }

        int amount = ComputeHealAmount(target, _skill.Power);
        PerformHealAndPlacement(ctx, target, revive, amount);

        PrintHealSummary(ctx.View, target, amount);

        effect = new ActionHandler.ActionEffect(0, 0, 1, ActionHandler.ActionKind.Skill);
        return true;
    }


    private bool IsPrepareHeal(in ActionHandler.ActionContext ctx, out bool revive, out Unit target)
    {
        ctx.View.WriteLine(CombatLogic.TextSeparator);

        revive = IsReviveSkill(_skill.Name);

        target = SelectHealTarget(ctx, revive);
        if (target is null) return false;

        ctx.View.WriteLine(CombatLogic.TextSeparator);

        if (!IsPayManaCost(ctx.Actor.Stats, _skill.Cost)) return false;

        AnnounceHealOrRevive(ctx.View, ctx.Actor.Name, target.Name, revive);
        return true;
    }

    private static void PerformHealAndPlacement(in ActionHandler.ActionContext ctx, Unit target, bool revive, int amount)
    {
        DamageSystem.Heal(target, amount);

        if (!revive || target.Stats.HealthPoints <= 0) return;

        EnsureRevivedIsPlacedSafely(ctx.AttackingTeam, target);

        if (TeamUtils.IsSamurai(target))
        {
            UpdateOrderForRevivedSamurai(ctx.Order, ctx.Actor, target);
        }
    }

    
    private static bool IsReviveSkill(string name) =>
        name.Equals("Recarm", StringComparison.OrdinalIgnoreCase) ||
        name.Equals("Samarecarm", StringComparison.OrdinalIgnoreCase);
    
    private static Unit? SelectHealTarget(in ActionHandler.ActionContext ctx, bool revive)
    {
        return revive
            ? Menus.SelectDeadAllyTarget(ctx.View, ctx.Actor.Name, ctx.AttackingTeam)
            : Menus.SelectAllyTarget(ctx.View, ctx.Actor.Name, ctx.AttackingTeam);
    }
    
    private static bool IsPayManaCost(Stats stats, int cost)
    {
        if (stats.ManaPoints < cost) return false;
        stats.ManaPoints = Math.Max(0, stats.ManaPoints - cost);
        return true;
    }
    
    private static void AnnounceHealOrRevive(View view, string actorName, string targetName, bool revive)
    {
        if (revive)
            view.WriteLine($"{actorName} revive a {targetName}");
        else
            view.WriteLine($"{actorName} cura a {targetName}");
    }
    
    private static int ComputeHealAmount(Unit target, int skillPowerPercent)
    {
        int maxHp = target.Stats.MaximumHealthPoints;
        return (int)((decimal)maxHp * skillPowerPercent / 100m);
    }
    
    private static void UpdateOrderForRevivedSamurai(List<Unit> order, Unit actor, Unit target)
    {
        order.Remove(target);

        int actorIdx = -1;
        for (int i = 0; i < order.Count; i++)
            if (ReferenceEquals(order[i], actor)) { actorIdx = i; break; }

        if (actorIdx < 0) actorIdx = order.Count;
        order.Insert(actorIdx, target);
    }
    
    private static void PrintHealSummary(View view, Unit target, int amount)
    {
        view.WriteLine($"{target.Name} recibe {amount} de HP");
        view.WriteLine($"{target.Name} termina con HP:{target.Stats.HealthPoints}/{target.Stats.MaximumHealthPoints}");
    }
    
    
    private static void EnsureRevivedIsPlacedSafely(Team team, Unit revived)
    {
        EnsureFourFieldSlots(team);

        if (TeamUtils.IsSamurai(revived))
        {
            HandleRevivedSamurai(team, revived);
        }
        else
        {
            HandleRevivedNonSamurai(team, revived);
        }
    }

    private static void EnsureFourFieldSlots(Team team)
    {
        while (team.TeamUnits.Count < 4)
            team.TeamUnits.Add(null);
    }
    
    private static void HandleRevivedSamurai(Team team, Unit revived)
    {
        bool isLeaderOccupiedByRevived = ReferenceEquals(team.TeamUnits[0], revived);

        if (!isLeaderOccupiedByRevived)
        {
            RemoveRevivedFromNonLeaderFieldSlots(team, revived);
            SetLeaderSlotToRevived(team, revived);
        }
        else
        {
            RemoveRevivedFromNonLeaderFieldSlots(team, revived);
        }
    }

    private static void RemoveRevivedFromNonLeaderFieldSlots(Team team, Unit revived)
    {
        for (int i = team.TeamUnits.Count - 1; i >= 1; i--)
            if (ReferenceEquals(team.TeamUnits[i], revived))
                team.TeamUnits.RemoveAt(i);
    }

    private static void SetLeaderSlotToRevived(Team team, Unit revived)
    {
        team.TeamUnits[0] = revived;
    }
    
    private static void HandleRevivedNonSamurai(Team team, Unit revived)
    {
        ClearRevivedFromFieldSlots(team, revived);
        EnsureRevivedOnBench(team, revived);
    }

    private static void ClearRevivedFromFieldSlots(Team team, Unit revived)
    {
        for (int pos = 1; pos <= 3; pos++)
            if (pos < team.TeamUnits.Count && ReferenceEquals(team.TeamUnits[pos], revived))
                team.TeamUnits[pos] = null;
    }

    private static void EnsureRevivedOnBench(Team team, Unit revived)
    {
        if (IsRevivedOnBench(team, revived)) return;
        team.TeamUnits.Add(revived);
    }

    private static bool IsRevivedOnBench(Team team, Unit revived)
    {
        for (int i = 4; i < team.TeamUnits.Count; i++)
            if (ReferenceEquals(team.TeamUnits[i], revived))
                return true;
        return false;
    }

}
