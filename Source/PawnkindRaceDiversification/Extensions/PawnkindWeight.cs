using RimWorld;
using System.Collections.Generic;
using Verse;

namespace PawnkindRaceDiversification.Extensions
{
    public sealed class PawnkindWeight
    {
        public string pawnKindDef;

        public List<string> backstoryCategories;
        public List<BackstoryCategoryFilter> backstoryFilters;
        public bool overrideBackstories = false;

        public float weight;
    }
}
