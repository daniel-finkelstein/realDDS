using System.Collections.Generic;

namespace Shin_Megami_Tensei_Models;

public class Samurai : Unit
{
    public Samurai(string name, Stats stats, IEnumerable<Skill>? skills = null)
        : base(name, "Samurai", stats, skills) { }
}