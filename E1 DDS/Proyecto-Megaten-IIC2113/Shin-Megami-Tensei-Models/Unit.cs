using System.Collections.Generic;
using System.Linq;

namespace Shin_Megami_Tensei_Models;

public abstract class Unit
{
    public string Name { get; set; }
    public string Type { get; set; }
    public Stats Stats { get; set; }

    // NUEVO: cualquier unidad puede tener skills
    public List<Skill> Skills { get; }

    protected Unit(string name, string type, Stats stats, IEnumerable<Skill>? skills = null)
    {
        Name = name;
        Type = type;
        Stats = stats;
        Skills = skills?.ToList() ?? new List<Skill>();
    }

    public override string ToString() => $"{Name} ({Type}) - {Stats}";
}