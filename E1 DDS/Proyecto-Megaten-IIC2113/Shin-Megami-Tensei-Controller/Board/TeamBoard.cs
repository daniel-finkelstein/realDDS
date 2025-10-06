using System;
using System.Collections.Generic;
using System.Linq;
using Shin_Megami_Tensei_Models;
using Shin_Megami_Tensei_View;
using Shin_Megami_Tensei.Utils;
using static Shin_Megami_Tensei.CombatLogic;

namespace Shin_Megami_Tensei.Actions;

internal static class TeamBoard
{
    public static bool HandleSummonAsSamurai(in ActionHandler.ActionContext ctx, out ActionHandler.ActionEffect effect)
    {
        var view = ctx.View;
        var team = ctx.AttackingTeam;

        var bench = GetAliveBenchOrdered(team);

        view.WriteLine("Seleccione un monstruo para invocar");
        if (bench.Count == 0)
        {
            view.WriteLine("1-Cancelar");
            Menus.ReadIndexAllowCancel(view, 1); effect = default; return false;
        }
        Menus.PrintBenchOptions(view, bench);
        int pick = Menus.ReadIndexAllowCancel(view, bench.Count + 1);
        if (pick == bench.Count + 1) { effect = default; return false; }

        var summoned = bench[pick - 1];

        view.WriteLine(TextSeparator);
        view.WriteLine("Seleccione una posición para invocar");

        var slots = TeamUtils.GetTeamSlots(team);
        for (int i = 0; i < 3; i++)
        {
            int boardPos = i + 2; // 2..4
            var occ = slots[boardPos - 1];
            if (occ != null)
                view.WriteLine($"{i + 1}-{occ.Name} HP:{occ.Stats.HealthPoints}/{occ.Stats.MaximumHealthPoints} MP:{occ.Stats.ManaPoints}/{occ.Stats.MaximumManaPoints} (Puesto {boardPos})");
            else
                view.WriteLine($"{i + 1}-Vacío (Puesto {boardPos})");
        }
        view.WriteLine("4-Cancelar");

        int posChoice = Menus.ReadIndexAllowCancel(view, 4);
        if (posChoice == 4) { effect = default; return false; }
        int boardSlot = posChoice + 1; // 2..4

        var previous = team.TeamUnits[boardSlot - 1];


        int idxSummoned = team.TeamUnits.IndexOf(summoned);
        team.TeamUnits[boardSlot - 1] = summoned;
        if (previous != null) team.TeamUnits[idxSummoned] = previous;
        else team.TeamUnits.RemoveAt(idxSummoned);


        if (previous != null)
        {

            TeamUtils.ReplaceUnitInOrder(ctx.Order, previous, summoned);
        }
        else
        {
            ctx.Order.Remove(summoned);
            var order    = ctx.Order;
            var summoner = ctx.Actor;

            order.Remove(summoned);
            int summonerIdx = order.FindIndex(u => ReferenceEquals(u, summoner));
            if (summonerIdx < 0) order.Add(summoned);
            else order.Insert(summonerIdx, summoned);
        }

        view.WriteLine(TextSeparator);
        view.WriteLine($"{summoned.Name} ha sido invocado");

        ReorderBenchToOriginal(team);

        effect = new ActionHandler.ActionEffect(0, 1, 1, ActionHandler.ActionKind.Summon);
        return true;
    }

    public static bool HandleSummonAsMonster(in ActionHandler.ActionContext ctx, out ActionHandler.ActionEffect effect)
    {
        var view = ctx.View;
        var team = ctx.AttackingTeam;

        var bench = GetAliveBenchOrdered(team);

        view.WriteLine("Seleccione un monstruo para invocar");
        if (bench.Count == 0)
        {
            view.WriteLine("1-Cancelar");
            Menus.ReadIndexAllowCancel(view, 1); effect = default; return false;
        }
        Menus.PrintBenchOptions(view, bench);
        int cancel = bench.Count + 1;
        int choice = Menus.ReadIndexAllowCancel(view, cancel);
        if (choice == cancel) { effect = default; return false; }

        var summoned    = bench[choice - 1];
        int actorSlot   = TeamUtils.FindActorSlot(team, ctx.Actor);
        int idxSummoned = team.TeamUnits.IndexOf(summoned);

        team.TeamUnits[actorSlot - 1] = summoned;
        team.TeamUnits[idxSummoned]   = ctx.Actor;

        TeamUtils.ReplaceUnitInOrder(ctx.Order, ctx.Actor, summoned);

        view.WriteLine(TextSeparator);
        view.WriteLine($"{summoned.Name} ha sido invocado");

        ReorderBenchToOriginal(team);

        effect = new ActionHandler.ActionEffect(0, 0, 1, ActionHandler.ActionKind.Summon);
        return true;
    }

    // ===== Helpers banca/orden original =====
    private static List<Unit> GetAliveBenchOrdered(Team team)
    {
        ReorderBenchToOriginal(team);
        var list = new List<Unit>();
        for (int i = 4; i < team.TeamUnits.Count; i++)
        {
            var u = team.TeamUnits[i];
            if (u != null && u.Stats.HealthPoints > 0) list.Add(u);
        }
        return list;
    }

    private static readonly Dictionary<Team, Dictionary<Unit, int>> OriginalIndexByTeam = new();

    private static void EnsureOriginalIndexMap(Team team)
    {
        if (OriginalIndexByTeam.ContainsKey(team)) return;
        var map = new Dictionary<Unit, int>();
        for (int i = 0; i < team.TeamUnits.Count; i++)
        {
            var u = team.TeamUnits[i];
            if (u != null) map[u] = i;
        }
        OriginalIndexByTeam[team] = map;
    }

    private static void ReorderBenchToOriginal(Team team)
    {
        EnsureOriginalIndexMap(team);
        var map = OriginalIndexByTeam[team];

        var board = new List<Unit>(4);
        for (int i = 0; i < Math.Min(4, team.TeamUnits.Count); i++)
            board.Add(team.TeamUnits[i]);

        var bench = new List<Unit>();
        for (int i = 4; i < team.TeamUnits.Count; i++)
        {
            var u = team.TeamUnits[i];
            if (u != null && u.Stats.HealthPoints > 0) bench.Add(u);
        }
        bench.Sort((a, b) => map[a].CompareTo(map[b]));

        team.TeamUnits.Clear();
        team.TeamUnits.AddRange(board);
        team.TeamUnits.AddRange(bench);
    }
}
