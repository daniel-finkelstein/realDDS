namespace Shin_Megami_Tensei.Actions;

internal sealed class PassAction : ICombatAction
{
    public bool IsExecuteActionSuccess(in ActionHandler.ActionContext ctx, out ActionHandler.ActionEffect effect)
    {
        effect = new ActionHandler.ActionEffect(0, 1, 1, ActionHandler.ActionKind.Pass);
        return true;
    }
}