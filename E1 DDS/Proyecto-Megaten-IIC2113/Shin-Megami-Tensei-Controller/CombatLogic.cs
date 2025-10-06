using Shin_Megami_Tensei_Models;
using Shin_Megami_Tensei_View;
using Shin_Megami_Tensei.Battle;

namespace Shin_Megami_Tensei;

public static partial class CombatLogic
{
    internal const string TextSeparator = "----------------------------------------";

    public static void Run(View view, Team player1Team, Team player2Team)
        => BattleRunner.Run(view, player1Team, player2Team);
}