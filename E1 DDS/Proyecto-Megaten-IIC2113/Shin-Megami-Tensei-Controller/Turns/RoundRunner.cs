using System.Collections.Generic;
using Shin_Megami_Tensei_Models;
using Shin_Megami_Tensei_View;
using Shin_Megami_Tensei.Utils;
using Shin_Megami_Tensei.Actions;

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

        public static void PlayRound(View view, BoardSetup board, int player, Team p1, Team p2)
        {
            var (attacking, defending, attackerTag, _, attackingSamurai, _) =
                TeamUtils.ResolveSidesAndTags(player, p1, p2);

            var order        = TeamUtils.GetOrderForPlayer(board, player);
            var initialOrder = new List<Unit>(order);
            
            TeamUtils.EnsureOriginalOrderSnapshot(p1);
            TeamUtils.EnsureOriginalOrderSnapshot(p2);
            
            var originalRoster = TeamUtils.GetOriginalOrderSnapshot(attacking);

            var rc = new RoundContext(
                view, board, p1, p2, order,
                attackingSamurai, attackerTag,
                attacking, defending, initialOrder,
                originalRoster
            );

            ViewRenderer.PrintRoundStart(rc.View, rc.AttackingSamuraiName, rc.AttackerTag, rc.Player1Team, rc.Player2Team);

            if (order.Count == 0)
            {
                ViewRenderer.PrintCounters(rc.View, 0, 0);
                return;
            }

            RunRound(in rc);
        }


        private static void RunRound(in RoundContext rc)
        {
            int cursor = 0;
            int full   = rc.Order.Count;
            int blink  = 0;

            ViewRenderer.ShowRoundStatus(rc.View, rc.Order, cursor, full, blink);

            while (CanProceed(rc.Board, full, blink) &&
                   ExecuteStep(rc, ref cursor, ref full, ref blink)) { }
        }

        private static bool CanProceed(BoardSetup board, int full, int blink) =>
            (full > 0 || blink > 0) && !board.BattleOver();

        private static bool ExecuteStep(RoundContext rc, ref int cursor, ref int full, ref int blink)
        {
            int aliveBefore = TeamUtils.CountAliveUnitsOnBoard(rc.AttackingTeam);

            if (!PlayTurn(in rc, cursor, out var effect)) return false;

            var counters = new RoundCounters(full, blink);
            var (fullUsed, blinkUsed, blinkGained) =
                counters.ApplyCost(effect.FullTurnsLost, effect.BlinkTurnsGained, effect.BlinkTurnsLost);
            
            


            ViewRenderer.ReportTurnConsumption(rc.View, fullUsed, blinkUsed, blinkGained, effect.Kind);

            full  = counters.Full;
            blink = counters.Blink;

            if (!CanProceed(rc.Board, full, blink)) return false;

            int aliveAfter = TeamUtils.CountAliveUnitsOnBoard(rc.AttackingTeam);
            bool summonedIntoEmptySlot = aliveAfter > aliveBefore;

            if (summonedIntoEmptySlot && rc.Order.Count > 0)
                cursor = (cursor + 1) % rc.Order.Count;
            
            cursor = TeamUtils.AdvanceCursor(rc.Order, cursor);

            ViewRenderer.ShowInterTurn(rc.View, rc.Player1Team, rc.Player2Team, rc.Order, cursor, full, blink);
            return true;
        }
        
        private static bool PlayTurn(in RoundContext rc, int cursor, out ActionHandler.ActionEffect effect)
        {
            effect = default;
            
            var actor = TeamUtils.GetNextAliveUnitFromCursor(rc.Order, cursor);
            if (actor == null) return false;

            var actx = new ActionHandler.ActionContext(
                rc.View,
                actor,
                rc.AttackingSamuraiName,
                rc.AttackerTag,
                rc.AttackingTeam,
                rc.DefendingTeam,
                rc.Order,
                rc.InitialOrder,
                rc.OriginalRoster
            );

            effect = ActionHandler.RunActionSelectionLoop(in actx);
            return true;
        }
    }
}
