using System;
using Shin_Megami_Tensei_Models;
using Shin_Megami_Tensei_View;
using Shin_Megami_Tensei.Utils;
using static Shin_Megami_Tensei.CombatLogic;

namespace Shin_Megami_Tensei.Actions;

internal sealed class SummonAsSamuraiAction : ICombatAction
{
    public bool IsExecuteActionSuccess(in ActionHandler.ActionContext ctx, out ActionHandler.ActionEffect effect)
    {
        if (!SummonAction.TryPickBenchSummon(in ctx, out var summoned))
        {
            effect = default;
            return false;
        }

        var view = ctx.View;
        var team = ctx.AttackingTeam;

        int boardSlot = SummonAction.ReadSamuraiBoardSlot(view, team);
        if (boardSlot == -1) { effect = default; return false; }

        var previous    = team.TeamUnits[boardSlot - 1];
        int idxSummoned = team.TeamUnits.IndexOf(summoned);

        team.TeamUnits[boardSlot - 1] = summoned;

        if (previous != null)
        {
            if (idxSummoned >= 0) team.TeamUnits[idxSummoned] = previous;
        }
        else
        {
            if (idxSummoned >= 0) team.TeamUnits[idxSummoned] = null;
        }
        
        if (previous != null)
        {
            TeamUtils.ReplaceUnitInOrder(ctx.Order, previous, summoned);
        }
        else
        {
            ctx.Order.Remove(summoned);
            var order    = ctx.Order;
            var summoner = ctx.Actor;
            int summonerIdx = order.FindIndex(u => ReferenceEquals(u, summoner));
            if (summonerIdx < 0) order.Add(summoned);
            else order.Insert(summonerIdx, summoned);
        }

        SummonAction.WriteSummoned(view, summoned);
        SummonAction.ReorderBenchToOriginal(team);

        effect = new ActionHandler.ActionEffect(0, 1, 1, ActionHandler.ActionKind.Summon);
        return true;
    }
}