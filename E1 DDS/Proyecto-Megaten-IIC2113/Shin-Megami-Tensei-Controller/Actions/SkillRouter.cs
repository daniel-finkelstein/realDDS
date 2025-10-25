using System.Collections.Generic;
using Shin_Megami_Tensei_Models;
using Shin_Megami_Tensei_View;
using Shin_Megami_Tensei.Combat;
using Shin_Megami_Tensei.Utils;
using ModelSkill = Shin_Megami_Tensei_Models.Skill;

namespace Shin_Megami_Tensei.Actions
{
    internal sealed class SkillRouter : ICombatAction
    {
        // Estado K por equipo (para claws deterministas 1..3)
        private static readonly Dictionary<object, int> _skillKByTeam = new();

        internal static int GetTeamSkillK(object teamKey)
        {
            _skillKByTeam.TryGetValue(teamKey, out int k);
            return k;
        }
        private static void BumpTeamSkillK(object teamKey)
        {
            _skillKByTeam[teamKey] = GetTeamSkillK(teamKey) + 1;
        }

        public bool IsExecuteActionSuccess(in ActionHandler.ActionContext ctx, out ActionHandler.ActionEffect effect)
        {
            // ⚠️ Lista de skills del MODELO, sin chocar con Combat.Skill
            IList<ModelSkill> usable = TeamUtils.GetUsableSkills(ctx.Actor);

            // Usamos el helper nuevo (no modificamos tu Menus.cs original)
            ModelSkill? chosen = Menus.SelectSkill(ctx.View, ctx.Actor, usable);
            if (chosen is null) { effect = default; return false; }

            // Construye la skill (Effect + Targeting) y ejecútala
            var skill = SkillFactory.FromModel(chosen);
            bool ok = skill.TryExecute(in ctx, out effect);

            if (ok) BumpTeamSkillK(ctx.AttackingTeam);
            return ok;
        }
    }
}