namespace Shin_Megami_Tensei.Actions;

internal sealed class SummonAction : ICombatAction
{
    private readonly bool _actorIsSamurai;
    public SummonAction(bool actorIsSamurai) => _actorIsSamurai = actorIsSamurai;

    public bool TryExecute(in ActionHandler.ActionContext ctx, out ActionHandler.ActionEffect effect)
    {
        return _actorIsSamurai
            ? TeamBoard.HandleSummonAsSamurai(ctx, out effect)
            : TeamBoard.HandleSummonAsMonster(ctx, out effect);
    }
}