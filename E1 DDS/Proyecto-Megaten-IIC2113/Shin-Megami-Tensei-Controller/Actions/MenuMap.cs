namespace Shin_Megami_Tensei.Actions;

public static class MenuMap
{
    public static ActionSelection MapForSamurai(int input) => input switch
    {
        1 => ActionSelection.Attack,
        2 => ActionSelection.Shoot,
        3 => ActionSelection.UseSkill,
        4 => ActionSelection.Summon,
        5 => ActionSelection.Pass,
        6 => ActionSelection.Surrender,
        _ => ActionSelection.Attack
    };
    
    public static ActionSelection MapForMonster(int input) => input switch
    {
        1 => ActionSelection.Attack,
        2 => ActionSelection.UseSkill,
        3 => ActionSelection.Summon,
        4 => ActionSelection.Pass,
        _ => ActionSelection.Attack
    };
}