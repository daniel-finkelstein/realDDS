using System;
using System.Collections.Generic;
using Shin_Megami_Tensei_Models;
using Shin_Megami_Tensei_View;
using Shin_Megami_Tensei.Combat;
using Shin_Megami_Tensei.Utils;

namespace Shin_Megami_Tensei.Actions;

internal static class ActionHandler
{
    internal enum ActionKind { Pass, Attack, Skill, Summon, Surrender }

    internal readonly record struct ActionEffect(
        int FullTurnsLost,
        int BlinkTurnsGained,
        int BlinkTurnsLost,
        ActionKind Kind);

    internal readonly record struct ActionContext(
        View View,
        Unit Actor,
        string AttackingSamuraiName,
        string AttackerTag,
        Team AttackingTeam,
        Team DefendingTeam,
        List<Unit> Order,
        List<Unit> InitialOrder,
        List<Unit> OriginalRoster);

    public static ActionEffect RunActionSelectionLoop(in ActionContext ctx)
    {
        for (int i = 0; ; i++)
        {
            if (i > 0) ctx.View.WriteLine(CombatLogic.TextSeparator);

            Menus.ShowActionMenu(ctx.View, ctx.Actor);
            int selectedAction = Menus.ReadMenuInput(ctx.View);
            Menus.AfterSelectionSeparator(ctx.View);

            if (TryExecuteSelectedAction(in ctx, selectedAction, out var effect))
                return effect;
        }
    }

    internal static ActionEffect BuildEffectForAffinity(Affinity affinity, ActionKind kind)
    {
        return affinity switch
        {
            Affinity.Weak    => new ActionEffect(FullTurnsLost: 1, BlinkTurnsGained: 1, BlinkTurnsLost: 0, kind),
            Affinity.Neutral => new ActionEffect(FullTurnsLost: 0, BlinkTurnsGained: 0, BlinkTurnsLost: 1, kind),
            Affinity.Resist  => new ActionEffect(FullTurnsLost: 0, BlinkTurnsGained: 0, BlinkTurnsLost: 1, kind),
            Affinity.Null    => new ActionEffect(FullTurnsLost: 0, BlinkTurnsGained: 0, BlinkTurnsLost: 2, kind),
            Affinity.Repel   => new ActionEffect(FullTurnsLost: int.MaxValue, BlinkTurnsGained: 0, BlinkTurnsLost: int.MaxValue, kind),
            Affinity.Drain   => new ActionEffect(FullTurnsLost: int.MaxValue, BlinkTurnsGained: 0, BlinkTurnsLost: int.MaxValue, kind),
            _                => new ActionEffect(FullTurnsLost: 1, BlinkTurnsGained: 0, BlinkTurnsLost: 0, kind),
        };
    }
    
    private static bool TryExecuteSelectedAction(in ActionContext ctx, int input, out ActionEffect effect)
    {
        ICombatAction? action = TeamUtils.IsSamurai(ctx.Actor)
            ? MapSamuraiMenuToAction(input)
            : MapMonsterMenuToAction(input);

        if (action is null) { effect = default; return false; }
        return action.IsExecuteActionSuccess(in ctx, out effect);
    }

    private static ICombatAction? MapSamuraiMenuToAction(int input)
    {
        var selection = MenuMap.MapForSamurai(input);
        return selection switch
        {
            ActionSelection.Surrender => new SurrenderAction(),
            ActionSelection.Pass      => new PassAction(),
            ActionSelection.UseSkill  => new Skillhandler(),
            ActionSelection.Summon    => new SummonAction(actorIsSamurai: true),
            ActionSelection.Shoot     => new AttackAction(AttackAction.AttackMode.Ranged),
            ActionSelection.Attack    => new AttackAction(AttackAction.AttackMode.Melee),
            _                         => null
        };
    }

    private static ICombatAction? MapMonsterMenuToAction(int input)
    {
        var selection = MenuMap.MapForMonster(input);
        return selection switch
        {
            ActionSelection.Pass      => new PassAction(),
            ActionSelection.UseSkill  => new Skillhandler(),
            ActionSelection.Summon    => new SummonAction(actorIsSamurai: false),
            ActionSelection.Shoot     => new AttackAction(AttackAction.AttackMode.Ranged),
            ActionSelection.Attack    => new AttackAction(AttackAction.AttackMode.Melee),
            _                         => null
        };
    }
}
