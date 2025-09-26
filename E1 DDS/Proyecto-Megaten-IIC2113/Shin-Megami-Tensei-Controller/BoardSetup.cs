using System;
using System.Collections.Generic;
using Shin_Megami_Tensei_Models;

namespace Shin_Megami_Tensei;

public class BoardSetup
{
    private readonly List<Unit> team1UnitList;
    private readonly List<Unit> team2UnitList;

    public BoardSetup(Team player1Team, Team player2Team)
    {
        team1UnitList = player1Team.TeamUnits;
        team2UnitList = player2Team.TeamUnits;
    }

    public bool BattleOver() => CountAlive(team1UnitList) == 0 || CountAlive(team2UnitList) == 0;

    private static bool IsAlive(Unit? unit) => unit != null && unit.Stats.HealthPoints > 0;
    private int CountAlive(List<Unit> team)
    {
        int c = 0;
        for (int i = 0; i < 4 && i < team.Count; i++)
            if (IsAlive(team[i])) c++;
        return c;
    }

    public List<Unit> Player1RoundOrder() => BuildRoundOrder(team1UnitList);
    public List<Unit> Player2RoundOrder() => BuildRoundOrder(team2UnitList);

    private List<Unit> BuildRoundOrder(List<Unit> team)
    {
        var aliveUnits = CopyAliveUnits(team);
        SortBySpeedThenLeft(aliveUnits, team);
        return aliveUnits;
    }

    private List<Unit> CopyAliveUnits(List<Unit> team)
    {
        var result = new List<Unit>(4);
        for (int i = 0; i < 4 && i < team.Count; i++)
            if (IsAlive(team[i])) result.Add(team[i]);
        return result;
    }

    private void SortBySpeedThenLeft(List<Unit> aliveUnits, List<Unit> originalTeamOrder)
        => aliveUnits.Sort((sortedUnit1, sortedUnit2) => CompareBySpeedThenLeft(sortedUnit1, sortedUnit2, originalTeamOrder));

    private int CompareBySpeedThenLeft(Unit comparedUnit1, Unit comparedUnit2, List<Unit> originalTeamOrder)
    {
        int comparedUnit1Speed = comparedUnit1.Stats.AttackOrder;
        int comparedUnit2Speed = comparedUnit2.Stats.AttackOrder;
        if (comparedUnit1Speed != comparedUnit2Speed) return comparedUnit2Speed - comparedUnit1Speed;

        int comparedUnit1Index = IndexInTeam(originalTeamOrder, comparedUnit1);
        int comparedUnit2Index = IndexInTeam(originalTeamOrder, comparedUnit2);
        return comparedUnit1Index - comparedUnit2Index;
    }

    private int IndexInTeam(List<Unit> team, Unit unit)
    {
        for (int i = 0; i < 4 && i < team.Count; i++)
            if (ReferenceEquals(team[i], unit)) return i;
        return int.MaxValue;
    }
    
}
