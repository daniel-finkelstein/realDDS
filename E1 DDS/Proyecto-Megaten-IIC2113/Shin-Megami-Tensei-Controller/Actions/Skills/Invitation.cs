using System;
using System.Collections.Generic;
using System.Linq;
using Shin_Megami_Tensei_Models;
using Shin_Megami_Tensei_View;
using Shin_Megami_Tensei.Utils;
using static Shin_Megami_Tensei.CombatLogic;

namespace Shin_Megami_Tensei.Actions;

internal sealed class SkillInvitationAction : ICombatAction
{
    private readonly int _manaPointsCost;
    public SkillInvitationAction(int manaPointsCost) => _manaPointsCost = manaPointsCost;

    public bool IsExecuteActionSuccess(in ActionHandler.ActionContext ctx, out ActionHandler.ActionEffect effect)
    {
        effect = default;

        if (!IsSelectCandidate(ctx, out var summoned)) return false;
        if (!IsSelectBoardSlot(ctx, out var boardSlot)) return false;
        if (!IsPayManaCost(ctx.Actor.Stats, _manaPointsCost))  return false;

        var previous = PlaceOnTeamUnits(ctx.AttackingTeam, summoned, boardSlot);

        UpdateTurnOrder(ctx.Order, ctx.Actor, summoned, previous);

        var wasDead = summoned.Stats.HealthPoints <= 0;
        HandleReviveAndAnnounce(ctx.View, summoned, ctx.Actor, wasDead);

        effect = BuildActionEffect(_manaPointsCost);
        return true;
    }
    
    private static bool IsSelectCandidate(in ActionHandler.ActionContext ctx, out Unit summoned)
    {
        summoned = null!;

        var candidates = BuildCandidates(ctx);

        PrintCandidateMenu(ctx.View, candidates);

        if (!IsReadMenuSelection(ctx.View, candidates.Count + 1, out var pick)) return false;
        if (pick == candidates.Count + 1) return false;

        return IsMapSelectionToCandidate(candidates, pick, out summoned);
    }

    private static List<Unit> BuildCandidates(in ActionHandler.ActionContext ctx)
    {
        var onFieldAlive = new HashSet<Unit>(ctx.Order);
        return ctx.OriginalRoster
            .Where(u => !TeamUtils.IsSamurai(u) && !onFieldAlive.Contains(u))
            .ToList();
    }

    private static void PrintCandidateMenu(View view, IReadOnlyList<Unit> candidates)
    {
        view.WriteLine("Seleccione un monstruo para invocar");
        for (int i = 0; i < candidates.Count; i++)
        {
            var u = candidates[i];
            view.WriteLine($"{i + 1}-{u.Name} HP:{u.Stats.HealthPoints}/{u.Stats.MaximumHealthPoints} " +
                           $"MP:{u.Stats.ManaPoints}/{u.Stats.MaximumManaPoints}");
        }
        view.WriteLine($"{candidates.Count + 1}-Cancelar");
    }

    private static bool IsReadMenuSelection(View view, int maxInclusive, out int pick)
    {
        var line = view.ReadLine();
        if (!int.TryParse(line, out pick)) return false;
        if (pick < 1 || pick > maxInclusive) return false;
        return true;
    }

    private static bool IsMapSelectionToCandidate(IReadOnlyList<Unit> candidates, int pick, out Unit selected)
    {
        selected = null!;
        if (pick < 1 || pick > candidates.Count) return false;
        selected = candidates[pick - 1];
        return true;
    }

    
    private static bool IsSelectBoardSlot(in ActionHandler.ActionContext ctx, out int boardSlot)
    {
        boardSlot = -1;

        WriteBoardSelectionHeader(ctx.View);

        var slots = TeamUtils.GetTeamSlots(ctx.AttackingTeam);
        PrintBoardSlots(ctx.View, slots);

        if (!IsReadBoardSlotChoice(ctx.View, out var choice)) return false;
        if (IsCancelChoice(choice)) return false;

        boardSlot = MapChoiceToBoardSlot(choice);
        return true;
    }

    private static void WriteBoardSelectionHeader(View view)
    {
        view.WriteLine(TextSeparator);
        view.WriteLine("Seleccione una posición para invocar");
    }

    private static void PrintBoardSlots(View view, IReadOnlyList<Unit?> slots)
    {
        for (int i = 0; i < 3; i++)
        {
            int boardPos = i + 2;
            var occ = slots[boardPos - 1];
            WriteSlotLine(view, i, boardPos, occ);
        }
        view.WriteLine("4-Cancelar");
    }

    private static void WriteSlotLine(View view, int menuIndex, int boardPos, Unit? occupant)
    {
        int optionNumber = menuIndex + 1;
        if (occupant != null)
        {
            view.WriteLine($"{optionNumber}-{occupant.Name} " +
                           $"HP:{occupant.Stats.HealthPoints}/{occupant.Stats.MaximumHealthPoints} " +
                           $"MP:{occupant.Stats.ManaPoints}/{occupant.Stats.MaximumManaPoints} " +
                           $"(Puesto {boardPos})");
        }
        else
        {
            view.WriteLine($"{optionNumber}-Vacío (Puesto {boardPos})");
        }
    }

    private static bool IsReadBoardSlotChoice(View view, out int choice)
    {
        choice = Menus.ReadIndexAllowCancel(view, 4);
        return true;
    }

    private static bool IsCancelChoice(int choice) => choice == 4;

    private static int MapChoiceToBoardSlot(int choice) => choice + 1;

    
    private static bool IsPayManaCost(Stats stats, int mpCost)
    {
        if (stats.ManaPoints < mpCost) return false;
        stats.ManaPoints = Math.Max(0, stats.ManaPoints - mpCost);
        return true;
    }
    
    private static Unit? PlaceOnTeamUnits(Team team, Unit summoned, int boardSlot)
    {
        var previous    = team.TeamUnits[boardSlot - 1];
        int summonedIdx = team.TeamUnits.IndexOf(summoned);

        EnsureFourFieldSlots(team);
        
        team.TeamUnits[boardSlot - 1] = summoned;

        if (previous != null)
        {
            HandleDisplacedOccupant(team, previous, summonedIdx);
        }
        else
        {
            if (summonedIdx >= 0)
                team.TeamUnits.RemoveAt(summonedIdx);
        }

        return previous;
    }

    private static void EnsureFourFieldSlots(Team team)
    {
        while (team.TeamUnits.Count < 4)
            team.TeamUnits.Add(null);
    }

    private static void HandleDisplacedOccupant(Team team, Unit previous, int summonedIdx)
    {
        if (summonedIdx >= 0)
        {
            team.TeamUnits[summonedIdx] = previous;
        }
        else
        {
            PlacePreviousOnBench(team, previous);
        }
    }

    private static void PlacePreviousOnBench(Team team, Unit previous)
    {
        int benchHole = team.TeamUnits.FindIndex(4, u => u == null);
        if (benchHole >= 0) team.TeamUnits[benchHole] = previous;
        else team.TeamUnits.Add(previous);
    }

    
    private static void UpdateTurnOrder(List<Unit> order, Unit actor, Unit summoned, Unit? previous)
    {
        if (previous != null)
        {
            TeamUtils.ReplaceUnitInOrder(order, previous, summoned);
        }
        else
        {
            order.Remove(summoned);
            int actorIdx = order.FindIndex(u => ReferenceEquals(u, actor));
            if (actorIdx < 0) actorIdx = order.Count;
            order.Insert(actorIdx, summoned);
        }
    }
    
    private static void HandleReviveAndAnnounce(View view, Unit summoned, Unit actor, bool wasDead)
    {
        view.WriteLine(TextSeparator);
        view.WriteLine($"{summoned.Name} ha sido invocado");
        if (wasDead)
        {
            view.WriteLine($"{actor.Name} revive a {summoned.Name}");
            view.WriteLine($"{summoned.Name} recibe {summoned.Stats.MaximumHealthPoints} de HP");
            summoned.Stats.HealthPoints = summoned.Stats.MaximumHealthPoints;
            view.WriteLine($"{summoned.Name} termina con HP:{summoned.Stats.HealthPoints}/{summoned.Stats.MaximumHealthPoints}");
        }
    }
    
    private static ActionHandler.ActionEffect BuildActionEffect(int manaPointsCost)
    {
        bool isMenuSummon = manaPointsCost == 0;
        int  blinkGain    = isMenuSummon ? 1 : 0;
        var  actionKind         = isMenuSummon ? ActionHandler.ActionKind.Summon
                                         : ActionHandler.ActionKind.Skill;

        return new ActionHandler.ActionEffect(
            0,
            blinkGain,
            1,
            actionKind
        );
    }
}
