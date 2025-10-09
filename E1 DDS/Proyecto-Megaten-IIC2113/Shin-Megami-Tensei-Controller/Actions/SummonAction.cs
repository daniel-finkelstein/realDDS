using System;
using System.Collections.Generic;
using Shin_Megami_Tensei_Models;
using Shin_Megami_Tensei_View;
using Shin_Megami_Tensei.Utils;
using static Shin_Megami_Tensei.CombatLogic;

namespace Shin_Megami_Tensei.Actions;

internal sealed class SummonAction : ICombatAction
{
    private readonly bool _actorIsSamurai;
    public SummonAction(bool actorIsSamurai) => _actorIsSamurai = actorIsSamurai;

    public bool IsExecuteActionSuccess(in ActionHandler.ActionContext ctx, out ActionHandler.ActionEffect effect)
    {
        if (_actorIsSamurai)
        {
            var action = new SummonAsSamuraiAction();
            return action.IsExecuteActionSuccess(in ctx, out effect);
        }
        else
        {
            var action = new SummonAsMonsterAction();
            return action.IsExecuteActionSuccess(in ctx, out effect);
        }
    }
    internal static bool TryPickBenchSummon(in ActionHandler.ActionContext ctx, out Unit summoned)
    {
        var view  = ctx.View;
        var bench = GetAliveBenchOrdered(in ctx);

        view.WriteLine("Seleccione un monstruo para invocar");

        if (bench.Count == 0)
        {
            view.WriteLine("1-Cancelar");
            Menus.ReadIndexAllowCancel(view, 1);
            summoned = null!;
            return false;
        }

        Menus.PrintBenchOptions(view, bench);
        int cancel = bench.Count + 1;
        int choice = Menus.ReadIndexAllowCancel(view, cancel);
        if (choice == cancel)
        {
            summoned = null!;
            return false;
        }

        summoned = bench[choice - 1];
        return true;
    }
    
    internal static int ReadSamuraiBoardSlot(View view, Team team)
    {
        view.WriteLine(TextSeparator);
        view.WriteLine("Seleccione una posición para invocar");

        var slots = TeamUtils.GetTeamSlots(team);
        for (int i = 0; i < 3; i++)
        {
            int boardPos = i + 2;
            var occ = slots[boardPos - 1];
            if (occ != null)
                view.WriteLine($"{i + 1}-{occ.Name} HP:{occ.Stats.HealthPoints}/{occ.Stats.MaximumHealthPoints} " +
                               $"MP:{occ.Stats.ManaPoints}/{occ.Stats.MaximumManaPoints} (Puesto {boardPos})");
            else
                view.WriteLine($"{i + 1}-Vacío (Puesto {boardPos})");
        }
        view.WriteLine("4-Cancelar");

        int posChoice = Menus.ReadIndexAllowCancel(view, 4);
        if (posChoice == 4) return -1;

        return posChoice + 1;
    }

    internal static void WriteSummoned(View view, Unit summoned)
    {
        view.WriteLine(TextSeparator);
        view.WriteLine($"{summoned.Name} ha sido invocado");
    }

    internal static void ReorderBenchToOriginal(Team team)
    {
        var original = TeamUtils.GetOriginalOrderSnapshot(team);
        var map = new Dictionary<Unit, int>(ReferenceEqualityComparer.Instance);
        for (int i = 0; i < original.Count; i++)
            if (original[i] != null) map[original[i]!] = i;

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

    internal static List<Unit> GetAliveBenchOrdered(in ActionHandler.ActionContext ctx)
    {
        var team = ctx.AttackingTeam;

        var bench = new List<Unit>();
        for (int i = 4; i < team.TeamUnits.Count; i++)
        {
            var u = team.TeamUnits[i];
            if (u != null && u.Stats.HealthPoints > 0)
                bench.Add(u);
        }

        var index = new Dictionary<Unit, int>(ReferenceEqualityComparer.Instance);
        for (int i = 0; i < ctx.OriginalRoster.Count; i++)
            index[ctx.OriginalRoster[i]] = i;

        bench.Sort((a, b) => index[a].CompareTo(index[b]));
        return bench;
    }
}
