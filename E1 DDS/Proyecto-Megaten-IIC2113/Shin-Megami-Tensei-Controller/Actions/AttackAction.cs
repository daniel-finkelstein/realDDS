using Shin_Megami_Tensei_Models;
using Shin_Megami_Tensei_View;
using Shin_Megami_Tensei.Combat;

namespace Shin_Megami_Tensei.Actions;

internal sealed class AttackAction : ICombatAction
{
    internal enum AttackMode { Melee, Ranged }

    private readonly AttackMode _mode;
    public AttackAction(AttackMode mode) => _mode = mode;

    public bool IsExecuteActionSuccess(in ActionHandler.ActionContext ctx, out ActionHandler.ActionEffect effect)
    {
        var target = Menus.SelectTarget(ctx.View, ctx.Actor.Name, ctx.DefendingTeam);
        if (target is null) { effect = default; return false; }

        WriteAttackIntro(ctx.View, ctx.Actor.Name, target.Name, _mode);

        var affinity = ResolveAffinityForMode(ctx.Actor, target, _mode);
        decimal baseRaw = ComputeBaseAttackRaw(ctx.Actor, _mode);

        var affSvc  = new AffinityService();
        var outcome = affSvc.Apply(affinity, baseRaw);
        
        int dmgToDef  = outcome.DamageToDefender;
        int dmgToAtk  = outcome.DamageToAttacker;
        int healOnDef = outcome.HealOnDefender;

        ReportAffinity(ctx.View, ctx.Actor, target, affinity, dmgToAtk, healOnDef);
        ApplyOutcomeAndPrint(ctx, target, dmgToDef, dmgToAtk, healOnDef);

        effect = ActionHandler.BuildEffectForAffinity(affinity, ActionHandler.ActionKind.Attack);
        return true;
    }

    private static void WriteAttackIntro(View view, string actorName, string targetName, AttackMode mode)
    {
        view.WriteLine(CombatLogic.TextSeparator);
        view.WriteLine(mode == AttackMode.Ranged
            ? $"{actorName} dispara a {targetName}"
            : $"{actorName} ataca a {targetName}");
    }

    private static Affinity ResolveAffinityForMode(Unit attacker, Unit target, AttackMode mode)
    {
        var resolver = new DefaultAffinityResolver();
        var element  = (mode == AttackMode.Ranged) ? Element.Gun : Element.Phys;
        return resolver.Resolve(attacker, target, element);
    }
    
    private static void ReportAffinity(View view, Unit actor, Unit target, Affinity affinity, int damageToAttacker, int healOnDefender)
    {
        int amountForText = affinity switch
        {
            Affinity.Repel => damageToAttacker,
            Affinity.Drain => healOnDefender,
            _              => 0
        };
        ViewRenderer.ReportAffinity(view, actor, target, affinity, amountForText);
    }

    private static void ApplyOutcomeAndPrint(in ActionHandler.ActionContext ctx, Unit target, int damageToDefender, int damageToAttacker, int healOnDefender)
    {
        if (damageToDefender > 0)
        {
            DamageSystem.ApplyDamage(ctx.DefendingTeam, target, damageToDefender);
            ctx.View.WriteLine($"{target.Name} recibe {damageToDefender} de daño");
            ctx.View.WriteLine($"{target.Name} termina con HP:{target.Stats.HealthPoints}/{target.Stats.MaximumHealthPoints}");
            return;
        }

        if (damageToAttacker > 0)
        {
            DamageSystem.ApplyDamage(ctx.AttackingTeam, ctx.Actor, damageToAttacker);
            ctx.View.WriteLine($"{ctx.Actor.Name} termina con HP:{ctx.Actor.Stats.HealthPoints}/{ctx.Actor.Stats.MaximumHealthPoints}");
            return;
        }

        if (healOnDefender > 0)
        {
            DamageSystem.Heal(target, healOnDefender);
        }
        
        ctx.View.WriteLine($"{target.Name} termina con HP:{target.Stats.HealthPoints}/{target.Stats.MaximumHealthPoints}");
    }

    private static decimal ComputeBaseAttackRaw(Unit attacker, AttackMode mode)
    {
        int stat = mode == AttackMode.Ranged
            ? attacker.Stats.ShootingPower
            : attacker.Stats.PhysicalAttackPower;

        int modifier = mode == AttackMode.Ranged ? 80 : 54;
        return (decimal)stat * modifier * 114m / 10000m;
    }
}
