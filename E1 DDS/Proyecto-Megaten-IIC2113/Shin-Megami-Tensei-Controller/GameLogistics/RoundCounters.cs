namespace Shin_Megami_Tensei.Turns
{
    internal struct RoundCounters
    {
        public int Full  { get; private set; }
        public int Blink { get; private set; }

        public RoundCounters(int full, int blink)
        {
            Full = full;
            Blink = blink;
        }

        public (int fullUsed, int blinkUsed, int blinkGained)
            ApplyCost(int fullCost, int blinkGain, int blinkCost)
        {
            int usedFull = 0, usedBlink = 0, gainedBlink = 0;

            // Repel/Drain: consume TODOS los turnos
            // Convención: señálalo pasando fullCost = -1 y blinkCost = -1
            if (fullCost < 0 && blinkCost < 0)
            {
                usedFull  = Full;
                usedBlink = Blink;
                Full  = 0;
                Blink = 0;
                return (usedFull, usedBlink, gainedBlink);
            }

            // Casos que prefieren BLINK (Null/Miss/Neutral/Resist y Pass/Summon)
            if (blinkCost > 0)
            {
                int fromBlink = Math.Min(Blink, blinkCost);
                Blink    -= fromBlink;
                usedBlink += fromBlink;

                int remainder = blinkCost - fromBlink;
                if (remainder > 0)
                {
                    int fromFull = Math.Min(Full, remainder);
                    Full    -= fromFull;
                    usedFull += fromFull;

                    // Sólo si caímos a Full otorgamos blinkGain (para Pass/Summon)
                    if (fromFull > 0 && blinkGain > 0)
                    {
                        Blink       += blinkGain;
                        gainedBlink += blinkGain;
                    }
                }
            }

            // Casos que prefieren FULL (Weak)
            if (fullCost > 0)
            {
                int payWithFull = Math.Min(Full, fullCost);
                Full    -= payWithFull;
                usedFull += payWithFull;

                if (payWithFull == fullCost)
                {
                    // Se pagó todo con Full -> gana Blink (Weak)
                    if (blinkGain > 0)
                    {
                        Blink       += blinkGain;
                        gainedBlink += blinkGain;
                    }
                }
                else
                {
                    // No alcanzó Full -> paga el resto con Blink (sin premio)
                    int remainder = fullCost - payWithFull;
                    int fromBlink = Math.Min(Blink, remainder);
                    Blink    -= fromBlink;
                    usedBlink += fromBlink;
                }
            }

            return (usedFull, usedBlink, gainedBlink);
        }

    }
}
