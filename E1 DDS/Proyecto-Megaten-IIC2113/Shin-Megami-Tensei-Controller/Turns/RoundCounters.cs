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

        /// <summary>
        /// fullCost: costo en Full preferido (p.ej. Weak = 1 Full y gana 1 Blink).
        /// blinkGain: blinks que se ganan SOLO si el pago se hizo con Full (regla de la tabla).
        /// blinkCost: costo en Blink preferido (p.ej. Neutral/Resist = 1 Blink; Null = 2 Blink;
        ///           Pass/Summon = 1 Blink, y si falta cae a Full y gana 1 Blink).
        /// Devuelve lo efectivamente usado/ganado para imprimir.
        /// </summary>
        public (int fullUsed, int blinkUsed, int blinkGained)
            ApplyCost(int fullCost, int blinkGain, int blinkCost)
        {
            int usedFull = 0, usedBlink = 0, gainedBlink = 0;

            // 1) Costos que PREFIEREN BLINK (Neutral/Resist/Null/Pass/Summon)
            if (blinkCost > 0)
            {
                // Pagar con Blink primero
                int fromBlink = Math.Min(Blink, blinkCost);
                Blink    -= fromBlink;
                usedBlink += fromBlink;

                int remainder = blinkCost - fromBlink;
                if (remainder > 0)
                {
                    // Faltó Blink -> cae a Full por lo que falte
                    int fromFull = Math.Min(Full, remainder);
                    Full    -= fromFull;
                    usedFull += fromFull;

                    // Solo si tuvimos que caer a Full se otorga el blinkGain (regla de Pass/Summon)
                    if (fromFull > 0 && blinkGain > 0)
                    {
                        Blink       += blinkGain;
                        gainedBlink += blinkGain;
                    }
                }
            }

            // 2) Costos que PREFIEREN FULL (Weak)
            if (fullCost > 0)
            {
                int payWithFull = Math.Min(Full, fullCost);
                Full    -= payWithFull;
                usedFull += payWithFull;

                if (payWithFull == fullCost)
                {
                    // Se pagó TODO con Full -> se otorga blinkGain (Weak: +1 Blink)
                    if (blinkGain > 0)
                    {
                        Blink       += blinkGain;
                        gainedBlink += blinkGain;
                    }
                }
                else
                {
                    // No había Full suficiente -> cae a Blink por el resto (Weak: consume 1 Blink y NO gana Blink)
                    int remainder = fullCost - payWithFull;
                    int fromBlink = Math.Min(Blink, remainder);
                    Blink    -= fromBlink;
                    usedBlink += fromBlink;
                    // NO se otorga blinkGain cuando se cae a Blink
                }
            }

            return (usedFull, usedBlink, gainedBlink);
        }
    }
}
