using System;
using System.Collections.Generic;
using Shin_Megami_Tensei_Models;
using Shin_Megami_Tensei_View;
using Shin_Megami_Tensei.Turns;
using Shin_Megami_Tensei.Board;

namespace Shin_Megami_Tensei.Utils;

internal static class TeamUtils
{
    private static readonly Dictionary<Team, List<Unit>> OriginalOrderByTeam = new();
    

    public static (Team clonedPlayer1, Team clonedPlayer2) PrepareTeamsForGame(Team t1, Team t2)
    {
        var p1 = CloneTeam(t1);
        var p2 = CloneTeam(t2);
        ResetTeamToMax(p1);
        ResetTeamToMax(p2);
        return (p1, p2);
    }

    public static (Team attacking, Team defending, string attackerTag, string defenderTag, string attackingSamurai, string defendingSamurai)
        ResolveSidesAndTags(int player, Team t1, Team t2)
    {
        var attacking   = (player == 1) ? t1 : t2;
        var defending   = (player == 1) ? t2 : t1;
        var attackerTag = (player == 1) ? "J1" : "J2";
        var defenderTag = (player == 1) ? "J2" : "J1";

        return (attacking, defending, attackerTag, defenderTag, GetSamuraiName(attacking), GetSamuraiName(defending));
    }

    public static List<Unit> GetOrderForPlayer(BoardSetup board, int playerNumber) =>
        (playerNumber == 1) ? board.BuildPlayer1RoundOrder() : board.BuildPlayer2RoundOrder();

    public static int AdvanceCursor(List<Unit> order, int cursor) =>
        (order.Count == 0) ? 0 : (cursor + 1) % order.Count;

    public static Unit? GetNextAliveUnitFromCursor(List<Unit> order, int cursor)
    {
        for (int i = 0; i < order.Count; i++)
        {
            int index = (cursor + i) % order.Count;
            if (order[index].Stats.HealthPoints > 0) return order[index];
        }
        return null;
    }

    public static string GetSamuraiName(Team team) =>
        TryGetSamuraiName(team) ?? TryGetFirstUnitName(team) ?? "Jugador";

    public static bool IsSamurai(Unit? unit) =>
        unit != null && string.Equals(unit.Type, "Samurai", StringComparison.OrdinalIgnoreCase);

    public static Unit?[] GetTeamSlots(Team team)
    {
        var slots = new Unit?[4];
        for (int i = 0; i < 4; i++)
            slots[i] = i < team.TeamUnits.Count ? team.TeamUnits[i] : null;
        return slots;
    }

    public static void DefeatTeam(Team team)
    {
        for (int i = 0; i < team.TeamUnits.Count; i++)
        {
            var u = team.TeamUnits[i];
            if (u != null) u.Stats.HealthPoints = 0;
        }
    }

    public static int FindActorSlot(Team team, Unit actor)
    {
        for (int i = 0; i < 4 && i < team.TeamUnits.Count; i++)
            if (ReferenceEquals(team.TeamUnits[i], actor))
                return i + 1;
        return 1;
    }

    public static void ReplaceUnitInOrder(List<Unit> order, Unit oldUnit, Unit newUnit)
    {
        for (int i = 0; i < order.Count; i++)
        {
            if (ReferenceEquals(order[i], oldUnit))
            {
                order[i] = newUnit;
                break;
            }
        }
    }

    public static List<Unit> BuildTargetOptions(Team team)
    {
        var opponentUnits = new List<Unit>(4);
        var slots = GetTeamSlots(team);
        for (int i = 0; i < 4; i++)
            if (slots[i]?.Stats.HealthPoints > 0) opponentUnits.Add(slots[i]!);
        return opponentUnits;
    }

    public static void EnsureOriginalOrderSnapshot(Team team)
    {
        if (!OriginalOrderByTeam.ContainsKey(team))
            OriginalOrderByTeam[team] = new List<Unit>(team.TeamUnits);
    }

    public static (Team team, string tag) ResolveWinnerByBoard(Team p1, Team p2, int actingPlayer)
    {
        bool p1Alive = CountAliveUnitsOnBoard(p1) > 0;
        bool p2Alive = CountAliveUnitsOnBoard(p2) > 0;
        if (p1Alive && !p2Alive) return (p1, "J1");
        if (p2Alive && !p1Alive) return (p2, "J2");
        return (actingPlayer == 1 ? p1 : p2, actingPlayer == 1 ? "J1" : "J2");
    }

    public static int CountAliveUnitsOnBoard(Team team)
    {
        int alive = 0;
        for (int i = 0; i < 4; i++)
        {
            var u = (i < team.TeamUnits.Count) ? team.TeamUnits[i] : null;
            if (u != null && u.Stats.HealthPoints > 0) alive++;
        }
        return alive;
    }

    public static List<Skill> GetUsableSkills(Unit unit)
    {
        var usable = new List<Skill>();
        if (unit?.Skills == null) return usable;

        int currentMp = unit.Stats.ManaPoints;
        foreach (var s in unit.Skills)
            if (s != null && s.Cost <= currentMp) usable.Add(s);

        return usable;
    }

    public static List<Unit> GetOriginalOrderSnapshot(Team team)
    {
        if (OriginalOrderByTeam.TryGetValue(team, out var cached) && cached is not null)
            return cached;

        var seen = new HashSet<Unit>(ReferenceEqualityComparer.Instance);
        var snapshot = new List<Unit>();

        foreach (var u in team.TeamUnits)
        {
            if (u is null) continue;
            if (seen.Add(u)) snapshot.Add(u);
        }

        OriginalOrderByTeam[team] = snapshot;
        return snapshot;
    }
    

    private static void ResetTeamToMax(Team team)
    {
        for (int i = 0; i < team.TeamUnits.Count; i++)
        {
            var unit = team.TeamUnits[i];
            if (unit == null) continue;

            unit.Stats.HealthPoints = unit.Stats.MaximumHealthPoints;
            unit.Stats.ManaPoints   = unit.Stats.MaximumManaPoints;
        }
    }

    private static Team CloneTeam(Team team)
    {
        var clones = new List<Unit>(team.TeamUnits.Count);
        for (int i = 0; i < team.TeamUnits.Count; i++)
        {
            var src = team.TeamUnits[i];
            clones.Add(src is null ? null : UnitFactory.Clone(src));
        }
        return new Team { TeamUnits = clones };
    }

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
}
