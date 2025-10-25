using System;
using System.Collections.Generic;
using System.Linq;
using Shin_Megami_Tensei.Actions;  // ActionHandler
using Shin_Megami_Tensei_Models;   // Unit, Team
using Shin_Megami_Tensei_View;     // Menus / View
using Shin_Megami_Tensei.Utils;

namespace Shin_Megami_Tensei.Combat
{
    internal enum TargetSide  { Self, Allies, Enemies }
    internal enum TargetShape { Single, All, RandomN }

    [Flags]
    internal enum TargetFilters { None = 0, OnlyAlive = 1, OnlyDead = 2, IncludeSelf = 4 }
    
    internal abstract class Targeting
    {
        internal TargetSide    Side    { get; init; }
        internal TargetShape   Shape   { get; init; }
        internal int           Count   { get; init; } = 1;
        internal TargetFilters Filters { get; init; } = TargetFilters.OnlyAlive;

        // Si true, Skill.TryExecute abrirá menú de selección
        internal virtual bool RequiresSelection => true;

        internal abstract IReadOnlyList<Unit> Select(in ActionHandler.ActionContext ctx, Unit actor);

        protected IEnumerable<Unit> Pool(in ActionHandler.ActionContext ctx, Unit actor)
        {
            IEnumerable<Unit?> pool = Side switch
            {
                TargetSide.Self    => new[] { actor },
                TargetSide.Allies  => ctx.AttackingTeam.TeamUnits,
                TargetSide.Enemies => ctx.DefendingTeam.TeamUnits,
                _ => Array.Empty<Unit?>()
            };

            var units = pool.Where(u => u != null).Cast<Unit>();

            if (Filters.HasFlag(TargetFilters.OnlyAlive)) units = units.Where(u => u.Stats.HealthPoints > 0);
            if (Filters.HasFlag(TargetFilters.OnlyDead))  units = units.Where(u => u.Stats.HealthPoints <= 0);
            if (!Filters.HasFlag(TargetFilters.IncludeSelf)) units = units.Where(u => !ReferenceEquals(u, actor));

            return units;
        }
    }

    // ===== CONCRETOS =====

    // Enemigo único vivo (daño single target)
    internal sealed class SingleEnemyTargeting : Targeting
    {
        internal SingleEnemyTargeting()
        {
            Side = TargetSide.Enemies; Shape = TargetShape.Single; Filters = TargetFilters.OnlyAlive;
        }

        internal override IReadOnlyList<Unit> Select(in ActionHandler.ActionContext ctx, Unit actor)
        {
            var target = Menus.SelectTarget(ctx.View, actor.Name, ctx.DefendingTeam);
            return target is null ? Array.Empty<Unit>() : new[] { target };
        }
    }

    // Aliado único vivo (curas)
    internal sealed class SingleAllyAliveTargeting : Targeting
    {
        internal SingleAllyAliveTargeting()
        {
            Side = TargetSide.Allies; Shape = TargetShape.Single;
            Filters = TargetFilters.OnlyAlive | TargetFilters.IncludeSelf;
        }

        internal override IReadOnlyList<Unit> Select(in ActionHandler.ActionContext ctx, Unit actor)
        {
            var target = Menus.SelectAllyTarget(ctx.View, actor.Name, ctx.AttackingTeam);
            return target is null ? Array.Empty<Unit>() : new[] { target };
        }
    }

    // Aliado único muerto (revivir)
    internal sealed class SingleAllyDeadTargeting : Targeting
    {
        internal SingleAllyDeadTargeting()
        {
            Side = TargetSide.Allies; Shape = TargetShape.Single;
            Filters = TargetFilters.OnlyDead | TargetFilters.IncludeSelf;
        }

        internal override IReadOnlyList<Unit> Select(in ActionHandler.ActionContext ctx, Unit actor)
        {
            var target = Menus.SelectDeadAllyTarget(ctx.View, actor.Name, ctx.AttackingTeam);
            return target is null ? Array.Empty<Unit>() : new[] { target };
        }
    }

    // Sin selección (para skills sin objetivo directo: Sabbatma / Invitation)
    internal sealed class NoTargetTargeting : Targeting
    {
        internal NoTargetTargeting()
        {
            Side = TargetSide.Self;
            Shape = TargetShape.Single;
            Filters = TargetFilters.OnlyAlive | TargetFilters.IncludeSelf;
        }

        internal override bool RequiresSelection => false;

        internal override IReadOnlyList<Unit> Select(in ActionHandler.ActionContext ctx, Unit actor)
            => Array.Empty<Unit>();
    }
}
