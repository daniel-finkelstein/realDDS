using System;
using System.Collections.Generic;
using Shin_Megami_Tensei_Models;

namespace Shin_Megami_Tensei;

public static partial class CombatLogic
{
    private static (Team attackingTeam, Team defendingTeam, string attackerTag, string defenderTag, string attackingSamuraiName, string defendingSamuraiName)
    ResolveSidesAndTags(int player, Team team1, Team team2)
    {
        var attackingTeam = (player == 1) ? team1 : team2;
        var defendingTeam = (player == 1) ? team2 : team1;
        var attackerTag = (player == 1) ? "J1" : "J2";
        var defenderTag = (player == 1) ? "J2" : "J1";
        return (attackingTeam, defendingTeam, attackerTag, defenderTag, GetSamuraiName(attackingTeam), GetSamuraiName(defendingTeam));
    }

    private static List<Unit> GetOrderForPlayer(BoardSetup board, int playerNumber) =>
        (playerNumber == 1) ? board.Player1RoundOrder() : board.Player2RoundOrder();

    private static Unit? NextAliveFromCursor(List<Unit> order, int cursor)
    {
        for (int i = 0; i < order.Count; i++)
        {
            int index = (cursor + i) % order.Count;
            if (order[index].Stats.HealthPoints > 0) return order[index];
        }
        return null;
    }

    private static int AdvanceCursor(List<Unit> order, int cursor)
        => (order.Count == 0) ? 0 : (cursor + 1) % order.Count;
    

    private static string GetSamuraiName(Team team) =>
        TryGetSamuraiName(team) ?? TryGetFirstUnitName(team) ?? "Jugador";

    private static string? TryGetSamuraiName(Team team)
    {
        for (int i = 0; i < team.TeamUnits.Count; i++)
            if (IsSamurai(team.TeamUnits[i])) return team.TeamUnits[i]!.Name;
        return null;
    }

    private static string? TryGetFirstUnitName(Team team)
    {
        for (int i = 0; i < team.TeamUnits.Count; i++)
            if (team.TeamUnits[i] != null) return team.TeamUnits[i]!.Name;
        return null;
    }

    private static bool IsSamurai(Unit? unit) =>
        unit != null && string.Equals(unit.Type, "Samurai", StringComparison.OrdinalIgnoreCase);


    private static Unit?[] GetTeamSlots(Team team)
    {
        var slots = new Unit?[4];
        for (int i = 0; i < 4; i++)
            slots[i] = i < team.TeamUnits.Count ? team.TeamUnits[i] : null;
        return slots;
    }

    private static void DefeatTeam(Team team)
    {
        for (int i = 0; i < team.TeamUnits.Count; i++)
        {
            var unit = team.TeamUnits[i];
            if (unit != null)
                unit.Stats.HealthPoints = 0;
        }
    }

    private static int AliveOnBoard(Team team)
    {
        int amountOfAliveUnits = 0;
        for (int i = 0; i < 4; i++)
        {
            var unit = (i < team.TeamUnits.Count) ? team.TeamUnits[i] : null;
            if (unit != null && unit.Stats.HealthPoints > 0) amountOfAliveUnits++;
        }
        return amountOfAliveUnits;
    }
    
    

    private static void ResetTeamToMax(Team team)
    {
        for (int i = 0; i < team.TeamUnits.Count; i++)
        {
            var teamMemberUnit = team.TeamUnits[i];
            if (teamMemberUnit == null) continue;
            teamMemberUnit.Stats.HealthPoints = teamMemberUnit.Stats.MaximumHealthPoints;
            teamMemberUnit.Stats.ManaPoints   = teamMemberUnit.Stats.MaximumManaPoints;
        }
    }

    private static Team CloneTeam(Team team)
    {
        var list = new List<Unit>(team.TeamUnits.Count);
        for (int i = 0; i < team.TeamUnits.Count; i++)
        {
            var clonedUnit = team.TeamUnits[i];
            list.Add(clonedUnit is null ? null : UnitFactory.Clone(clonedUnit));
        }
        return new Team { TeamUnits = list };
    }
    
}
