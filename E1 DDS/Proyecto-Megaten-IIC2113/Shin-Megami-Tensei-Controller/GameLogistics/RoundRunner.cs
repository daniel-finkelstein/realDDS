using System.Collections.Generic;
using Shin_Megami_Tensei_Models;
using Shin_Megami_Tensei_View;
using Shin_Megami_Tensei.Utils;
using Shin_Megami_Tensei.Actions;
using Shin_Megami_Tensei.Board;

namespace Shin_Megami_Tensei.Turns
{
    internal static class RoundRunner
    {
        private readonly record struct RoundContext(
            View View,
            BoardSetup Board,
            Team Player1Team,
            Team Player2Team,
            List<Unit> Order,
            string AttackingSamuraiName,
            string AttackerTag,
            Team AttackingTeam,
            Team DefendingTeam,
            List<Unit> InitialOrder,
            List<Unit> OriginalRoster);
        
        public static void PlayRound(View view, BoardSetup board, int player, Team player1Team, Team player2Team)
        {
            var (attackingTeam, defendingTeam, attackerTag, _, attackingSamuraiName, _) =
                TeamUtils.ResolveSidesAndTags(player, player1Team, player2Team);

            var order        = TeamUtils.GetOrderForPlayer(board, player);
            var initialOrder = new List<Unit>(order);

            TeamUtils.EnsureOriginalOrderSnapshot(player1Team);
            TeamUtils.EnsureOriginalOrderSnapshot(player2Team);

            var originalRoster = TeamUtils.GetOriginalOrderSnapshot(attackingTeam);

            var ctx = new RoundContext(
                view,
                board,
                player1Team,
                player2Team,
                order,
                attackingSamuraiName,
                attackerTag,
                attackingTeam,
                defendingTeam,
                initialOrder,
                originalRoster
            );

            ViewRenderer.PrintRoundStart(ctx.View, ctx.AttackingSamuraiName, ctx.AttackerTag, ctx.Player1Team, ctx.Player2Team);

            if (order.Count == 0)
            {
                ViewRenderer.PrintCounters(ctx.View, 0, 0);
                return;
            }

            RunRoundLoop(in ctx);
        }
        
        private static void RunRoundLoop(in RoundContext ctx)
        {
            int cursor = 0;
            int full   = ctx.Order.Count;
            int blink  = 0;

            ViewRenderer.ShowRoundStatus(ctx.View, ctx.Order, cursor, full, blink);

            while (HasRemainingTurns(ctx.Board, full, blink) &&
                   ExecuteTurnAndAdvance(ctx, ref cursor, ref full, ref blink)) { }
        }

        private static bool HasRemainingTurns(BoardSetup board, int full, int blink) =>
            (full > 0 || blink > 0) && !board.IsBattleOver();

        private static bool ExecuteTurnAndAdvance(RoundContext ctx, ref int cursor, ref int full, ref int blink)
        {
            int orderCountBefore = ctx.Order.Count;

            if (!IsPlayTurn(ctx, cursor, out var effect)) return false;

            var counters = new RoundCounters(full, blink);
            var (fullUsed, blinkUsed, blinkGained) =
                counters.ApplyCost(effect.FullTurnsLost, effect.BlinkTurnsGained, effect.BlinkTurnsLost);

            ViewRenderer.ReportTurnConsumption(ctx.View, fullUsed, blinkUsed, blinkGained, effect.Kind);

            full  = counters.Full;
            blink = counters.Blink;

            if (!HasRemainingTurns(ctx.Board, full, blink)) return false;

            int orderCountAfter = ctx.Order.Count;

            bool insertedIntoOrder = orderCountAfter > orderCountBefore;
            if (insertedIntoOrder && ctx.Order.Count > 0)
                cursor = (cursor + 1) % ctx.Order.Count;

            cursor = TeamUtils.AdvanceCursor(ctx.Order, cursor);

            ViewRenderer.ShowInterTurn(ctx.View, ctx.Player1Team, ctx.Player2Team, ctx.Order, cursor, full, blink);
            return true;
        }

        private static bool IsPlayTurn(in RoundContext ctx, int cursor, out ActionHandler.ActionEffect effect)
        {
            effect = default;

            var actor = TeamUtils.GetNextAliveUnitFromCursor(ctx.Order, cursor);
            if (actor is null) return false;

            var actionCtx = new ActionHandler.ActionContext(
                ctx.View,
                actor,
                ctx.AttackingSamuraiName,
                ctx.AttackerTag,
                ctx.AttackingTeam,
                ctx.DefendingTeam,
                ctx.Order,
                ctx.InitialOrder,
                ctx.OriginalRoster
            );

            effect = ActionHandler.RunActionSelectionLoop(in actionCtx);
            return true;
        }
    }
}
