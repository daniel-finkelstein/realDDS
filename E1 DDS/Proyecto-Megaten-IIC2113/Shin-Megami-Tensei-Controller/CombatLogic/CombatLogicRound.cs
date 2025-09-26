using System;
using System.Collections.Generic;
using Shin_Megami_Tensei_Models;
using Shin_Megami_Tensei_View;

namespace Shin_Megami_Tensei;

public static partial class CombatLogic
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
        Team DefendingTeam);
    private static void PlayRound(View view, BoardSetup board, int player, Team player1Team, Team player2Team)
    {
        var (attackingTeam, defendingTeam, attackerTag, _, attackingSamuraiName, _) =
            ResolveSidesAndTags(player, player1Team, player2Team);
        

        var turnUnitOrder = GetOrderForPlayer(board, player);
        var roundContext = new RoundContext(view, board, player1Team, player2Team, turnUnitOrder, attackingSamuraiName, attackerTag, attackingTeam, defendingTeam);
        ShowRoundStart(roundContext);
        if (CheckNoTurns(view, turnUnitOrder)) return;
        
        RunRound(in roundContext);

    }

    private static void ShowRoundStart(RoundContext roundContext)
    {
        PrintRoundHeader(roundContext.View, roundContext.AttackingSamuraiName, roundContext.AttackerTag);
        PrintBoards(roundContext.View, roundContext.Player1Team, roundContext.Player2Team);
    }


    private static bool CheckNoTurns(View view, List<Unit> order)
    {
        if (order.Count > 0) return false;
        PrintCounters(view, 0, 0);
        return true;
    }

    
    private static void RunRound(in RoundContext roundContext)
    {
        var (cursor, fullTurns) = InitRoundState(roundContext.Order);
        int blinkingTurns = 0;

        ShowRoundStatus(roundContext.View, roundContext.Order, cursor, fullTurns, blinkingTurns);
        RoundLoop(in roundContext, ref cursor, ref fullTurns, ref blinkingTurns);
    }

    
    private static (int cursor, int fullTurns) InitRoundState(List<Unit> order)
        => (0, order.Count);

    private static bool CanProceed(BoardSetup board, int fullTurns, int blinkingTurns) =>
        (fullTurns > 0 || blinkingTurns > 0) && !board.BattleOver();


    private static void RoundLoop(in RoundContext rc, ref int cursor, ref int fullTurns, ref int blinkingTurns)
    {
        while (CanProceed(rc.Board, fullTurns, blinkingTurns) &&
               ExecuteRoundStep(rc, ref cursor, ref fullTurns, ref blinkingTurns))
        { }
    }

    

    private static bool ExecuteRoundStep(RoundContext rc, ref int cursor, ref int fullTurns, ref int blinkingTurns)
    {
        if (!PlayTurn(in rc, cursor, out var effect)) return false;

        int fullUsed = 0, blinkUsed = 0;

        // 1) Pagar el costo base con Blink si hay; si no, con Full
        bool anyBasePaidWithFull = false;
        int baseCost = Math.Max(0, effect.FullTurnsLost);
        for (int i = 0; i < baseCost; i++)
        {
            if (blinkingTurns > 0) { blinkingTurns--; blinkUsed++; }
            else { fullTurns--; fullUsed++; anyBasePaidWithFull = true; }
        }

        // 2) Penalidad extra de blink (no convierte a Full si falta)
        int extraBlinkLoss = Math.Min(Math.Max(0, effect.BlinkTurnsLost), blinkingTurns);
        blinkingTurns -= extraBlinkLoss;
        blinkUsed += extraBlinkLoss;

        // 3) Recompensa de blink solo si se pagó parte del costo base con Full
        int blinkGainedApplied = anyBasePaidWithFull ? Math.Max(0, effect.BlinkTurnsGained) : 0;
        blinkingTurns += blinkGainedApplied;

        // 4) Imprimir consumo real
        ReportTurnConsumption(rc.View, fullUsed, blinkUsed, blinkGainedApplied, effect.Kind);

        // 5) ¿seguir?
        if (!CanProceed(rc.Board, fullTurns, blinkingTurns)) return false;

        // 6) Avanzar cursor (sin reordenar la lista)
        MoveCursor(rc.Order, ref cursor);

        // 7) Estado intermedio
        ShowInterTurn(rc, cursor, fullTurns, blinkingTurns);
        return true;
    }


    private static void DecreaseTurns(ref int fullTurns) => fullTurns--;

    private static void MoveCursor(List<Unit> order, ref int cursor) =>
        cursor = AdvanceCursor(order, cursor);
    

    private static bool PlayTurn(in RoundContext roundContext, int cursor, out ActionEffect effect)
    {
        effect = default;
        var actor = NextAliveFromCursor(roundContext.Order, cursor);
        if (actor == null) return false;

        var actionContext = new ActionContext(
            roundContext.View,
            actor,
            roundContext.AttackingSamuraiName,
            roundContext.AttackerTag,
            roundContext.AttackingTeam,
            roundContext.DefendingTeam
        );

        effect = ActionLoop(in actionContext); // ← ahora recogemos el efecto (FullLost, BlinkGained, BlinkLost)
        return true;
    }




    private static void ShowRoundStatus(View view, List<Unit> order, int cursor, int fullTurns, int blinkingTurns)
    {
        PrintCounters(view, fullTurns, blinkingTurns); // antes: PrintCounters(view, fullTurns)
        PrintOrderFromCursor(view, order, cursor);
    }

    

    private static void ShowInterTurn(RoundContext roundContext, int cursor, int fullTurns, int blinkingTurns)
    {
        roundContext.View.WriteLine(TextSeparator);
        PrintBoards(roundContext.View, roundContext.Player1Team, roundContext.Player2Team);
        ShowRoundStatus(roundContext.View, roundContext.Order, cursor, fullTurns, blinkingTurns);
    }

}
