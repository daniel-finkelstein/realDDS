using System;
using System.Collections.Generic;


 
namespace Shin_Megami_Tensei_Models;

public class Team
{
    public List<Unit> TeamUnits { get; set; }
    
    public Team()
        {
        TeamUnits = new List<Unit>();
        }

    public void AddUnit(Unit unit)
    {
        TeamUnits.Add(unit);
    }

    public override string ToString()
    {
        return string.Join("\n", TeamUnits);
    }
}