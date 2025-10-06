using Shin_Megami_Tensei_Models;
using Shin_Megami_Tensei_View;
using Shin_Megami_Tensei.Turns;
using Shin_Megami_Tensei.Utils;

namespace Shin_Megami_Tensei.Battle;

internal static class BattleRunner
{
    public static void Run(View view, Team player1Team, Team player2Team)
    {
        var (p1, p2) = TeamUtils.PrepareTeamsForGame(player1Team, player2Team);
        var board    = new BoardSetup(p1, p2);
        PlayBattle(view, board, p1, p2);
    }

    private static void PlayBattle(View view, BoardSetup board, Team p1, Team p2)
    {
        while (!board.BattleOver())
        {
            if (HasBattleEndedAfterRound(view, board, 1, p1, p2)) break;
            if (HasBattleEndedAfterRound(view, board, 2, p1, p2)) break;
        }
    }

    private static bool HasBattleEndedAfterRound(View view, BoardSetup board, int player, Team p1, Team p2)
    {
        RoundRunner.PlayRound(view, board, player, p1, p2);
        return HasWinnerAndPrinted(view, board, p1, p2, player);
    }

    private static bool HasWinnerAndPrinted(View view, BoardSetup board, Team p1, Team p2, int actingPlayer)
    {
        if (!board.BattleOver()) return false;
        ViewRenderer.PrintWinner(view, p1, p2, actingPlayer);
        return true;
    }
}