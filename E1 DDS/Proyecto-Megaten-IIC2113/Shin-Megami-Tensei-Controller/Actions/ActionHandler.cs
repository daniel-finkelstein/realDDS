using System;
using System.Collections.Generic;
using Shin_Megami_Tensei_Models;
using Shin_Megami_Tensei_View;
using Shin_Megami_Tensei.Combat;
using Shin_Megami_Tensei.Utils;

namespace Shin_Megami_Tensei.Actions;

internal static class ActionHandler
{
    internal readonly record struct ActionContext(
        View View,
        Unit Actor,
        string AttackingSamuraiName,
        string AttackerTag,
        Team AttackingTeam,
        Team DefendingTeam,
        List<Unit> Order,
        List<Unit> InitialOrder,
        List <Unit> OriginalRoster);

    internal enum ActionKind { Pass, Attack, Skill, Summon, Surrender }

    internal readonly record struct ActionEffect(int FullTurnsLost, int BlinkTurnsGained, int BlinkTurnsLost, ActionKind Kind);

    public static ActionEffect RunActionSelectionLoop(in ActionContext ctx)
    {
        for (int i = 0; ; i++)
        {
            if (i > 0) ctx.View.WriteLine(CombatLogic.TextSeparator);

            Menus.ShowActionMenu(ctx.View, ctx.Actor);

            int selectedAction = Menus.ReadMenuInput(ctx.View);
            Menus.AfterSelectionSeparator(ctx.View);

            if (TryHandleSelection(in ctx, selectedAction, out var effect))
                return effect;
        }
    }

    private static bool TryHandleSelection(in ActionContext ctx, int input, out ActionEffect effect)
    {
        if (TeamUtils.IsSamurai(ctx.Actor))
            return HandleSamurai(in ctx, input, out effect);
        return HandleMonster(in ctx, input, out effect);
    }

    private static bool HandleSamurai(in ActionContext ctx, int input, out ActionEffect effect)
    {
        var sel = MenuMap.MapForSamurai(input);
        switch (sel)
        {
            case ActionSelection.Surrender: return HandleSurrender(ctx, out effect);
            case ActionSelection.Pass:      return HandlePass(ctx.View, out effect);
            case ActionSelection.UseSkill:  return HandleUseSkill(ctx, out effect);
            case ActionSelection.Summon:    return HandleSummonSamurai(ctx, out effect);
            case ActionSelection.Shoot:     return ExecuteAttack(ctx, AttackKind.Ranged, out effect);
            case ActionSelection.Attack:
            default:                        return ExecuteAttack(ctx, AttackKind.Melee,  out effect);
        }
    }

    private static bool HandleMonster(in ActionContext ctx, int input, out ActionEffect effect)
    {
        var sel = MenuMap.MapForMonster(input);
        switch (sel)
        {
            case ActionSelection.Pass:      return HandlePass(ctx.View, out effect);
            case ActionSelection.UseSkill:  return HandleUseSkill(ctx, out effect);
            case ActionSelection.Summon:    return HandleSummonMonster(ctx, out effect);
            case ActionSelection.Shoot:     return ExecuteAttack(ctx, AttackKind.Ranged, out effect);
            case ActionSelection.Attack:
            default:                        return ExecuteAttack(ctx, AttackKind.Melee,  out effect);
        }
    }

    private static bool HandleSurrender(in ActionContext ctx, out ActionEffect effect)
    {
        ctx.View.WriteLine($"{ctx.AttackingSamuraiName} ({ctx.AttackerTag}) se rinde");
        TeamUtils.DefeatTeam(ctx.AttackingTeam);
        effect = new ActionEffect(0, 0, 0, ActionKind.Surrender);
        return true;
    }

    private static bool HandlePass(View view, out ActionEffect effect)
    {
        effect = new ActionEffect(0, 1, 1, ActionKind.Pass);
        return true;
    }

    private static bool HandleUseSkill(in ActionContext ctx, out ActionEffect effect)
    {
        var usable = TeamUtils.GetUsableSkills(ctx.Actor);
        var chosen = Menus.SelectSkill(ctx.View, ctx.Actor, usable);
        if (chosen is null) { effect = default; return false; }

        // ---- skills especiales de invocación (sin descontar MP aquí) ----
        if (string.Equals(chosen.Name, "Sabbatma", StringComparison.OrdinalIgnoreCase))
        {
            ctx.View.WriteLine(CombatLogic.TextSeparator);
            return TeamBoard.HandleSabbatma(in ctx, chosen.Cost, out effect);
        }
        if (string.Equals(chosen.Name, "Invitation", StringComparison.OrdinalIgnoreCase))
        {
            ctx.View.WriteLine(CombatLogic.TextSeparator);
            return HandleInvitation(in ctx, chosen.Cost, out effect);
        }
        
        if (IsHeal(chosen))
            return ExecuteSkillHealSingle(in ctx, chosen, out effect);

        return ExecuteSkillAttackSingle(in ctx, chosen, out effect);
    }



    
private static readonly Dictionary<object, int> _skillKByTeam = new();

private static int GetTeamSkillK(object teamKey)
{
    _skillKByTeam.TryGetValue(teamKey, out int k);
    return k;
}
private static void BumpTeamSkillK(object teamKey)
{
    _skillKByTeam[teamKey] = GetTeamSkillK(teamKey) + 1;
}

private static (int A, int B)? GetHitsRangeForSkill(Skill skill)
{
    string name = (skill?.Name ?? "").Trim().ToLowerInvariant();
    
    if (name.EndsWith(" claw"))
        return (1, 3);

    return null;
}



private static int GetSkillHitCount(in ActionContext ctx, Skill skill)
{
    var range = GetHitsRangeForSkill(skill);
    if (range is null) return 1;

    var (A, B) = range.Value;
    int len = B - A + 1;
    if (len <= 0) return Math.Max(1, A); // seguridad

    int k = GetTeamSkillK(ctx.AttackingTeam);
    int offset = ((k % len) + len) % len; // 0..len-1
    return A + offset;
}


private static bool ExecuteSkillAttackSingle(in ActionContext ctx, Skill skill, out ActionEffect effect)
{

    ctx.View.WriteLine(CombatLogic.TextSeparator);

    var target = Menus.SelectTarget(ctx.View, ctx.Actor.Name, ctx.DefendingTeam);
    if (target is null) { effect = default; return false; }

    int hits = GetSkillHitCount(ctx, skill);
    if (hits > 1)
        return ExecuteSkillAttackMulti(ctx, skill, target, hits, out effect);


    ctx.View.WriteLine(CombatLogic.TextSeparator);

    var element = MapSkillTypeToElement(skill.Type);
    string verb = SkillRuntime.DescribeElementCast(element);
    ctx.View.WriteLine($"{ctx.Actor.Name} {verb} a {target.Name}");

    // MP
    var stats = ctx.Actor.Stats;
    if (stats.ManaPoints < skill.Cost) { effect = default; return false; }
    stats.ManaPoints = Math.Max(0, stats.ManaPoints - skill.Cost);

    // Base daño: √(stat * power)
    int stat = GetSkillOffensiveStat(ctx.Actor, skill.Type);
    decimal baseSkill = (decimal)Math.Sqrt((double)((decimal)stat * skill.Power));
    if (baseSkill < 0) baseSkill = 0;

    // Afinidad (Almighty => Neutral, resto resolver)
    var resolver = new DefaultAffinityResolver();
    var affinity = (element == Element.Almighty) ? Affinity.Neutral
                                                 : resolver.Resolve(ctx.Actor, target, element);

    var affSvc = new AffinityService();
    var outcome = affSvc.Apply(affinity, baseSkill);

    // Texto de afinidad
    int amountForText = affinity switch
    {
        Affinity.Repel => outcome.DamageToAttacker,
        Affinity.Drain => outcome.HealOnDefender,
        _              => 0
    };
    ViewRenderer.ReportAffinity(ctx.View, ctx.Actor, target, affinity, amountForText);

    // Efectos numéricos
    if (outcome.DamageToDefender > 0)
    {
        DamageSystem.ApplyDamage(ctx.DefendingTeam, target, outcome.DamageToDefender);
        ctx.View.WriteLine($"{target.Name} recibe {outcome.DamageToDefender} de daño");
        ctx.View.WriteLine($"{target.Name} termina con HP:{target.Stats.HealthPoints}/{target.Stats.MaximumHealthPoints}");
    }
    else if (outcome.DamageToAttacker > 0)
    {
        DamageSystem.ApplyDamage(ctx.AttackingTeam, ctx.Actor, outcome.DamageToAttacker);
        ctx.View.WriteLine($"{ctx.Actor.Name} termina con HP:{ctx.Actor.Stats.HealthPoints}/{ctx.Actor.Stats.MaximumHealthPoints}");
    }
    else if (outcome.HealOnDefender > 0)
    {
        DamageSystem.Heal(target, outcome.HealOnDefender);
        ctx.View.WriteLine($"{target.Name} termina con HP:{target.Stats.HealthPoints}/{target.Stats.MaximumHealthPoints}");
    }
    else
    {
        ctx.View.WriteLine($"{target.Name} termina con HP:{target.Stats.HealthPoints}/{target.Stats.MaximumHealthPoints}");
    }

    effect = BuildEffectForAffinity(affinity, ActionKind.Skill);

    // ¡K se incrementa DESPUÉS de aplicar la habilidad!
    BumpTeamSkillK(ctx.AttackingTeam);
    return true;
}


private static bool ExecuteSkillAttackMulti(in ActionContext ctx, Skill skill, Unit target, int hits, out ActionEffect effect)
{
    // Separador DESPUÉS de elegir objetivo
    ctx.View.WriteLine(CombatLogic.TextSeparator);

    var element = MapSkillTypeToElement(skill.Type);
    string verb  = SkillRuntime.DescribeElementCast(element);
    string ActionLine(Unit a, Unit t) => $"{a.Name} {verb} a {t.Name}";

    // Pagar MP una sola vez
    var stats = ctx.Actor.Stats;
    if (stats.ManaPoints < skill.Cost) { effect = default; return false; }
    stats.ManaPoints = Math.Max(0, stats.ManaPoints - skill.Cost);

    // Base por hit
    int stat = GetSkillOffensiveStat(ctx.Actor, skill.Type);
    decimal basePerHit = (decimal)Math.Sqrt((double)((decimal)stat * skill.Power));
    if (basePerHit < 0) basePerHit = 0;

    // Afinidad (constante para los golpes; Almighty => Neutral)
    var resolver = new DefaultAffinityResolver();
    var affinity = (element == Element.Almighty) ? Affinity.Neutral
                                                 : resolver.Resolve(ctx.Actor, target, element);

    var affSvc = new AffinityService();
    AffinityService.Outcome last = default;

    for (int i = 0; i < hits; i++)
    {
        ctx.View.WriteLine(ActionLine(ctx.Actor, target));

        last = affSvc.Apply(affinity, basePerHit);

        int amountForText = affinity switch
        {
            Affinity.Repel => last.DamageToAttacker,
            Affinity.Drain => last.HealOnDefender,
            _              => 0
        };
        ViewRenderer.ReportAffinity(ctx.View, ctx.Actor, target, affinity, amountForText);

        if (last.DamageToDefender > 0)
        {
            DamageSystem.ApplyDamage(ctx.DefendingTeam, target, last.DamageToDefender);
            ctx.View.WriteLine($"{target.Name} recibe {last.DamageToDefender} de daño");
        }
        else if (last.DamageToAttacker > 0)
        {
            DamageSystem.ApplyDamage(ctx.AttackingTeam, ctx.Actor, last.DamageToAttacker);
        }
        else if (last.HealOnDefender > 0)
        {
            DamageSystem.Heal(target, last.HealOnDefender);
        }
    }

    // Una sola línea de HP final:
    if (last.DamageToAttacker > 0)
        ctx.View.WriteLine($"{ctx.Actor.Name} termina con HP:{ctx.Actor.Stats.HealthPoints}/{ctx.Actor.Stats.MaximumHealthPoints}");
    else
        ctx.View.WriteLine($"{target.Name} termina con HP:{target.Stats.HealthPoints}/{target.Stats.MaximumHealthPoints}");

    effect = BuildEffectForAffinity(affinity, ActionKind.Skill);

    // Incrementa K del equipo atacante después de aplicar la habilidad
    BumpTeamSkillK(ctx.AttackingTeam);
    return true;
}

private static Element MapSkillTypeToElement(string type)
{
    return type.ToLowerInvariant() switch
    {
        "phys"     => Element.Phys,
        "gun"      => Element.Gun,
        "fire"     => Element.Fire,
        "ice"      => Element.Ice,
        "elec"     => Element.Elec,
        "force"    => Element.Force,
        "almighty" => Element.Almighty,
        _          => Element.Neutral
    };
}


private static int GetSkillOffensiveStat(Unit attacker, string skillType)
{
    var s = attacker.Stats;
    return skillType.Equals("Phys", StringComparison.OrdinalIgnoreCase) ? s.PhysicalAttackPower :
           skillType.Equals("Gun",  StringComparison.OrdinalIgnoreCase) ? s.ShootingPower :
                          s.MagicalAttackPower;
}



    
private static bool ExecuteSkillHealSingle(in ActionContext ctx, Skill skill, out ActionEffect effect)
{
    ctx.View.WriteLine(CombatLogic.TextSeparator);

    static bool IsReviveSkill(string name) =>
        name.Equals("Recarm", StringComparison.OrdinalIgnoreCase) ||
        name.Equals("Samarecarm", StringComparison.OrdinalIgnoreCase) ||
        name.Equals("Invitation", StringComparison.OrdinalIgnoreCase);

    bool revive = IsReviveSkill(skill.Name);

    var target = revive
        ? Menus.SelectDeadAllyTarget(ctx.View, ctx.Actor.Name, ctx.AttackingTeam)
        : Menus.SelectAllyTarget(ctx.View, ctx.Actor.Name, ctx.AttackingTeam);

    if (target is null)
    {
        effect = default;
        return false;
    }

    // Segundo separador, como en los ataques
    ctx.View.WriteLine(CombatLogic.TextSeparator);

    // Cobro de MP
    var stats = ctx.Actor.Stats;
    if (stats.ManaPoints < skill.Cost)
    {
        effect = default;
        return false;
    }
    stats.ManaPoints = Math.Max(0, stats.ManaPoints - skill.Cost);

    // Mensaje
    if (revive && target.Stats.HealthPoints <= 0)
        ctx.View.WriteLine($"{ctx.Actor.Name} revive a {target.Name}");
    else
        ctx.View.WriteLine($"{ctx.Actor.Name} cura a {target.Name}");

    // Curación: % del HP Máximo según skill.Power (25 → 25%, 50 → 50%, 100 → 100%)
    int maxHp = target.Stats.MaximumHealthPoints;
    int amount = (int)((decimal)maxHp * skill.Power / 100m);

    // Aplica la curación / revive
    DamageSystem.Heal(target, amount);

    // --- FIX: si fue revive, asegurarse de que la unidad revivida vuelve al roster/bench ---
    if (revive && target.Stats.HealthPoints > 0)
    {
        EnsureRevivedGoesToBench(ctx.AttackingTeam, target);
    }

    ctx.View.WriteLine($"{target.Name} recibe {amount} de HP");
    ctx.View.WriteLine($"{target.Name} termina con HP:{target.Stats.HealthPoints}/{target.Stats.MaximumHealthPoints}");

    // Coste de turnos para skills de soporte: 0 Full, 1 Blinking (como esperan los tests)
    effect = new ActionEffect(0, 0, 1, ActionKind.Skill);
    return true;
}


private static void EnsureRevivedGoesToBench(Team team, Unit revived)
{
    // 1. Liberar el slot si estaba en el tablero (puestos 2..4 = índices 1..3)
    for (int i = 1; i <= 3 && i < team.TeamUnits.Count; i++)
    {
        if (ReferenceEquals(team.TeamUnits[i], revived))
        {
            team.TeamUnits[i] = null;
            break;
        }
    }

    // 2. Asegurar que esté en el roster si no está
    if (!team.TeamUnits.Contains(revived))
        team.TeamUnits.Add(revived);

    // 3. Reordenar la banca al orden original (manteniendo los nulls del tablero)
    ReorderBenchToOriginal(team);
}

private static void ReorderBenchToOriginal(Team team)
{
    const int FirstBenchIndex = 4;

    if (team.TeamUnits.Count <= FirstBenchIndex)
        return;

    var original = TeamUtils.GetOriginalOrderSnapshot(team);

    var onBoard = new HashSet<Unit>();
    for (int i = 1; i <= 3 && i < team.TeamUnits.Count; i++)
    {
        var u = team.TeamUnits[i];
        if (u != null) onBoard.Add(u);
    }

    var bench = new List<Unit>();
    for (int i = FirstBenchIndex; i < team.TeamUnits.Count; i++)
    {
        var u = team.TeamUnits[i];
        if (u == null) continue;
        if (onBoard.Contains(u)) continue;
        if (!bench.Contains(u)) bench.Add(u);
    }

    bench.Sort((a, b) =>
    {
        int ia = original.IndexOf(a);
        int ib = original.IndexOf(b);
        if (ia < 0 && ib < 0) return 0;
        if (ia < 0) return 1;
        if (ib < 0) return -1;
        return ia.CompareTo(ib);
    });

    if (team.TeamUnits.Count > FirstBenchIndex)
        team.TeamUnits.RemoveRange(FirstBenchIndex, team.TeamUnits.Count - FirstBenchIndex);

    team.TeamUnits.AddRange(bench);
}







private static bool IsHeal(Skill s) =>
    s.Type.Equals("Heal", StringComparison.OrdinalIgnoreCase);


private static decimal ComputeBaseAttackRaw(Unit attacker, AttackKind kind)
{
    int stat = kind == AttackKind.Ranged
        ? attacker.Stats.ShootingPower   // Gun → SKL
        : attacker.Stats.PhysicalAttackPower; // Phys → STR

    int modifier = kind == AttackKind.Ranged ? 80 : 54; // según pauta
    return (decimal)stat * modifier * 114m / 10000m;
}

private static bool ExecuteAttack(
    in ActionContext ctx,
    AttackKind kind,
    out ActionEffect effect,
    IDamageService? damageService = null,
    IAffinityResolver? affinityResolver = null)
{
    var target = Menus.SelectTarget(ctx.View, ctx.Actor.Name, ctx.DefendingTeam);
    if (target is null) { effect = default; return false; }
    

    ctx.View.WriteLine(CombatLogic.TextSeparator);
    ctx.View.WriteLine(kind == AttackKind.Ranged
        ? $"{ctx.Actor.Name} dispara a {target.Name}"
        : $"{ctx.Actor.Name} ataca a {target.Name}");

    var resolver = affinityResolver ?? new DefaultAffinityResolver();
    var element  = (kind == AttackKind.Ranged) ? Element.Gun : Element.Phys;
    var affinity = resolver.Resolve(ctx.Actor, target, element);

    var affSvc  = new AffinityService();
    decimal baseRaw = ComputeBaseAttackRaw(ctx.Actor, kind);
    var outcome = affSvc.Apply(affinity, baseRaw);

    int amountForText = affinity switch
    {
        Affinity.Repel => outcome.DamageToAttacker,
        Affinity.Drain => outcome.HealOnDefender,
        _              => 0
    };
    ViewRenderer.ReportAffinity(ctx.View, ctx.Actor, target, affinity, amountForText);

    if (outcome.DamageToDefender > 0)
    {
        DamageSystem.ApplyDamage(ctx.DefendingTeam, target, outcome.DamageToDefender);
        ctx.View.WriteLine($"{target.Name} recibe {outcome.DamageToDefender} de daño");
        ctx.View.WriteLine($"{target.Name} termina con HP:{target.Stats.HealthPoints}/{target.Stats.MaximumHealthPoints}");
    }
    else if (outcome.DamageToAttacker > 0)
    {
        DamageSystem.ApplyDamage(ctx.AttackingTeam, ctx.Actor, outcome.DamageToAttacker);
        ctx.View.WriteLine($"{ctx.Actor.Name} termina con HP:{ctx.Actor.Stats.HealthPoints}/{ctx.Actor.Stats.MaximumHealthPoints}");
    }
    else if (outcome.HealOnDefender > 0)
    {
        DamageSystem.Heal(target, outcome.HealOnDefender);
        ctx.View.WriteLine($"{target.Name} termina con HP:{target.Stats.HealthPoints}/{target.Stats.MaximumHealthPoints}");
    }
    else
    {
        ctx.View.WriteLine($"{target.Name} termina con HP:{target.Stats.HealthPoints}/{target.Stats.MaximumHealthPoints}");
    }
    
    effect = BuildEffectForAffinity(affinity, ActionKind.Attack);
    return true;
}


private static ActionEffect BuildEffectForAffinity(Affinity affinity, ActionKind kind)
{

    return affinity switch
    {
        Affinity.Weak    => new ActionEffect(FullTurnsLost: 1, BlinkTurnsGained: 1, BlinkTurnsLost: 0, kind),
        Affinity.Neutral => new ActionEffect(FullTurnsLost: 0, BlinkTurnsGained: 0, BlinkTurnsLost: 1, kind),
        Affinity.Resist  => new ActionEffect(FullTurnsLost: 0, BlinkTurnsGained: 0, BlinkTurnsLost: 1, kind),
        Affinity.Null    => new ActionEffect(FullTurnsLost: 0, BlinkTurnsGained: 0, BlinkTurnsLost: 2, kind),
        Affinity.Repel   => new ActionEffect(FullTurnsLost: int.MaxValue, BlinkTurnsGained: 0, BlinkTurnsLost: int.MaxValue, kind),
        Affinity.Drain   => new ActionEffect(FullTurnsLost: int.MaxValue, BlinkTurnsGained: 0, BlinkTurnsLost: int.MaxValue, kind),
        _                => new ActionEffect(FullTurnsLost: 1, BlinkTurnsGained: 0, BlinkTurnsLost: 0, kind),
    };
}



    private static bool HandleSummonSamurai(in ActionContext ctx, out ActionEffect effect) =>
        TeamBoard.HandleSummonAsSamurai(ctx, out effect);

    private static bool HandleSummonMonster(in ActionContext ctx, out ActionEffect effect) =>
        TeamBoard.HandleSummonAsMonster(ctx, out effect);
    
    
    private static bool HandleInvitation(in ActionContext ctx, int mpCost, out ActionEffect effect)
    {
        effect = default;


        
        var onFieldAlive = new HashSet<Unit>(ctx.Order);

        // Candidatos = (OriginalRoster del equipo atacante) - (vivos en tablero) - (samurái)
        var candidates = ctx.OriginalRoster
            .Where(u => !TeamUtils.IsSamurai(u) && !onFieldAlive.Contains(u))
            .ToList();

        
        ctx.View.WriteLine("Seleccione un monstruo para invocar");
        for (int i = 0; i < candidates.Count; i++)
        {
            var u = candidates[i];
            ctx.View.WriteLine($"{i + 1}-{u.Name} HP:{u.Stats.HealthPoints}/{u.Stats.MaximumHealthPoints} MP:{u.Stats.ManaPoints}/{u.Stats.MaximumManaPoints}");
        }
        ctx.View.WriteLine($"{candidates.Count + 1}-Cancelar");
        
        var line = ctx.View.ReadLine();
        if (!int.TryParse(line, out int pick) || pick < 1 || pick > candidates.Count + 1)
            return false;
        if (pick == candidates.Count + 1) return false;

        var chosen = candidates[pick - 1];
        
        return TeamBoard.PlaceSummonedChoosingAnySlot(in ctx, chosen, mpCost, out effect);


    }


}
