using System;
using System.Collections.Generic;
using System.Linq;
using Shin_Megami_Tensei_Models;
using Shin_Megami_Tensei_View;
using Shin_Megami_Tensei.Combat;
using Shin_Megami_Tensei.Utils;

namespace Shin_Megami_Tensei.Actions;

internal sealed class SkillAction : ICombatAction
{
    private static readonly Dictionary<object, int> _skillKByTeam = new();

    public bool TryExecute(in ActionHandler.ActionContext ctx, out ActionHandler.ActionEffect effect)
    {
        var usable = TeamUtils.GetUsableSkills(ctx.Actor);
        var chosen = Menus.SelectSkill(ctx.View, ctx.Actor, usable);
        if (chosen is null) { effect = default; return false; }

        // Casos especiales
        if (string.Equals(chosen.Name, "Sabbatma", StringComparison.OrdinalIgnoreCase))
        {
            ctx.View.WriteLine(CombatLogic.TextSeparator);
            bool ok = TeamBoard.HandleSabbatma(in ctx, chosen.Cost, out effect);
            if (ok) BumpTeamSkillK(ctx.AttackingTeam);
            return ok;
        }
        if (string.Equals(chosen.Name, "Invitation", StringComparison.OrdinalIgnoreCase))
        {
            ctx.View.WriteLine(CombatLogic.TextSeparator);
            bool ok = HandleInvitation(in ctx, chosen.Cost, out effect);
            if (ok) BumpTeamSkillK(ctx.AttackingTeam);
            return ok;
        }

        // Heal / Revive
        if (IsHeal(chosen))
        {
            bool ok = ExecuteSkillHealSingle(in ctx, chosen, out effect);
            if (ok) BumpTeamSkillK(ctx.AttackingTeam);
            return ok;
        }

        // Ataque (single/multi)
        return ExecuteSkillAttackSingle(in ctx, chosen, out effect);
    }

    // --- Invitation (idéntico al flujo actual) ---
    private static bool HandleInvitation(in ActionHandler.ActionContext ctx, int mpCost, out ActionHandler.ActionEffect effect)
    {
        effect = default;

        var onFieldAlive = new HashSet<Unit>(ctx.Order);
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
        if (!int.TryParse(line, out int pick) || pick < 1 || pick > candidates.Count + 1) return false;
        if (pick == candidates.Count + 1) return false;

        var chosen = candidates[pick - 1];
        return TeamBoard.PlaceSummonedChoosingAnySlot(in ctx, chosen, mpCost, out effect);
    }

    // --- Ataques de skill ---
    private static bool ExecuteSkillAttackSingle(in ActionHandler.ActionContext ctx, Skill skill, out ActionHandler.ActionEffect effect)
    {
        ctx.View.WriteLine(CombatLogic.TextSeparator);

        var target = Menus.SelectTarget(ctx.View, ctx.Actor.Name, ctx.DefendingTeam);
        if (target is null) { effect = default; return false; }

        int hits = GetSkillHitCount(ctx, skill);
        if (hits > 1)
            return ExecuteSkillAttackMulti(in ctx, skill, target, hits, out effect);

        ctx.View.WriteLine(CombatLogic.TextSeparator);

        var element = MapSkillTypeToElement(skill.Type);
        string verb = SkillRuntime.DescribeElementCast(element);
        ctx.View.WriteLine($"{ctx.Actor.Name} {verb} a {target.Name}");

        // MP
        var stats = ctx.Actor.Stats;
        if (stats.ManaPoints < skill.Cost) { effect = default; return false; }
        stats.ManaPoints = Math.Max(0, stats.ManaPoints - skill.Cost);

        // Base daño
        int stat = GetSkillOffensiveStat(ctx.Actor, skill.Type);
        decimal baseSkill = (decimal)Math.Sqrt((double)((decimal)stat * skill.Power));
        if (baseSkill < 0) baseSkill = 0;

        // Afinidad (Almighty => Neutral)
        var resolver = new DefaultAffinityResolver();
        var affinity = (element == Element.Almighty) ? Affinity.Neutral
                                                     : resolver.Resolve(ctx.Actor, target, element);

        var affSvc = new AffinityService();
        var outcome = affSvc.Apply(affinity, baseSkill);

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

        effect = ActionHandler.BuildEffectForAffinity(affinity, ActionHandler.ActionKind.Skill);
        BumpTeamSkillK(ctx.AttackingTeam);
        return true;
    }

    private static bool ExecuteSkillAttackMulti(in ActionHandler.ActionContext ctx, Skill skill, Unit target, int hits, out ActionHandler.ActionEffect effect)
    {
        ctx.View.WriteLine(CombatLogic.TextSeparator);

        var element = MapSkillTypeToElement(skill.Type);
        string verb = SkillRuntime.DescribeElementCast(element);

        // MP (una vez)
        var stats = ctx.Actor.Stats;
        if (stats.ManaPoints < skill.Cost) { effect = default; return false; }
        stats.ManaPoints = Math.Max(0, stats.ManaPoints - skill.Cost);

        int stat = GetSkillOffensiveStat(ctx.Actor, skill.Type);
        decimal basePerHit = (decimal)Math.Sqrt((double)((decimal)stat * skill.Power));
        if (basePerHit < 0) basePerHit = 0;

        var resolver = new DefaultAffinityResolver();
        var affinity = (element == Element.Almighty) ? Affinity.Neutral
                                                     : resolver.Resolve(ctx.Actor, target, element);

        var affSvc = new AffinityService();
        AffinityService.Outcome last = default;

        for (int i = 0; i < hits; i++)
        {
            ctx.View.WriteLine($"{ctx.Actor.Name} {verb} a {target.Name}");

            last = affSvc.Apply(affinity, basePerHit);

            int amountForText = affinity switch
            {
                Affinity.Repel => last.DamageToAttacker,
                Affinity.Drain => last.HealOnDefender,
                _              => 0
            };
            ViewRenderer.ReportAffinity(ctx.View, ctx.Actor, target, affinity, amountForText);

            if (last.DamageToDefender > 0)
                DamageSystem.ApplyDamage(ctx.DefendingTeam, target, last.DamageToDefender);
            else if (last.DamageToAttacker > 0)
                DamageSystem.ApplyDamage(ctx.AttackingTeam, ctx.Actor, last.DamageToAttacker);
            else if (last.HealOnDefender > 0)
                DamageSystem.Heal(target, last.HealOnDefender);
        }

        if (last.DamageToAttacker > 0)
            ctx.View.WriteLine($"{ctx.Actor.Name} termina con HP:{ctx.Actor.Stats.HealthPoints}/{ctx.Actor.Stats.MaximumHealthPoints}");
        else
            ctx.View.WriteLine($"{target.Name} termina con HP:{target.Stats.HealthPoints}/{target.Stats.MaximumHealthPoints}");

        effect = ActionHandler.BuildEffectForAffinity(affinity, ActionHandler.ActionKind.Skill);
        BumpTeamSkillK(ctx.AttackingTeam);
        return true;
    }

    // --- Heal / Revive ---
    private static bool ExecuteSkillHealSingle(in ActionHandler.ActionContext ctx, Skill skill, out ActionHandler.ActionEffect effect)
    {
        ctx.View.WriteLine(CombatLogic.TextSeparator);

        static bool IsReviveSkill(string name) =>
            name.Equals("Recarm", StringComparison.OrdinalIgnoreCase) ||
            name.Equals("Samarecarm", StringComparison.OrdinalIgnoreCase);

        bool revive = IsReviveSkill(skill.Name);

        var target = revive
            ? Menus.SelectDeadAllyTarget(ctx.View, ctx.Actor.Name, ctx.AttackingTeam)
            : Menus.SelectAllyTarget(ctx.View, ctx.Actor.Name, ctx.AttackingTeam);

        if (target is null) { effect = default; return false; }

        ctx.View.WriteLine(CombatLogic.TextSeparator);

        // MP
        var stats = ctx.Actor.Stats;
        if (stats.ManaPoints < skill.Cost) { effect = default; return false; }
        stats.ManaPoints = Math.Max(0, stats.ManaPoints - skill.Cost);

        if (revive && target.Stats.HealthPoints <= 0)
            ctx.View.WriteLine($"{ctx.Actor.Name} revive a {target.Name}");
        else
            ctx.View.WriteLine($"{ctx.Actor.Name} cura a {target.Name}");

        int maxHp = target.Stats.MaximumHealthPoints;
        int amount = (int)((decimal)maxHp * skill.Power / 100m);

        DamageSystem.Heal(target, amount);

        if (revive && target.Stats.HealthPoints > 0)
        {
            EnsureRevivedIsPlacedSafely(ctx.AttackingTeam, target);

            if (TeamUtils.IsSamurai(target))
            {
                var order = ctx.Order;
                var actor = ctx.Actor;

                order.Remove(target);

                int actorIdx = -1;
                for (int i = 0; i < order.Count; i++)
                    if (ReferenceEquals(order[i], actor)) { actorIdx = i; break; }
                if (actorIdx < 0) actorIdx = order.Count;

                order.Insert(actorIdx, target);
            }
        }

        ctx.View.WriteLine($"{target.Name} recibe {amount} de HP");
        ctx.View.WriteLine($"{target.Name} termina con HP:{target.Stats.HealthPoints}/{target.Stats.MaximumHealthPoints}");

        effect = new ActionHandler.ActionEffect(0, 0, 1, ActionHandler.ActionKind.Skill);
        return true;
    }

    private static void EnsureRevivedIsPlacedSafely(Team team, Unit revived)
    {
        bool isSamurai = TeamUtils.IsSamurai(revived);

        while (team.TeamUnits.Count < 4)
            team.TeamUnits.Add(null);

        if (isSamurai)
        {
            if (!ReferenceEquals(team.TeamUnits[0], revived))
            {
                for (int i = team.TeamUnits.Count - 1; i >= 1; i--)
                    if (ReferenceEquals(team.TeamUnits[i], revived))
                        team.TeamUnits.RemoveAt(i);
                team.TeamUnits[0] = revived;
            }
            else
            {
                for (int i = team.TeamUnits.Count - 1; i >= 1; i--)
                    if (ReferenceEquals(team.TeamUnits[i], revived))
                        team.TeamUnits.RemoveAt(i);
            }
        }
        else
        {
            for (int pos = 1; pos <= 3; pos++)
                if (pos < team.TeamUnits.Count && ReferenceEquals(team.TeamUnits[pos], revived))
                    team.TeamUnits[pos] = null;

            bool alreadyOnBench = false;
            for (int i = 4; i < team.TeamUnits.Count; i++)
                if (ReferenceEquals(team.TeamUnits[i], revived)) { alreadyOnBench = true; break; }
            if (!alreadyOnBench)
                team.TeamUnits.Add(revived);
        }
    }

    // --- Helpers ---
    private static bool IsHeal(Skill s) =>
        s.Type.Equals("Heal", StringComparison.OrdinalIgnoreCase);

    private static Element MapSkillTypeToElement(string type) =>
        type.ToLowerInvariant() switch
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

    private static int GetSkillOffensiveStat(Unit attacker, string skillType)
    {
        var s = attacker.Stats;
        return skillType.Equals("Phys", StringComparison.OrdinalIgnoreCase) ? s.PhysicalAttackPower :
               skillType.Equals("Gun",  StringComparison.OrdinalIgnoreCase) ? s.ShootingPower :
                              s.MagicalAttackPower;
    }

    private static (int A, int B)? GetHitsRangeForSkill(Skill skill)
    {
        string name = (skill?.Name ?? "").Trim().ToLowerInvariant();
        if (name.EndsWith(" claw")) return (1, 3);
        return null;
    }

    private static int GetSkillHitCount(in ActionHandler.ActionContext ctx, Skill skill)
    {
        var range = GetHitsRangeForSkill(skill);
        if (range is null) return 1;

        var (A, B) = range.Value;
        int len = B - A + 1;
        if (len <= 0) return Math.Max(1, A);

        int k = GetTeamSkillK(ctx.AttackingTeam);
        int offset = ((k % len) + len) % len;
        return A + offset;
    }

    private static int GetTeamSkillK(object teamKey)
    {
        _skillKByTeam.TryGetValue(teamKey, out int k);
        return k;
    }

    private static void BumpTeamSkillK(object teamKey)
    {
        _skillKByTeam[teamKey] = GetTeamSkillK(teamKey) + 1;
    }
}
