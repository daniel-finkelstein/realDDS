using System;
using System.Collections.Generic;
using Shin_Megami_Tensei_Models;
using Shin_Megami_Tensei_View;
using Shin_Megami_Tensei.Utils;

namespace Shin_Megami_Tensei;

internal static class Menus
{
    public static void ShowActionMenu(View view, Unit actor)
    {
        view.WriteLine($"Seleccione una acción para {actor.Name}");
        if (Shin_Megami_Tensei.Utils.TeamUtils.IsSamurai(actor))
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

    public static void AfterSelectionSeparator(View view) => view.WriteLine(CombatLogic.TextSeparator);

    public static int ReadMenuInput(View view) => ReadBoundedOrDefault(view, 1, 6, 1);
    public static int ReadIndexAllowCancel(View view, int maxInclusive) => ReadBoundedOrDefault(view, 1, maxInclusive, 1);

    private static int ReadBoundedOrDefault(View view, int min, int max, int @default)
    {
        var line = view.ReadLine();
        if (!int.TryParse(line, out var val)) return @default;
        return (val < min || val > max) ? @default : val;
    }

    public static Unit? SelectTarget(View view, string attackerName, Team defendingTeam)
    {
        var opts = Shin_Megami_Tensei.Utils.TeamUtils.BuildTargetOptions(defendingTeam);
        if (opts.Count == 0) return null;

        view.WriteLine($"Seleccione un objetivo para {attackerName}");
        for (int i = 0; i < opts.Count; i++)
        {
            var u = opts[i];
            view.WriteLine($"{i + 1}-{u.Name} HP:{u.Stats.HealthPoints}/{u.Stats.MaximumHealthPoints} MP:{u.Stats.ManaPoints}/{u.Stats.MaximumManaPoints}");
        }
        view.WriteLine($"{opts.Count + 1}-Cancelar");

        int cancel = opts.Count + 1;
        int choice = ReadIndexAllowCancel(view, cancel);
        if (choice == cancel) return null;

        return opts[choice - 1];
    }

    public static void PrintBenchOptions(View view, List<Unit> bench)
    {
        for (int i = 0; i < bench.Count; i++)
        {
            var u = bench[i];
            view.WriteLine($"{i + 1}-{u.Name} HP:{u.Stats.HealthPoints}/{u.Stats.MaximumHealthPoints} MP:{u.Stats.ManaPoints}/{u.Stats.MaximumManaPoints}");
        }
        view.WriteLine($"{bench.Count + 1}-Cancelar");
    }

    public static Skill? SelectSkill(View view, Unit actor, IList<Skill> usable)
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

        int pick = ReadIndexAllowCancel(view, usable.Count + 1);
        if (pick == usable.Count + 1) return null;
        return usable[pick - 1];
    }
    
    
    public static Unit? SelectAllyTarget(View view, string actorName, Team team)
    {
        view.WriteLine("Seleccione un objetivo para " + actorName);
        var slots = TeamUtils.GetTeamSlots(team);

        var opciones = new List<Unit?>();
        int k = 1;
        for (int i = 0; i < 4; i++)
        {
            var u = slots[i];
            if (u != null)
            {
                view.WriteLine($"{k}-{u.Name} HP:{u.Stats.HealthPoints}/{u.Stats.MaximumHealthPoints} MP:{u.Stats.ManaPoints}/{u.Stats.MaximumManaPoints}");
                opciones.Add(u);
                k++;
            }
        }
        view.WriteLine($"{k}-Cancelar");

        int idx = ReadIndexAllowCancel(view, k);
        if (idx == k) return null;
        return opciones[idx - 1];
    }


}
