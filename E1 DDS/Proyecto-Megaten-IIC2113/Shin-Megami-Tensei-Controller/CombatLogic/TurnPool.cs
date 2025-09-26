namespace Shin_Megami_Tensei;

internal sealed class TurnPool
{
    public int Full { get; private set; }
    public int Blink { get; private set; }

    public TurnPool(int full)
    {
        Full = full;
        Blink = 0;
    }
    
    public (int fullUsed, int blinkUsed) SpendOne()
    {
        if (Blink > 0)
        {
            Blink--;
            return (0, 1);
        }
        Full--;
        return (1, 0);
    }

    public void GainBlink(int n = 1) => Blink += n;

    public bool CanProceed(bool battleOver) =>
        (Full > 0 || Blink > 0) && !battleOver;
}