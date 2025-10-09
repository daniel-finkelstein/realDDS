using System;
using Shin_Megami_Tensei_Models;
using Shin_Megami_Tensei_View;
using Shin_Megami_Tensei.Utils;
using static Shin_Megami_Tensei.CombatLogic;

namespace Shin_Megami_Tensei.Actions;

internal sealed class SkillSabbatmaAction : ICombatAction
{
    private readonly int _manaCost;
    public SkillSabbatmaAction(int manaCost) => _manaCost = manaCost;

    public bool IsExecuteActionSuccess(in ActionHandler.ActionContext ctx, out ActionHandler.ActionEffect effect)
    {
        var view = ctx.View;
        var team = ctx.AttackingTeam;
        
        

        var bench = SummonAction.GetAliveBenchOrdered(in ctx);
        view.WriteLine("Seleccione un monstruo para invocar");
        if (bench.Count == 0)
        {
            view.WriteLine("1-Cancelar");
            Menus.ReadIndexAllowCancel(view, 1);
            effect = default;
            return false;
        }

        Menus.PrintBenchOptions(view, bench);
        int pick = Menus.ReadIndexAllowCancel(view, bench.Count + 1);
        if (pick == bench.Count + 1)
        {
            effect = default;
            return false;
        }

        var summoned = bench[pick - 1];

        view.WriteLine(TextSeparator);
        view.WriteLine("Seleccione una posición para invocar");

        var slots = TeamUtils.GetTeamSlots(team);
        for (int i = 0; i < 3; i++)
        {
            int boardPos = i + 2;
            var occ = slots[boardPos - 1];
            if (occ != null)
                view.WriteLine($"{i + 1}-{occ.Name} HP:{occ.Stats.HealthPoints}/{occ.Stats.MaximumHealthPoints} MP:{occ.Stats.ManaPoints}/{occ.Stats.MaximumManaPoints} (Puesto {boardPos})");
            else
                view.WriteLine($"{i + 1}-Vacío (Puesto {boardPos})");
        }
        view.WriteLine("4-Cancelar");

        int posChoice = Menus.ReadIndexAllowCancel(view, 4);
        if (posChoice == 4)
        {
            effect = default;
            return false;
        }

        int boardSlot = posChoice + 1;
        
        var stats = ctx.Actor.Stats;
        if (stats.ManaPoints < _manaCost)
        {
            effect = default;
            return false;
        }
        stats.ManaPoints = Math.Max(0, stats.ManaPoints - _manaCost);

        var previous    = team.TeamUnits[boardSlot - 1];
        int idxSummoned = team.TeamUnits.IndexOf(summoned);

        team.TeamUnits[boardSlot - 1] = summoned;
        if (previous != null) team.TeamUnits[idxSummoned] = previous;
        else team.TeamUnits.RemoveAt(idxSummoned);

        
        var order = ctx.Order;
        var actor = ctx.Actor;

        if (previous != null)
        {
            TeamUtils.ReplaceUnitInOrder(order, previous, summoned);
        }
        else
        {
            int actorIdx = order.FindIndex(u => ReferenceEquals(u, actor));
            if (actorIdx < 0) actorIdx = order.Count;
            

            order.Remove(summoned);
            order.Insert(actorIdx, summoned);
        }

        view.WriteLine(TextSeparator);
        view.WriteLine($"{summoned.Name} ha sido invocado");

        effect = new ActionHandler.ActionEffect(
            0,
            0,
            1,
            ActionHandler.ActionKind.Skill
        );

        return true;
    }
}
