using System.Collections.Generic;

namespace Shin_Megami_Tensei_Models;

public class Monster : Unit
{
    public Monster(string name, Stats stats, IEnumerable<Skill>? skills = null)
        : base(name, "Monster", stats, skills) { }
}