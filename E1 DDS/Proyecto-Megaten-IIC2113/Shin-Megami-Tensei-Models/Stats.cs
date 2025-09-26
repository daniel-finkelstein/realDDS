namespace Shin_Megami_Tensei_Models;

public class Stats
{
    public int HealthPoints { get; set; }
    public int ManaPoints { get; set; }
    public int PhysicalAttackPower { get; set; }
    public int ShootingPower { get; set; }
    public int MagicalAttackPower { get; set; }
    public int AttackOrder { get; set; }
    public int AbilityEffectiveness { get; set; }
    
    public int MaximumHealthPoints { get; set; }
    
    public int MaximumManaPoints { get; set; }

    public Stats(int hp, int mp, int str, int skl, int mag, int spd, int lck)
    {
        HealthPoints = hp;
        ManaPoints = mp;
        PhysicalAttackPower = str;
        ShootingPower = skl;
        MagicalAttackPower = mag;
        AttackOrder = spd;
        AbilityEffectiveness = lck;
        MaximumHealthPoints = hp;
        MaximumManaPoints = mp;

    }
    
    public void ApplyDamage(int amount)
        => HealthPoints = Math.Max(0, HealthPoints - Math.Max(0, amount));

    public void Heal(int amount)
        => HealthPoints = Math.Min(MaximumHealthPoints, HealthPoints + Math.Max(0, amount));

    public void SpendMana(int amount)
        => ManaPoints = Math.Max(0, ManaPoints - Math.Max(0, amount));

    public void RestoreMana(int amount)
        => ManaPoints = Math.Min(MaximumManaPoints, ManaPoints + Math.Max(0, amount));

    public override string ToString()
    {
        return $"HP:{HealthPoints} MP:{ManaPoints} STR:{PhysicalAttackPower} SKL:{ShootingPower} MAG:{MagicalAttackPower} SPD:{AttackOrder} LCK:{AbilityEffectiveness}";
    }
}