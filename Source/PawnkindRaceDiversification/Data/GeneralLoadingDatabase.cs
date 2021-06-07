using AlienRace;
using System.Collections.Generic;
using Verse;

namespace PawnkindRaceDiversification.Data
{
    internal sealed class GeneralLoadingDatabase
    {
        internal static List<string> impliedRacesLoaded = new List<string>();
        internal static List<PawnKindDef> pawnKindDefsExcluded = new List<PawnKindDef>();
        internal static Dictionary<PawnKindDef, string> pawnKindRaceDefRelations = new Dictionary<PawnKindDef, string>();
        internal static Dictionary<string, List<string>> raceHairTagData = new Dictionary<string, List<string>>();
    }
}
