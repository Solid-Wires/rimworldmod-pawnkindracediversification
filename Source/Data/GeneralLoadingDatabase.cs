using AlienRace;
using System;
using System.Collections.Generic;
using Verse;

namespace PawnkindRaceDiversification.Data
{
    internal sealed class GeneralLoadingDatabase
    {
        internal static List<string> impliedRacesLoaded = new List<string>();
        internal static List<string> pawnKindDefsExcluded = new List<string>();
        internal static Dictionary<string, string> pawnKindRaceDefRelations = new Dictionary<string, string>();

        //Obsolete
        //internal static Dictionary<string, Dictionary<Type, StyleSettings>> raceStyleData = new Dictionary<string, Dictionary<Type, StyleSettings>>();
        /*
        public static void AddOrInsertStyle(string defName, Type type, StyleSettings style)
        {
            if (raceStyleData.ContainsKey(defName))
                raceStyleData[defName].Add(type, style);
            else
                raceStyleData.Add(defName, new Dictionary<Type, StyleSettings>() { {type, style} }) ;
            
        }
        */
    }
}
