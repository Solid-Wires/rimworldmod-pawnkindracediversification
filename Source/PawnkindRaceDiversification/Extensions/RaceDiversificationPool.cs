using System.Collections.Generic;
using Verse;

namespace PawnkindRaceDiversification.Extensions
{
    public class RaceDiversificationPool : DefModExtension
    {
        public List<FactionWeight> factionWeights;

        public List<PawnkindWeight> pawnKindWeights;

        public float flatGenerationWeight = 0.0f;
    }
}
