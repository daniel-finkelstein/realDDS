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

    var bench = GetAliveBenchOrdered(in ctx);

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

    var previous = team.TeamUnits[boardSlot - 1];
    int idxSummoned = team.TeamUnits.IndexOf(summoned);
    
    team.TeamUnits[boardSlot - 1] = summoned;

    if (previous != null)
    {
        if (idxSummoned >= 0) team.TeamUnits[idxSummoned] = previous;
    }
    else
    {
        if (idxSummoned >= 0) team.TeamUnits[idxSummoned] = null;
    }
    
    if (previous != null)
    {
        TeamUtils.ReplaceUnitInOrder(ctx.Order, previous, summoned);
    }
    else
    {
        ctx.Order.Remove(summoned);
        var order = ctx.Order;
        var summoner = ctx.Actor;

        int summonerIdx = order.FindIndex(u => ReferenceEquals(u, summoner));
        if (summonerIdx < 0) order.Add(summoned);
        else order.Insert(summonerIdx, summoned);
    }

    view.WriteLine(TextSeparator);
    view.WriteLine($"{summoned.Name} ha sido invocado");

    ReorderBenchToOriginal(team);

    effect = new ActionHandler.ActionEffect(0,1,1,ActionHandler.ActionKind.Summon);
    return true;
}


    public static bool HandleSummonAsMonster(in ActionHandler.ActionContext ctx, out ActionHandler.ActionEffect effect)
    {
        var view = ctx.View;
        var team = ctx.AttackingTeam;

        var bench = GetAliveBenchOrdered(in ctx);

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

        effect = new ActionHandler.ActionEffect(0, 1, 1, ActionHandler.ActionKind.Summon);
        return true;
    }
    

// TeamBoard.cs
    public static List<Unit> GetAliveBenchOrdered(in ActionHandler.ActionContext ctx)
    {
        var team = ctx.AttackingTeam;

        // 1) Tomar banca viva SIN mutar team.TeamUnits
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




    private static readonly Dictionary<Team, Dictionary<Unit, int>> OriginalIndexByTeam = new();

    private static void EnsureOriginalIndexMap(Team team)
    {
        if (OriginalIndexByTeam.ContainsKey(team)) return;

        // Usa el orden original del roster (el snapshot que nunca cambia).
        // Si no tienes TeamUtils.GetOriginalOrderSnapshot(team), reemplázalo por la fuente
        // equivalente que tengan ustedes (ctx.OriginalRoster, etc.).
        var original = TeamUtils.GetOriginalOrderSnapshot(team);

        var map = new Dictionary<Unit, int>(original.Count);
        for (int i = 0; i < original.Count; i++)
        {
            var u = original[i];
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

public static bool HandleSabbatma(
    in ActionHandler.ActionContext ctx,
    int manaCost,
    out ActionHandler.ActionEffect effect)
{
    var view = ctx.View;
    var team = ctx.AttackingTeam;
    
    var bench = GetAliveBenchOrdered(in ctx);
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
        int boardPos = i + 2; // 2..4
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

    int boardSlot = posChoice + 1; // 2..4
    
    var stats = ctx.Actor.Stats;
    if (stats.ManaPoints < manaCost)
    {
        effect = default;
        return false;
    }
    stats.ManaPoints = Math.Max(0, stats.ManaPoints - manaCost);
    
    var previous     = team.TeamUnits[boardSlot - 1];
    int idxSummoned  = team.TeamUnits.IndexOf(summoned);

    team.TeamUnits[boardSlot - 1] = summoned;
    if (previous != null) team.TeamUnits[idxSummoned] = previous;
    else team.TeamUnits.RemoveAt(idxSummoned);

    // --- ORDEN ---
    var order  = ctx.Order;
    var actor  = ctx.Actor; // <- copiar fuera del parámetro `in` para evitar capturarlo

    if (previous != null)
    {
        // Reemplaza a alguien: hereda su lugar
        TeamUtils.ReplaceUnitInOrder(order, previous, summoned);
    }
    else
    {
        // Puesto vacío: el input define la posición relativa
        // Insertamos PRE-rotación en: (índiceActor + 1) + posChoice
        int actorIdx = order.FindIndex(u => ReferenceEquals(u, actor)); // ok, no captura `ctx`
        if (actorIdx < 0) actorIdx = order.Count;

        int preRotationInsertIndex = Math.Min(order.Count, actorIdx + 1 + posChoice);

        order.Remove(summoned);            // por si estaba
        order.Insert(actorIdx, summoned);
    }

    view.WriteLine(TextSeparator);
    view.WriteLine($"{summoned.Name} ha sido invocado");

    effect = new ActionHandler.ActionEffect(
        0, // consume 1 acción (el motor decide FT/BT)
        0,
        1,
        ActionHandler.ActionKind.Skill
    );

    return true;
}


public static bool PlaceSummonedChoosingAnySlot(
    in ActionHandler.ActionContext ctx,
    Unit summoned,
    int manaCost,
    out ActionHandler.ActionEffect effect)
{
    var view = ctx.View;
    var team = ctx.AttackingTeam;

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
    if (posChoice == 4)
    {
        effect = default;
        return false;
    }

    int boardSlot = posChoice + 1; // 2..4

    // MP check + descuento
    var stats = ctx.Actor.Stats;
    if (stats.ManaPoints < manaCost)
    {
        effect = default;
        return false;
    }
    stats.ManaPoints = Math.Max(0, stats.ManaPoints - manaCost);


    var previous    = team.TeamUnits[boardSlot - 1];
    int idxSummoned = team.TeamUnits.IndexOf(summoned);

// 1) Garantizar 4 slots de campo (A..D) siempre presentes
    while (team.TeamUnits.Count < 4)
        team.TeamUnits.Add(null);

// Colocar el invocado en el slot elegido
    team.TeamUnits[boardSlot - 1] = summoned;

    if (previous != null)
    {
        if (idxSummoned >= 0)
        {
            // El invocado ya estaba en TeamUnits -> swap normal
            team.TeamUnits[idxSummoned] = previous;
        }
        else
        {
            // El invocado NO estaba en TeamUnits (viene de roster) -> previous va a banca
            // Buscar primer hueco null en banca (índices >= 4)
            int benchHole = team.TeamUnits.FindIndex(4, u => u == null);
            if (benchHole >= 0)
                team.TeamUnits[benchHole] = previous;
            else
                team.TeamUnits.Add(previous); // si no hay hueco, agregar al final
        }
    }
    else
    {
        // Slot elegido estaba vacío: si el invocado estaba en TeamUnits (venía de la banca),
        // hay que quitarlo de donde estaba para no duplicarlo
        if (idxSummoned >= 0)
            team.TeamUnits.RemoveAt(idxSummoned);
    }


    // --- ORDEN ---
    var order = ctx.Order;
    var actor = ctx.Actor;

    if (previous != null)
    {
        // Como el desplazado se va a banca en este camino, reemplaza en el orden
        TeamUtils.ReplaceUnitInOrder(order, previous, summoned);
    }
    else
    {
        // Slot vacío: insertar cerca del actor
        order.Remove(summoned);
        int actorIdx = order.FindIndex(u => ReferenceEquals(u, actor));
        if (actorIdx < 0) actorIdx = order.Count;
        order.Insert(actorIdx, summoned);
    }

    bool wasDead = summoned.Stats.HealthPoints <= 0;

    view.WriteLine(TextSeparator);
    view.WriteLine($"{summoned.Name} ha sido invocado");
    if (wasDead)
    {
        view.WriteLine($"{actor.Name} revive a {summoned.Name}");
        view.WriteLine($"{summoned.Name} recibe {summoned.Stats.MaximumHealthPoints} de HP");
        summoned.Stats.HealthPoints = summoned.Stats.MaximumHealthPoints;
        view.WriteLine($"{summoned.Name} termina con HP:{summoned.Stats.HealthPoints}/{summoned.Stats.MaximumHealthPoints}");

        // tu línea de debug, intacta
        if (string.Equals(actor.Name, "Mother Harlot", StringComparison.OrdinalIgnoreCase))
        {
            //view.WriteLine($"(1):{team.TeamUnits[0]} (2):{team.TeamUnits[1]} (3):{team.TeamUnits[2]} (4):{team.TeamUnits[3]}");
        }
    }

    bool isMenuSummon = manaCost == 0;
    int  blinkGain    = isMenuSummon ? 1 : 0;
    var  kind         = isMenuSummon ? ActionHandler.ActionKind.Summon
                                     : ActionHandler.ActionKind.Skill;

    effect = new ActionHandler.ActionEffect(
        0,
        blinkGain,
        1,
        kind
    );
    return true;
}



}
