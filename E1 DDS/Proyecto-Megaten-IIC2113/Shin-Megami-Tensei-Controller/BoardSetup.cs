using System;
using System.Collections.Generic;
using Shin_Megami_Tensei_Models;

namespace Shin_Megami_Tensei.Board;

public sealed class BoardSetup
{
    private const int FrontlineSize = 4;


    private readonly List<Unit> _team1Units;
    private readonly List<Unit> _team2Units;

    public BoardSetup(Team player1Team, Team player2Team)
    {
        _team1Units = player1Team.TeamUnits;
        _team2Units = player2Team.TeamUnits;
    }


    public bool IsBattleOver() =>
        CountAliveOnFrontline(_team1Units) == 0 || CountAliveOnFrontline(_team2Units) == 0;

    public List<Unit> BuildPlayer1RoundOrder() => BuildFrontlineRoundOrder(_team1Units);

    public List<Unit> BuildPlayer2RoundOrder() => BuildFrontlineRoundOrder(_team2Units);


    private static bool IsAlive(Unit? unit) =>
        unit is not null && unit.Stats.HealthPoints > 0;

    private static int CountAliveOnFrontline(List<Unit> team)
    {
        int alive = 0;
        for (int i = 0; i < FrontlineSize && i < team.Count; i++)
            if (IsAlive(team[i])) alive++;
        return alive;
    }

    private List<Unit> BuildFrontlineRoundOrder(List<Unit> team)
    {
        var aliveFrontline = CollectAliveFrontlineUnits(team);
        SortBySpeedThenBoardPosition(aliveFrontline, team);
        return aliveFrontline;
    }

    private static List<Unit> CollectAliveFrontlineUnits(List<Unit> team)
    {
        var result = new List<Unit>(FrontlineSize);
        for (int i = 0; i < FrontlineSize && i < team.Count; i++)
            if (IsAlive(team[i])) result.Add(team[i]!);
        return result;
    }

    private void SortBySpeedThenBoardPosition(List<Unit> aliveFrontline, List<Unit> originalTeamOrder) =>
        aliveFrontline.Sort((u1, u2) => CompareBySpeedThenBoardPosition(u1, u2, originalTeamOrder));

    private static int CompareBySpeedThenBoardPosition(Unit u1, Unit u2, List<Unit> originalTeamOrder)
    {
        int speed1 = u1.Stats.AttackOrder;
        int speed2 = u2.Stats.AttackOrder;
        if (speed1 != speed2) return speed2 - speed1;

        int index1 = GetFrontlineIndex(originalTeamOrder, u1);
        int index2 = GetFrontlineIndex(originalTeamOrder, u2);
        return index1 - index2;
    }

    private static int GetFrontlineIndex(List<Unit> team, Unit unit)
    {
        for (int i = 0; i < FrontlineSize && i < team.Count; i++)
            if (ReferenceEquals(team[i], unit)) return i;
        return int.MaxValue;
    }
}
