using System;
using System.Collections.Generic;
using System.Linq;
using Shin_Megami_Tensei_Models;
using Shin_Megami_Tensei_View;
using Shin_Megami_Tensei.Combat;
using Shin_Megami_Tensei.Utils;

namespace Shin_Megami_Tensei.Actions;

internal sealed class Skillhandler : ICombatAction
{
    private static readonly Dictionary<object, int> _skillKByTeam = new();

    public bool IsExecuteActionSuccess(in ActionHandler.ActionContext ctx, out ActionHandler.ActionEffect effect)
    {
        var usable = TeamUtils.GetUsableSkills(ctx.Actor);
        var chosen = Menus.SelectSkill(ctx.View, ctx.Actor, usable);
        if (chosen is null) { effect = default; return false; }

        ICombatAction action;
        
        if (string.Equals(chosen.Name, "Sabbatma", StringComparison.OrdinalIgnoreCase))
        {
            ctx.View.WriteLine(CombatLogic.TextSeparator);
            action = new SkillSabbatmaAction(chosen.Cost);
        }
        else if (string.Equals(chosen.Name, "Invitation", StringComparison.OrdinalIgnoreCase))
        {
            ctx.View.WriteLine(CombatLogic.TextSeparator);
            action = new SkillInvitationAction(chosen.Cost);
        }
        else if (IsHeal(chosen))
        {
            action = new SkillHealAction(chosen);
        }
        else
        {
            int hits = GetSkillHitCount(ctx, chosen);
            action = (hits > 1)
                ? new SkillMultiHitAction(chosen, hits)
                : new SkillSingleHitAction(chosen);
        }

        bool ok = action.IsExecuteActionSuccess(in ctx, out effect);
        if (ok) BumpTeamSkillK(ctx.AttackingTeam);
        return ok;
    }
    

    internal static bool IsHeal(Skill s) =>
        s.Type.Equals("Heal", StringComparison.OrdinalIgnoreCase);

    internal static Element MapSkillTypeToElement(string type) =>
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

    internal static int GetSkillOffensiveStat(Unit attacker, string skillType)
    {
        var s = attacker.Stats;
        return skillType.Equals("Phys", StringComparison.OrdinalIgnoreCase) ? s.PhysicalAttackPower :
               skillType.Equals("Gun",  StringComparison.OrdinalIgnoreCase) ? s.ShootingPower :
                              s.MagicalAttackPower;
    }

    private static (int A, int B)? GetHitsRangeForSkill(Skill skill)
    {
        string name = (skill?.Name ?? "").Trim().ToLowerInvariant();
        if (name.EndsWith(" claw")) return (1, 3); // claws 1..3 deterministas por K de equipo
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
    
    
    public static string DescribeElementCast(Element e) => e switch
    {
        Element.Force => "lanza viento",
        Element.Fire  => "lanza fuego",
        Element.Ice   => "lanza hielo",
        Element.Elec  => "lanza electricidad",
        Element.Gun   => "dispara",
        Element.Phys  => "ataca",
        _             => "usa una habilidad sobre"
    };
}
