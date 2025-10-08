using Shin_Megami_Tensei_View;

namespace Shin_Megami_Tensei.Actions;

internal interface ICombatAction
{
    bool TryExecute(in ActionHandler.ActionContext ctx, out ActionHandler.ActionEffect effect);
}