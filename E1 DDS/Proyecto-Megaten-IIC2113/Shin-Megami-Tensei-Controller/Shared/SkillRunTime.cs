using Shin_Megami_Tensei_Models;

internal static class SkillRuntime
{
    public static Element ToElement(string type) => type?.Trim() switch
    {
        "Phys"  => Element.Phys,
        "Gun"   => Element.Gun,
        "Fire"  => Element.Fire,
        "Ice"   => Element.Ice,
        "Elec"  => Element.Elec,
        "Force" => Element.Force,
        "Heal"  => Element.Heal,
        _       => Element.Neutral
    };

    public static bool IsHeal(string type) =>
        string.Equals(type, "Heal", StringComparison.OrdinalIgnoreCase);

    public static string DescribeElementCast(Element e) => e switch
    {
        Element.Force => "lanza viento",
        Element.Fire  => "lanza fuego",
        Element.Ice   => "lanza hielo",
        Element.Elec  => "lanza electricidad",
        Element.Gun   => "dispara",
        Element.Phys  => "ataca",
        _             => "usa una habilidad sobre"
    };
}