using AlienRace;
using PawnkindRaceDiversification.Extensions;
using PawnkindRaceDiversification.Handlers;
using RimWorld;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Verse;
using static PawnkindRaceDiversification.Data.GeneralLoadingDatabase;
using static PawnkindRaceDiversification.Extensions.ExtensionDatabase;

namespace PawnkindRaceDiversification.Patches
{
    public static class PawnkindGenerationHijacker
    {
        //This can be set to true to prevent pawns from being generated with race weights.
        private static bool weightGeneratorPaused = false;
        private static int timesGeneratorPaused = 0;
        private static bool generatedBackstoryInfo = false;
        //private static List<StyleItemTagWeighted> prevPawnkindItemSettings = null;
        private static List<BackstoryCategoryFilter> prevFactionBackstoryCategoryFilters = null;
        private static List<string> prevPawnkindBackstoryCategories = null;
        private static List<BackstoryCategoryFilter> prevPawnkindBackstoryCategoryFilters = null;
        public static void PauseWeightGeneration()
        {
            //Every time weight generation is requested to be paused, the counter resets.
            //  This is to avoid accidentally unpausing when it shouldn't be.
            timesGeneratorPaused = 0;
            weightGeneratorPaused = true;
        }
        //public static bool DidRaceGenerate() => justGeneratedRace;
        public static bool IsPawnOfPlayerFaction { get; private set; } = false;

        public static bool IsKindValid(PawnGenerationRequest request, bool contextBefore)
        {
            //These steps make sure whether it is really necessary to modify this pawn
            //   or not.
            /*Precautions taken:
                *  1.) kindDef isn't null
                *  2.) kindDef is a humanlike
                *  3.) kindDef isn't an excluded kind def
                *  4.) raceDef isn't an implied race (pawnmorpher compatibility)
                *  5.) faction isn't the pawnmorpher factions (pawnmorpher compatibility)
                *  6.) The weight generator isn't paused
                *  7.) Prepare Carefully isn't doing anything
                *  8.) kindDef is human and settings want to override all human pawnkinds
                *  9.) The age of this request is consistent with the age of the race
                *      OR  kindDef isn't human and settings want to override all alien pawnkinds.
            * */
            if (request.KindDef != null
                && (request.KindDef.RaceProps != null
                    && request.KindDef.RaceProps.Humanlike)
                && !(pawnKindDefsExcluded.Contains(request.KindDef.defName))
                && !(impliedRacesLoaded.Contains(request.KindDef.race.defName))
                && !(request.Faction != null && (request.Faction.def.defName == "PawnmorpherPlayerColony" || request.Faction.def.defName == "PawnmorpherEnclave"))
                && !(weightGeneratorPaused)
                && !(PrepareCarefullyTweaks.loadedAlienRace != "none")
                && (IsValidAge(request.KindDef, request))
                && 
                (!contextBefore //Context after-determining is different at this point here
                || (
                ((request.KindDef.race == ThingDefOf.Human && ModSettingsHandler.OverrideAllHumanPawnkinds)
                || (request.KindDef.race != ThingDefOf.Human && ModSettingsHandler.OverrideAllAlienPawnkinds))
                )))
                return true;
            return false;
        }

        //Harmony manual prefix method
        public static void DetermineRace(PawnGenerationRequest request)
        {
            //Unpause the weight generator if the generator was paused 4 times.
            if (timesGeneratorPaused >= 4)
            {
                if (ModSettingsHandler.DebugMode)
                    PawnkindRaceDiversification.Logger.Warning("The race generator was left paused! Please report this to me! Unpausing the weight generator to avert this.");
                weightGeneratorPaused = false;
                timesGeneratorPaused = 0;
            }
            try
            {
                if (IsKindValid(request, true))
                {
                    //Change this kindDef's race to the selected race temporarily.
                    request.KindDef.race = WeightedRaceSelectionProcedure(request.KindDef, request.Faction);
                    //StyleFixProcedure(request.KindDef);
                    BackstoryInjectionProcedure(request.KindDef, request.Faction?.def);

                    IsPawnOfPlayerFaction = request.Faction != null ? request.Faction.IsPlayer : false;

                    //Reset the failsafe (since it generated a pawn)
                    timesGeneratorPaused = 0;
                    //PawnkindRaceDiversification.Logger.Message("Selecting race...");
                }
            } catch (Exception e)
            {
                if (PawnkindRaceDiversification.IsDebugModeInSettingsActive())
                {
                    string err = "PRD encountered an error BEFORE generating a pawn! Stacktrace: \n";
                    err += e.ToString();
                    PawnkindRaceDiversification.Logger.Error(err);
                }
                else
                {
                    PawnkindRaceDiversification.Logger.Error(e.StackTrace.ToString());
                }
            }
            //Count the amount of times the generator was left paused.
            if (weightGeneratorPaused)
                timesGeneratorPaused++;
        }
        public static void AfterDeterminedRace(PawnGenerationRequest request)
        {
            try
            {
                if (IsKindValid(request, false))
                {
                    //It's okay if this runs twice, because we want to be EXTRA sure that the pawnkind is fully reset
                    //  after a race was selected.

                    //Shouldn't have to null check this, but it could be possible...
                    if (request.KindDef != null)
                    {
                        //Reset this kindDef's style settings (obsolete)
                        /*
                        if (prevPawnkindItemSettings != null)
                            request.KindDef.styleItemTags = prevPawnkindItemSettings.ListFullCopyOrNull();
                        */
                        //Reset this kindDef's race
                        string raceDefName = pawnKindRaceDefRelations.TryGetValue(request.KindDef.defName);
                        request.KindDef.race = raceDefName != null ? racesLoaded.TryGetValue(raceDefName) : racesLoaded.First(r => r.Key.ToLower() == "human").Value;

                        //Reset backstory-related lists
                        if (generatedBackstoryInfo)
                        {
                            if (request.Faction != null)
                                request.Faction.def.backstoryFilters = prevFactionBackstoryCategoryFilters.ListFullCopyOrNull();
                            request.KindDef.backstoryCategories = prevPawnkindBackstoryCategories.ListFullCopyOrNull();
                            request.KindDef.backstoryFilters = prevPawnkindBackstoryCategoryFilters.ListFullCopyOrNull();
                            generatedBackstoryInfo = false;
                        }
                    }

                    IsPawnOfPlayerFaction = false;
                    //Reset remembered pawnkind hair tags (obsolete).
                    //prevPawnkindItemSettings = null;
                    //Reset backstory-related things.
                    prevFactionBackstoryCategoryFilters = null;
                    prevPawnkindBackstoryCategories = null;
                    prevPawnkindBackstoryCategoryFilters = null;
                    //PawnkindRaceDiversification.Logger.Message("Race selected and reset successfully.");
                }
            }
            catch (Exception e)
            {
                if (PawnkindRaceDiversification.IsDebugModeInSettingsActive())
                {
                    string err = "PRD encountered an error AFTER generating a pawn! Stacktrace: \n";
                    err += e.ToString();
                    PawnkindRaceDiversification.Logger.Error(err);
                }
                else
                {
                    PawnkindRaceDiversification.Logger.Error(e.StackTrace.ToString());
                }
            }
            //Unpause the weight generator (SHOULD ALWAYS UNPAUSE IF THIS METHOD RUNS AT ALL).
            weightGeneratorPaused = false;
        }

        public static ThingDef WeightedRaceSelectionProcedure(PawnKindDef pawnKind, Faction faction)
        {
            /*      Precedences for weights (first-to-last):
             *          1.) Flat weight (user settings per-save)
             *              ~ World Load
             *          2.) Flat weight (user settings global)
             *          *3.) Flat weight (set by AlienRace XML)
             *              + **Pawnkind weight
             *              + **Faction weight
             *      *Weight chance increases from these conditions, but flat weight in settings overrides these
             *      **If either of these weights are negative, then this pawn cannot spawn in these conditions
             * */
            Dictionary<string, float> determinedWeights = new Dictionary<string, float>();

            foreach (KeyValuePair<string, RaceDiversificationPool> data in racesDiversified)
            {
                //Faction weight
                FactionWeight factionWeight = null;
                if (faction != null)
                    factionWeight = data.Value.factionWeights?.Find(f => f.factionDef == faction.def.defName);
                float fw = factionWeight != null ? factionWeight.weight : 0.0f;
                //Negative value would mean that this pawn shouldn't generate with this faction.
                //  Skip this race.
                if (fw < 0.0f)
                {
                    determinedWeights.Remove(data.Key);
                    continue;
                }
                determinedWeights.SetOrAdd(data.Key, fw);

                //Pawnkind weight
                PawnkindWeight pawnkindWeight = data.Value.pawnKindWeights?.Find(p => p.pawnKindDef == pawnKind.defName);
                float pw = pawnkindWeight != null ? pawnkindWeight.weight : 0.0f;
                //Negative value would mean that this pawn shouldn't generate as this pawnkind.
                //  Skip this race.
                if (pw < 0.0f)
                {
                    determinedWeights.Remove(data.Key);
                    continue;
                }
                determinedWeights.SetOrAdd(data.Key, pw + fw);

                //Flat generation weight
                float w = data.Value.flatGenerationWeight;
                //Negative value means that this is not modifiable in the mod options, but this race wants
                //  to add weights on its own.
                if (w < 0.0f) determinedWeights.SetOrAdd(data.Key, pw + fw);
                else determinedWeights.SetOrAdd(data.Key, w + pw + fw);
            }
            //Flat generation weight, determined by user settings (applied globally)
            //  Overrides previous weight calculations.
            //  Prevented if race has a negative flat weight.
            foreach (KeyValuePair<string, float> kv in ModSettingsHandler.setFlatWeights)
            {
                if (kv.Value >= 0.0f)
                    determinedWeights.SetOrAdd(kv.Key, kv.Value);
            }
            //Flat generation weight, determined by user settings (applied locally)
            //  Overrides previous weight calculations.
            //  Prevented if race has a negative flat weight.
            foreach (KeyValuePair<string, float> kv in ModSettingsHandler.setLocalFlatWeights)
            {
                if (kv.Value >= 0.0f)
                    determinedWeights.SetOrAdd(kv.Key, kv.Value);
            }
            //Calculate race selection with a weighting procedure
            float sumOfWeights = 0.0f;
            foreach (KeyValuePair<string, float> w in determinedWeights)
                sumOfWeights += w.Value;
            float rnd = Rand.Value * sumOfWeights;
            try
            {
                foreach (KeyValuePair<string, float> w in determinedWeights)
                {
                    if (rnd < w.Value)
                    {
                        return racesLoaded[w.Key];
                    }
                    rnd -= w.Value;
                }
            } catch (Exception)
            {
                //If you see this, contact me about this.
                PawnkindRaceDiversification.Logger.Warning("Failed to assign weighted race! Defaulting to the original race from the pawnkind instead.");
            }
            
            //Return the original pawnkind race if no race selected
            return pawnKind.race;
        }

        /* Good news! Looking at HAR's code, this seems to have been made redundant.
         * This will be commented out in case it is needed ever again.
        private static void StyleFixProcedure(PawnKindDef pawnkindDef)
        {
            //HAR does not handle hair generation for pawnkinds, therefore I will fix this myself.
            //  To revert to default behavior that HAR already does with factions, I can temporarily set
            //  the pawnkind hairtags to null in order to stop forced hair generation.
            //Pawns that are allowed to have forced hair are pawns that already do spawn with hair (will change this later).
            //  However, pawns that are not supposed to spawn with hair should not have forced pawnkind hair gen.
            if (pawnkindDef?.styleItemTags != null)
            {
                Dictionary<Type, StyleSettings> loadedStyleSettings = raceStyleData.TryGetValue(pawnkindDef.race.defName);
                if (loadedStyleSettings != null)
                {
                    List<StyleItemTagWeighted> insertedStyleItemTags = new List<StyleItemTagWeighted>();
                    List<StyleItemTagWeighted> overriddenStyleItemTags = new List<StyleItemTagWeighted>();
                    bool foundOverrides = false;
                    prevPawnkindItemSettings = pawnkindDef.styleItemTags;
                    //Start finding tag overrides first
                    foreach (KeyValuePair<Type, StyleSettings> ts in loadedStyleSettings)
                    {
                        if (ts.Value != null
                            && ts.Value.hasStyle
                            && (ts.Key == typeof(TattooDef) && ModLister.IdeologyInstalled)
                            && ((ts.Value.styleTags?.Count > 0) || (ts.Value.styleTagsOverride?.Count > 0)))
                        {
                            if (ts.Value.styleTags != null)
                                foreach (string s in ts.Value.styleTags)
                                {
                                    insertedStyleItemTags.Add(new StyleItemTagWeighted(s, 1.0f));
                                    overriddenStyleItemTags.Add(new StyleItemTagWeighted(s, 1.0f));
                                }
                            if (ts.Value.styleTagsOverride != null)
                                foreach (string s in ts.Value.styleTagsOverride)
                                {
                                    overriddenStyleItemTags.Add(new StyleItemTagWeighted(s, 1.0f));
                                    foundOverrides = true;
                                }
                        }
                    }
                    if (foundOverrides)
                        pawnkindDef.styleItemTags = overriddenStyleItemTags;
                    else if (insertedStyleItemTags.Count > 0)
                        pawnkindDef.styleItemTags?.AddRange(insertedStyleItemTags);
                }
            }
        }
        */

        private static void BackstoryInjectionProcedure(PawnKindDef pawnkindDef, FactionDef factionDef)
        {
            /*  Backstory precedences (first-to-last):
             *      1.) Pawnkind backstories
             *      2.) Faction backstories
             *      3.) General backstories
             */
            string race = pawnkindDef.race.defName;
            if (racesDiversified.ContainsKey(race))
            {
                //Extension data
                RaceDiversificationPool raceExtensionData = racesDiversified[race];
                FactionWeight factionWeightData = null;
                if (factionDef != null)
                    factionWeightData = raceExtensionData.factionWeights?.FirstOrFallback(w => w.factionDef == factionDef.defName);
                PawnkindWeight pawnkindWeightData = raceExtensionData.pawnKindWeights?.FirstOrFallback(w => w.pawnKindDef == pawnkindDef.defName);
                

                //Handled backstory data
                List<string> backstoryCategories = new List<string>();
                List<BackstoryCategoryFilter> backstoryPawnkindFilters = new List<BackstoryCategoryFilter>();
                List<BackstoryCategoryFilter> backstoryFactionFilters = new List<BackstoryCategoryFilter>();
                bool pawnkindBackstoryOverride = false;
                bool factionBackstoryOverride = false;

                //Procedure
                backstoryCategories.AddRange(pawnkindWeightData?.backstoryCategories ?? new List<string>());
                backstoryPawnkindFilters.AddRange(pawnkindWeightData?.backstoryFilters ?? new List<BackstoryCategoryFilter>());
                pawnkindBackstoryOverride = pawnkindWeightData != null ? pawnkindWeightData.overrideBackstories : false;
                if (!pawnkindBackstoryOverride
                && !raceExtensionData.overrideBackstories)
                {
                    backstoryCategories.AddRange(pawnkindDef.backstoryCategories ?? new List<string>());
                    backstoryPawnkindFilters.AddRange(pawnkindDef.backstoryFilters ?? new List<BackstoryCategoryFilter>());
                }
                if (!pawnkindBackstoryOverride)
                {
                    backstoryCategories.AddRange(factionWeightData?.backstoryCategories ?? new List<string>());
                    backstoryFactionFilters.AddRange(factionWeightData?.backstoryFilters ?? new List<BackstoryCategoryFilter>());
                    factionBackstoryOverride = factionWeightData != null ? factionWeightData.overrideBackstories : false;
                    if (!factionBackstoryOverride
                    && !raceExtensionData.overrideBackstories)
                    {
                        backstoryFactionFilters.AddRange(factionDef?.backstoryFilters ?? new List<BackstoryCategoryFilter>());
                    }
                }
                if (!pawnkindBackstoryOverride
                    && !factionBackstoryOverride)
                {
                    backstoryCategories.AddRange(raceExtensionData.backstoryCategories ?? new List<string>());
                    backstoryFactionFilters.AddRange(raceExtensionData.backstoryFilters ?? new List<BackstoryCategoryFilter>());
                }
                
                //Failsafe
                if (backstoryFactionFilters.Count == 0
                    && backstoryPawnkindFilters.Count == 0
                    && backstoryCategories.Count == 0)
                {
                    //Nothing happened here.
                    return;
                }

                //Back up the previous backstory information, so that we are not overriding it afterwards
                prevFactionBackstoryCategoryFilters = factionDef?.backstoryFilters.ListFullCopyOrNull();
                prevPawnkindBackstoryCategories = pawnkindDef.backstoryCategories.ListFullCopyOrNull();
                prevPawnkindBackstoryCategoryFilters = pawnkindDef.backstoryFilters.ListFullCopyOrNull();
                generatedBackstoryInfo = true;

                //Assignment
                if (factionDef != null)
                    factionDef.backstoryFilters = backstoryFactionFilters;
                pawnkindDef.backstoryCategories = backstoryCategories;
                pawnkindDef.backstoryFilters = backstoryPawnkindFilters;
            }
        }

        //Returns false if any conditions are met that would invalidate the age.
        private static bool IsValidAge(PawnKindDef kindDef, PawnGenerationRequest request)
        {
            /*
            if (ModSettingsHandler.DebugMode)
                PawnkindRaceDiversification.Logger.Message("Generated biological age: " + biologicalAge.ToString() + "\n"
                    + "Minimum allowed age: " + kindDef.race.race.ageGenerationCurve.Points[0].x.ToString() + "\n"
                    + "Maximum allowed age: " + kindDef.race.race.ageGenerationCurve.Points[kindDef.race.race.ageGenerationCurve.Points.Capacity - 1].x.ToString());
            */
            //Only invalidates if settings don't allow age overriding
            if (!ModSettingsHandler.OverridePawnsWithInconsistentAges)
            {
                //Invalid if younger than or older than what's supposed to be generated (doesn't appear to be used)
                /*
                if (kindDef.race.race.ageGenerationCurve.Points[0].x > biologicalAge
                    || kindDef.race.race.ageGenerationCurve.Points[kindDef.race.race.ageGenerationCurve.Points.Capacity - 1].x < biologicalAge)
                {
                    return false;
                }
                */
                //Invalid if generated as a newborn or the kindDef's min and max generated age is 0
                //  Assumed to be a child
                if (request.Newborn || (kindDef.minGenerationAge == 0 && kindDef.maxGenerationAge == 0))
                    return false;
            }
            return true;
        }
    }
}
