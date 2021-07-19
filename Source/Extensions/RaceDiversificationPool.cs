using System.Collections.Generic;
using RimWorld;
using Verse;

namespace PawnkindRaceDiversification.Extensions
{
    public class RaceDiversificationPool : DefModExtension
    {
        public List<FactionWeight> factionWeights;

        public List<PawnkindWeight> pawnKindWeights;

        public List<string> backstoryCategories;
        public List<BackstoryCategoryFilter> backstoryFilters;
        public bool overrideBackstories = false;

        public float flatGenerationWeight = 0.0f;
    }
}
