using System;
using System.Collections.Generic;
using Shin_Megami_Tensei_Models;
using Shin_Megami_Tensei_View;

namespace Shin_Megami_Tensei;

public static partial class CombatLogic
{
    private const string TextSeparator = "----------------------------------------";

    public static void Run(View view, Team player1Team, Team player2Team)
    {
        var (clonedPlayer1Team, clonedPlayer2Team) = PrepareTeamsForGame(player1Team, player2Team);
        var board = new BoardSetup(clonedPlayer1Team, clonedPlayer2Team);
        PlayBattle(view, board, clonedPlayer1Team, clonedPlayer2Team);
    }

    private static (Team clonedPlayer1, Team clonedPlayer2) PrepareTeamsForGame(Team player1Team, Team player2Team)
    {
        var clonedPlayer1Team = CloneTeam(player1Team);
        var clonedPlayer2Team = CloneTeam(player2Team);
        ResetTeamToMax(clonedPlayer1Team);
        ResetTeamToMax(clonedPlayer2Team);
        return (clonedPlayer1Team, clonedPlayer2Team);
    }

    private static void PlayBattle(View view, BoardSetup board, Team player1Team, Team player2Team)
    {
        while (!board.BattleOver())
        {
            if (TakeRound(view, board, 1, player1Team, player2Team)) break;
            if (TakeRound(view, board, 2, player1Team, player2Team)) break;
        }
    }

    private static bool TakeRound(View view, BoardSetup board, int playerNumber, Team player1Team, Team player2Team)
    {
        PlayRound(view, board, playerNumber, player1Team, player2Team);
        return CheckAndMaybePrintWinner(view, board, player1Team, player2Team, playerNumber);
    }

    private static bool CheckAndMaybePrintWinner(View view, BoardSetup board, Team player1Team, Team player2Team, int actingPlayer)
    {
        if (!board.BattleOver()) return false;
        PrintWinner(view, player1Team, player2Team, actingPlayer);
        return true;
    }

    private static void PrintWinner(View view, Team player1Team, Team player2Team, int actingPlayer)
    {
        view.WriteLine(TextSeparator);
        var (winner, tag) = ResolveWinnerByBoard(player1Team, player2Team, actingPlayer);
        PrintWinnerLine(view, winner, tag);
    }

    private static (Team team, string tag) ResolveWinnerByBoard(Team player1Team, Team player2Team, int actingPlayer)
    {
        bool p1Alive = TeamAliveByBoard(player1Team);
        bool p2Alive = TeamAliveByBoard(player2Team);
        if (p1Alive && !p2Alive) return (player1Team, "J1");
        if (p2Alive && !p1Alive) return (player2Team, "J2");
        return ActingWinner(player1Team, player2Team, actingPlayer);
    }

    private static bool TeamAliveByBoard(Team team) => AliveOnBoard(team) > 0;

    private static (Team team, string tag) ActingWinner(Team player1Team, Team player2Team, int actingPlayer) =>
        (actingPlayer == 1 ? player1Team : player2Team, actingPlayer == 1 ? "J1" : "J2");

    private static void PrintWinnerLine(View view, Team team, string tag) =>
        view.WriteLine($"Ganador: {GetSamuraiName(team)} ({tag})");

}
