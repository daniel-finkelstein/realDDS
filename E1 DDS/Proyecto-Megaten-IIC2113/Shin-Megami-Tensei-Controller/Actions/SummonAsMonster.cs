using System;
using Shin_Megami_Tensei_Models;
using Shin_Megami_Tensei_View;
using Shin_Megami_Tensei.Utils;
using static Shin_Megami_Tensei.CombatLogic;

namespace Shin_Megami_Tensei.Actions;

internal sealed class SummonAsMonsterAction : ICombatAction
{
    public bool IsExecuteActionSuccess(in ActionHandler.ActionContext ctx, out ActionHandler.ActionEffect effect)
    {
        if (!SummonAction.TryPickBenchSummon(in ctx, out var summoned))
        {
            effect = default;
            return false;
        }

        var team = ctx.AttackingTeam;

        int actorSlot   = TeamUtils.FindActorSlot(team, ctx.Actor);
        int idxSummoned = team.TeamUnits.IndexOf(summoned);

        team.TeamUnits[actorSlot - 1] = summoned;
        team.TeamUnits[idxSummoned]   = ctx.Actor;

        TeamUtils.ReplaceUnitInOrder(ctx.Order, ctx.Actor, summoned);

        SummonAction.WriteSummoned(ctx.View, summoned);
        SummonAction.ReorderBenchToOriginal(team);

        effect = new ActionHandler.ActionEffect(0, 1, 1, ActionHandler.ActionKind.Summon);
        return true;
    }
}