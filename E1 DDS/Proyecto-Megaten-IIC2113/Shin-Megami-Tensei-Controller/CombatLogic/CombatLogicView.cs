using System;
using System.Collections.Generic;
using Shin_Megami_Tensei_Models;
using Shin_Megami_Tensei_View;

namespace Shin_Megami_Tensei;

public static partial class CombatLogic
{
    private static void PrintRoundHeader(View view, string attackingSamuraiName, string attackerTag)
    {
        view.WriteLine(TextSeparator);
        view.WriteLine($"Ronda de {attackingSamuraiName} ({attackerTag})");
        view.WriteLine(TextSeparator);
    }

    private static void PrintBoards(View view, Team player1Team, Team player2Team)
    {
        view.WriteLine($"Equipo de {GetSamuraiName(player1Team)} (J1)");
        PrintTeamSlots(view, player1Team);
        view.WriteLine($"Equipo de {GetSamuraiName(player2Team)} (J2)");
        PrintTeamSlots(view, player2Team);
        view.WriteLine(TextSeparator);
    }

    private static void PrintTeamSlots(View view, Team team)
    {
        var slots = GetTeamSlots(team);
        char label = 'A';
        for (int i = 0; i < 4; i++, label++)
        {
            var unit = slots[i];
            if (unit == null)
            {
                view.WriteLine($"{label}-");
            }
            else
            {
                view.WriteLine($"{label}-{unit.Name} HP:{unit.Stats.HealthPoints}/{unit.Stats.MaximumHealthPoints} " +
                               $"MP:{unit.Stats.ManaPoints}/{unit.Stats.MaximumManaPoints}");
            }
        }
    }

    private static void PrintCounters(View view, int fullTurns, int blinkingTurns)
    {
        view.WriteLine($"Full Turns: {fullTurns}");
        view.WriteLine($"Blinking Turns: {blinkingTurns}");
        view.WriteLine(TextSeparator);
    }

    private static void PrintOrderFromCursor(View view, List<Unit> order, int cursor)
    {
        view.WriteLine("Orden:");
        int k = 1;
        for (int i = 0; i < order.Count; i++)
        {
            var u = order[(cursor + i) % order.Count];
            if (u.Stats.HealthPoints > 0) view.WriteLine($"{k++}-{u.Name}");
        }
        view.WriteLine(TextSeparator);
    }
    

    private static void ReportAttack(ActionContext actionContext, Unit targetUnit, int damage, bool actionIsShoot)
    {
        actionContext.View.WriteLine(TextSeparator);
        actionContext.View.WriteLine(actionIsShoot
            ? $"{actionContext.Actor.Name} dispara a {targetUnit.Name}"
            : $"{actionContext.Actor.Name} ataca a {targetUnit.Name}");
        actionContext.View.WriteLine($"{targetUnit.Name} recibe {damage} de daño");
        actionContext.View.WriteLine($"{targetUnit.Name} termina con HP:{targetUnit.Stats.HealthPoints}/{targetUnit.Stats.MaximumHealthPoints}");
    }
    
    private static void ReportTurnConsumption(View view, int fullUsed, int blinkUsed, int blinkGained, ActionKind kind)
    {
        if (fullUsed == 0 && blinkUsed == 0 && blinkGained == 0)
            return;

        // Separador solo si NO hubo ganancia y la acción NO fue Pass
        bool needsSeparator = (kind != ActionKind.Pass) && (blinkGained == 0);
        if (needsSeparator)
            view.WriteLine(TextSeparator);

        view.WriteLine($"Se han consumido {fullUsed} Full Turn(s) y {blinkUsed} Blinking Turn(s)");
        view.WriteLine($"Se han obtenido {blinkGained} Blinking Turn(s)");
    }








    
    // Separador genérico
    private static void WriteSeparator(View view)
    {
        view.WriteLine(TextSeparator);
    }
    
    private static void ShowActionMenu(View view, Unit actor)
    {
        PrintMenuHeader(view, actor);
        if (IsSamurai(actor)) PrintSamuraiOptions(view);
        else PrintNonSamuraiOptions(view);
    }
    
    private static void AfterSelectionSeparator(View view)
    {
        view.WriteLine(TextSeparator);
    }
    

    private static void PrintMenuHeader(View view, Unit actor)
    {
        view.WriteLine($"Seleccione una acción para {actor.Name}");
    }



    private static void PrintSamuraiOptions(View view)
    {
        view.WriteLine("1: Atacar");
        view.WriteLine("2: Disparar");
        view.WriteLine("3: Usar Habilidad");
        view.WriteLine("4: Invocar");
        view.WriteLine("5: Pasar Turno");
        view.WriteLine("6: Rendirse");
    }

    private static void PrintNonSamuraiOptions(View view)
    {
        view.WriteLine("1: Atacar");
        view.WriteLine("2: Usar Habilidad");
        view.WriteLine("3: Invocar");
        view.WriteLine("4: Pasar Turno");
    }
    
    private static void ShowSkillPromptHeader(View view, Unit actor)
    {
        view.WriteLine($"Seleccione una habilidad para que {actor.Name} use");
    }
    
    private static void ShowSkillOptions(View view, List<Skill> usableSkills)
    {
        for (int i = 0; i < usableSkills.Count; i++)
            view.WriteLine($"{i + 1}-{usableSkills[i].Name} MP:{usableSkills[i].Cost}");
        view.WriteLine($"{usableSkills.Count + 1}-Cancelar");
    }
    
    private static void TargetLine(View view, int intValue, Unit posibleTargetUnit) =>
        view.WriteLine($"{intValue + 1}-{posibleTargetUnit.Name} HP:{posibleTargetUnit.Stats.HealthPoints}/{posibleTargetUnit.Stats.MaximumHealthPoints} MP:{posibleTargetUnit.Stats.ManaPoints}/{posibleTargetUnit.Stats.MaximumManaPoints}");
    
    private static void TargetHeader(View view, string attackingUnitName) => view.WriteLine($"Seleccione un objetivo para {attackingUnitName}");
    
    private static void PrintCancel(View view, int index) => view.WriteLine($"{index}-Cancelar");
    
    private static void ShowUseSkillMessage(View view, Unit attackingUnit, Skill chosenSkill)
    {
        view.WriteLine($"{attackingUnit.Name} usa {chosenSkill.Name}");
    }

}
