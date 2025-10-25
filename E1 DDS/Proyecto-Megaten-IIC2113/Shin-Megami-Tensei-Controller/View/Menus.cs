using System;
using System.Collections.Generic;
using Shin_Megami_Tensei_Models;
using Shin_Megami_Tensei_View;
using Shin_Megami_Tensei.Utils;
using Shin_Megami_Tensei.Combat;

namespace Shin_Megami_Tensei;

public static class Menus
{
    public static void ShowActionMenu(View view, Unit actor)
    {
        view.WriteLine($"Seleccione una acción para {actor.Name}");

        if (TeamUtils.IsSamurai(actor))
        {
            view.WriteLine("1: Atacar");
            view.WriteLine("2: Disparar");
            view.WriteLine("3: Usar Habilidad");
            view.WriteLine("4: Invocar");
            view.WriteLine("5: Pasar Turno");
            view.WriteLine("6: Rendirse");
        }
        else
        {
            view.WriteLine("1: Atacar");
            view.WriteLine("2: Usar Habilidad");
            view.WriteLine("3: Invocar");
            view.WriteLine("4: Pasar Turno");
        }
    }

    public static void AfterSelectionSeparator(View view) =>
        view.WriteLine(CombatLogic.TextSeparator);

    public static int ReadMenuInput(View view) =>
        ReadIntInRangeOrDefault(view, min: 1, max: 6, @default: 1);

    public static int ReadIndexAllowCancel(View view, int maxInclusive) =>
        ReadIntInRangeOrDefault(view, min: 1, max: maxInclusive, @default: 1);
    
    public static Unit? SelectTarget(View view, string attackerName, Team defendingTeam)
    {
        var options = TeamUtils.BuildTargetOptions(defendingTeam);
        if (options.Count == 0) return null;

        view.WriteLine($"Seleccione un objetivo para {attackerName}");
        PrintUnitOptionsWithCancel(view, options);

        int cancelIndex = options.Count + 1;
        int choice = ReadIndexAllowCancel(view, cancelIndex);
        return (choice == cancelIndex) ? null : options[choice - 1];
    }

    public static void PrintBenchOptions(View view, List<Unit> bench) =>
        PrintUnitOptionsWithCancel(view, bench);

// En Menus.cs (misma clase/namespace), reemplaza TODO el método por este:

    public static global::Shin_Megami_Tensei_Models.Skill? SelectSkill(
        View view,
        global::Shin_Megami_Tensei_Models.Unit actor,
        System.Collections.Generic.IList<global::Shin_Megami_Tensei_Models.Skill> usable)
    {
        view.WriteLine("Seleccione una habilidad para que " + actor.Name + " use");

        if (usable == null || usable.Count == 0)
        {
            view.WriteLine("1-Cancelar");
            ReadIndexAllowCancel(view, 1);
            return null;
        }

        for (int i = 0; i < usable.Count; i++)
            view.WriteLine($"{i + 1}-{usable[i].Name} MP:{usable[i].Cost}");
        view.WriteLine($"{usable.Count + 1}-Cancelar");

        int picked = ReadIndexAllowCancel(view, usable.Count + 1);
        return (picked == usable.Count + 1) ? null : usable[picked - 1];
    }


    public static Unit? SelectAllyTarget(View view, string actorName, Team team)
    {
        view.WriteLine("Seleccione un objetivo para " + actorName);

        var boardSlots = TeamUtils.GetTeamSlots(team);
        var options = new List<Unit>();

        for (int i = 0; i < boardSlots.Length; i++)
        {
            var u = boardSlots[i];
            if (u != null && u.Stats.HealthPoints > 0)
                options.Add(u);
        }

        if (options.Count == 0)
        {
            view.WriteLine("1-Cancelar");
            ReadIndexAllowCancel(view, 1);
            return null;
        }

        PrintUnitOptionsWithCancel(view, options);

        int cancelIndex = options.Count + 1;
        int choice = ReadIndexAllowCancel(view, cancelIndex);
        return (choice == cancelIndex) ? null : options[choice - 1];
    }

    public static Unit? SelectDeadAllyTarget(View view, string actorName, Team team)
    {
        view.WriteLine("Seleccione un objetivo para " + actorName);

        var original = TeamUtils.GetOriginalOrderSnapshot(team);
        var options  = new List<Unit>();

        for (int i = 0; i < original.Count; i++)
        {
            var u = original[i];
            if (u != null && u.Stats.HealthPoints <= 0)
                options.Add(u);
        }

        if (options.Count == 0)
        {
            view.WriteLine("1-Cancelar");
            ReadIndexAllowCancel(view, 1);
            return null;
        }

        PrintUnitOptionsWithCancel(view, options);

        int cancelIndex = options.Count + 1;
        int choice = ReadIndexAllowCancel(view, cancelIndex);
        return (choice == cancelIndex) ? null : options[choice - 1];
    }

    private static int ReadIntInRangeOrDefault(View view, int min, int max, int @default)
    {
        var line = view.ReadLine();
        if (!int.TryParse(line, out var value)) return @default;
        return (value < min || value > max) ? @default : value;
    }

    private static void PrintUnitOptionsWithCancel(View view, IList<Unit> units)
    {
        for (int i = 0; i < units.Count; i++)
        {
            var u = units[i];
            view.WriteLine($"{i + 1}-{u.Name} " +
                           $"HP:{u.Stats.HealthPoints}/{u.Stats.MaximumHealthPoints} " +
                           $"MP:{u.Stats.ManaPoints}/{u.Stats.MaximumManaPoints}");
        }
        view.WriteLine($"{units.Count + 1}-Cancelar");
    }
}
