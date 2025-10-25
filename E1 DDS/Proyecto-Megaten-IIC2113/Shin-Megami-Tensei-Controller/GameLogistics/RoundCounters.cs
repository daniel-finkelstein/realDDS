using System;

namespace Shin_Megami_Tensei.Turns
{
    internal struct RoundCounters
    {
        public int Full  { get; private set; }
        public int Blink { get; private set; }

        public RoundCounters(int full, int blink)
        {
            Full  = full;
            Blink = blink;
        }
        
        public (int fullUsed, int blinkUsed, int blinkGained)
            ApplyCost(int fullCost, int blinkGain, int blinkCost)
        {
            if (ShouldConsumeAll(fullCost, blinkCost))
                return ConsumeAll();

            int fullUsedTotal   = 0;
            int blinkUsedTotal  = 0;
            int blinkGainedTotal = 0;
            
            var (fu1, bu1, bg1) = PayBlinkCost(blinkCost, blinkGain);
            fullUsedTotal   += fu1;
            blinkUsedTotal  += bu1;
            blinkGainedTotal += bg1;
            
            var (fu2, bu2, bg2) = PayFullCost(fullCost, blinkGain);
            fullUsedTotal   += fu2;
            blinkUsedTotal  += bu2;
            blinkGainedTotal += bg2;

            return (fullUsedTotal, blinkUsedTotal, blinkGainedTotal);
        }
        

        private static bool ShouldConsumeAll(int fullCost, int blinkCost) =>
            fullCost < 0 && blinkCost < 0;

        private (int fullUsed, int blinkUsed, int blinkGained) ConsumeAll()
        {
            int fullUsed  = Full;
            int blinkUsed = Blink;
            Full  = 0;
            Blink = 0;
            return (fullUsed, blinkUsed, 0);
        }
        private (int fullUsed, int blinkUsed, int blinkGained)
            PayBlinkCost(int blinkCost, int blinkGain)
        {
            if (blinkCost <= 0) return (0, 0, 0);

            int blinkUsed = SpendBlink(blinkCost);
            int remainder = blinkCost - blinkUsed;
            if (remainder <= 0) return (0, blinkUsed, 0);

            int fullUsed = SpendFull(remainder);
            int blinkGained = (fullUsed > 0 && blinkGain > 0) ? GainBlink(blinkGain) : 0;

            return (fullUsed, blinkUsed, blinkGained);
        }

        private (int fullUsed, int blinkUsed, int blinkGained)
            PayFullCost(int fullCost, int blinkGain)
        {
            if (fullCost <= 0) return (0, 0, 0);

            int fullUsed = SpendFull(fullCost);
            int blinkGained = 0;

            if (fullUsed == fullCost)
            {
                blinkGained = (blinkGain > 0) ? GainBlink(blinkGain) : 0;
                return (fullUsed, 0, blinkGained);
            }

            int remainder = fullCost - fullUsed;
            int blinkUsed = SpendBlink(remainder);
            return (fullUsed, blinkUsed, blinkGained);
        }
        
        private int SpendBlink(int amount)
        {
            if (amount <= 0 || Blink <= 0) return 0;
            int used = Math.Min(Blink, amount);
            Blink -= used;
            return used;
        }
        
        private int SpendFull(int amount)
        {
            if (amount <= 0 || Full <= 0) return 0;
            int used = Math.Min(Full, amount);
            Full -= used;
            return used;
        }
        
        private int GainBlink(int amount)
        {
            if (amount <= 0) return 0;
            Blink += amount;
            return amount;
        }
    }
}
