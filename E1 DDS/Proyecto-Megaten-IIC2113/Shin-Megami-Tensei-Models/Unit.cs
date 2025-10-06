using System.Collections.Generic;
using System.Linq;

namespace Shin_Megami_Tensei_Models;

public abstract class Unit
{
    public string Name { get; set; }
    public string Type { get; set; }
    public Stats Stats { get; set; }
    
    public List<Skill> Skills { get; }
    
    public AffinityProfile Affinities { get; }

    protected Unit(string name, string type, Stats stats, IEnumerable<Skill>? skills = null, AffinityProfile? affinities = null)
    {
        Name = name;
        Type = type;
        Stats = stats;
        Skills = skills?.ToList() ?? new List<Skill>();
        Affinities = affinities ?? AffinityProfile.NeutralAll;
    }

    public override string ToString() => $"{Name} ({Type}) - {Stats}";
}