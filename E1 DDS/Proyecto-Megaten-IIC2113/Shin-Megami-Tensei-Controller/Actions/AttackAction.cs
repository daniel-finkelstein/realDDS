using Shin_Megami_Tensei_Models;
using Shin_Megami_Tensei_View;
using Shin_Megami_Tensei.Combat;

namespace Shin_Megami_Tensei.Actions;

internal sealed class AttackAction : ICombatAction
{
    internal enum AttackMode { Melee, Ranged }

    private readonly AttackMode _mode;
    public AttackAction(AttackMode mode) => _mode = mode;

    public bool TryExecute(in ActionHandler.ActionContext ctx, out ActionHandler.ActionEffect effect)
    {
        var target = Menus.SelectTarget(ctx.View, ctx.Actor.Name, ctx.DefendingTeam);
        if (target is null) { effect = default; return false; }

        ctx.View.WriteLine(CombatLogic.TextSeparator);
        ctx.View.WriteLine(_mode == AttackMode.Ranged
            ? $"{ctx.Actor.Name} dispara a {target.Name}"
            : $"{ctx.Actor.Name} ataca a {target.Name}");

        var resolver = new DefaultAffinityResolver();
        var element  = (_mode == AttackMode.Ranged) ? Element.Gun : Element.Phys;
        var affinity = resolver.Resolve(ctx.Actor, target, element);

        var affSvc  = new AffinityService();
        decimal baseRaw = ComputeBaseAttackRaw(ctx.Actor, _mode);
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

        effect = ActionHandler.BuildEffectForAffinity(affinity, ActionHandler.ActionKind.Attack);
        return true;
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
