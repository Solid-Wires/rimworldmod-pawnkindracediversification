using RimWorld;
using System.Collections.Generic;

namespace PawnkindRaceDiversification.Extensions
{
    public sealed class FactionWeight
    {
        public string factionDef;

        public List<string> backstoryCategories;
        public List<BackstoryCategoryFilter> backstoryFilters;
        public bool overrideBackstories = false;

        public float weight;
    }
}
