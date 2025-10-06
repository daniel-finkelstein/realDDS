using System;
using System.Collections.Generic;
using Shin_Megami_Tensei_Models;
using Shin_Megami_Tensei_View;
using Shin_Megami_Tensei.Utils;
using Shin_Megami_Tensei.Actions;

namespace Shin_Megami_Tensei;

internal static class ViewRenderer
{
    public static void PrintRoundStart(View view, string samuraiName, string tag, Team p1, Team p2)
    {
        view.WriteLine(CombatLogic.TextSeparator);
        view.WriteLine($"Ronda de {samuraiName} ({tag})");
        view.WriteLine(CombatLogic.TextSeparator);
        PrintBoards(view, p1, p2);
    }

    public static void PrintBoards(View view, Team p1, Team p2)
    {
        view.WriteLine($"Equipo de {TeamUtils.GetSamuraiName(p1)} (J1)");
        PrintTeamSlots(view, p1);
        view.WriteLine($"Equipo de {TeamUtils.GetSamuraiName(p2)} (J2)");
        PrintTeamSlots(view, p2);
        view.WriteLine(CombatLogic.TextSeparator);
    }

    public static void PrintTeamSlots(View view, Team team)
    {
        var slots = TeamUtils.GetTeamSlots(team);
        char label = 'A';
        for (int i = 0; i < 4; i++, label++)
        {
            var unit = slots[i];
            if (unit == null) view.WriteLine($"{label}-");
            else view.WriteLine($"{label}-{unit.Name} HP:{unit.Stats.HealthPoints}/{unit.Stats.MaximumHealthPoints} MP:{unit.Stats.ManaPoints}/{unit.Stats.MaximumManaPoints}");
        }
    }

    public static void PrintCounters(View view, int full, int blink)
    {
        view.WriteLine($"Full Turns: {full}");
        view.WriteLine($"Blinking Turns: {blink}");
        view.WriteLine(CombatLogic.TextSeparator);
    }

    public static void PrintOrderFromCursor(View view, List<Unit> order, int cursor)
    {
        view.WriteLine("Orden:");
        int k = 1;
        for (int i = 0; i < order.Count; i++)
        {
            var u = order[(cursor + i) % order.Count];
            if (u.Stats.HealthPoints > 0) view.WriteLine($"{k++}-{u.Name}");
        }
        view.WriteLine(CombatLogic.TextSeparator);
    }

    public static void ShowRoundStatus(View view, List<Unit> order, int cursor, int full, int blink)
    {
        PrintCounters(view, full, blink);
        PrintOrderFromCursor(view, order, cursor);
    }

    public static void ShowInterTurn(View view, Team p1, Team p2, List<Unit> order, int cursor, int full, int blink)
    {
        view.WriteLine(CombatLogic.TextSeparator);
        PrintBoards(view, p1, p2);
        ShowRoundStatus(view, order, cursor, full, blink);
    }
    

    public static void ReportTurnConsumption(
        View view, int fullUsed, int blinkUsed, int blinkGained,
        Actions.ActionHandler.ActionKind kind)
    {
        if (fullUsed == 0 && blinkUsed == 0 && blinkGained == 0) return;

        bool needsSeparator =
            kind != ActionHandler.ActionKind.Pass &&
            kind != ActionHandler.ActionKind.Surrender;

        if (needsSeparator)
            view.WriteLine(CombatLogic.TextSeparator);

        view.WriteLine($"Se han consumido {fullUsed} Full Turn(s) y {blinkUsed} Blinking Turn(s)");
        view.WriteLine($"Se han obtenido {blinkGained} Blinking Turn(s)");
    }


    public static void PrintWinner(View view, Team p1, Team p2, int actingPlayer)
    {
        view.WriteLine(CombatLogic.TextSeparator);
        var (winner, tag) = TeamUtils.ResolveWinnerByBoard(p1, p2, actingPlayer);
        view.WriteLine($"Ganador: {TeamUtils.GetSamuraiName(winner)} ({tag})");
    }
    
    public static void ReportAffinity(View view, Unit attacker, Unit target, Affinity affinity, int amount)
    {
        switch (affinity)
        {
            case Affinity.Weak:   view.WriteLine($"{target.Name} es débil contra el ataque de {attacker.Name}"); break;
            case Affinity.Resist: view.WriteLine($"{target.Name} es resistente el ataque de {attacker.Name}");   break;
            case Affinity.Null:   view.WriteLine($"{target.Name} bloquea el ataque de {attacker.Name}");         break;
            case Affinity.Repel:  view.WriteLine($"{target.Name} devuelve {amount} daño a {attacker.Name}");     break;
            case Affinity.Drain:  view.WriteLine($"{target.Name} absorbe {amount} daño");                         break;
        }
    }

}
