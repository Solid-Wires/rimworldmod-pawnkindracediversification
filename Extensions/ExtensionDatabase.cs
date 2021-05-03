using AlienRace;
using System.Collections.Generic;
using Verse;
using RimWorld;

namespace PawnkindRaceDiversification.Extensions
{
    internal sealed class ExtensionDatabase
    {
        internal static Dictionary<string, RaceDiversificationPool> racesDiversified = new Dictionary<string, RaceDiversificationPool>();
        internal static Dictionary<string, ThingDef_AlienRace> racesLoaded = new Dictionary<string, ThingDef_AlienRace>();
        internal static List<string> impliedRacesLoaded = new List<string>();
        internal static List<PawnKindDef> pawnKindDefsExcluded = new List<PawnKindDef>();
        internal static Dictionary<PawnKindDef, string> pawnKindRaceDefRelations = new Dictionary<PawnKindDef, string>();
    }
}
