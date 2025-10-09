using Shin_Megami_Tensei.Utils;

namespace Shin_Megami_Tensei.Actions;

internal sealed class SurrenderAction : ICombatAction
{
    public bool IsExecuteActionSuccess(in ActionHandler.ActionContext ctx, out ActionHandler.ActionEffect effect)
    {
        ctx.View.WriteLine($"{ctx.AttackingSamuraiName} ({ctx.AttackerTag}) se rinde");
        TeamUtils.DefeatTeam(ctx.AttackingTeam);
        effect = new ActionHandler.ActionEffect(0, 0, 0, ActionHandler.ActionKind.Surrender);
        return true;
    }
}