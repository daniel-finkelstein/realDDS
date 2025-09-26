namespace Shin_Megami_Tensei_Models;

public class Skill
{
    public string Name { get; }
    public string Type { get; }
    public int Cost { get; }
    public int Power { get; }
    public string Target { get; }
    public int Hits { get; }
    public string Effect { get; }

    public Skill(string name, string type, int cost, int power, string target, int hits, string effect)
    {
        Name = name;
        Type = type;
        Cost = cost;
        Power = power;
        Target = target;
        Hits = hits;
        Effect = effect;

    }

    public override string ToString()
    {
        return $"Name:{Name} Type:{Type} Cost:{Cost} Power:{Power} Target:{Target} Hits:{Hits} Effect:{Effect}";
    }
}