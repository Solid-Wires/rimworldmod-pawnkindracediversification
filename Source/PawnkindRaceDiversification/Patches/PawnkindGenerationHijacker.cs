using PawnkindRaceDiversification.Extensions;
using PawnkindRaceDiversification.Handlers;
using RimWorld;
using System;
using System.Collections.Generic;
using Verse;
using static PawnkindRaceDiversification.Data.GeneralLoadingDatabase;
using static PawnkindRaceDiversification.Extensions.ExtensionDatabase;

namespace PawnkindRaceDiversification.Patches
{
    public static class PawnkindGenerationHijacker
    {
        //This can be set to true to prevent pawns from being generated with race weights.
        private static bool weightGeneratorPaused = false;
        private static bool justGeneratedRace = false;
        private static bool generatedBackstoryInfo = false;
        private static List<string> prevPawnkindHairtags = null;
        private static List<BackstoryCategoryFilter> prevFactionBackstoryCategoryFilters = null;
        private static List<string> prevPawnkindBackstoryCategories = null;
        private static List<BackstoryCategoryFilter> prevPawnkindBackstoryCategoryFilters = null;
        public static void PauseWeightGeneration()
        {
            weightGeneratorPaused = true;
        }
        public static bool DidRaceGenerate() => justGeneratedRace;
        public static bool IsPawnOfPlayerFaction { get; private set; } = false;

        //Harmony manual prefix method
        public static void DetermineRace(PawnGenerationRequest request)
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
             *      OR  kindDef isn't human and settings want to override all alien pawnkinds.
             * */
            PawnKindDef kindDef = request.KindDef;
            Faction faction = request.Faction;
            if (kindDef != null
              && kindDef.RaceProps.Humanlike
              && !(pawnKindDefsExcluded.Contains(kindDef))
              && !(impliedRacesLoaded.Contains(kindDef.race.defName))
              && !(faction != null && (faction.def.defName == "PawnmorpherPlayerColony" || faction.def.defName == "PawnmorpherEnclave"))
              && !(weightGeneratorPaused)
              && !(PrepareCarefullyTweaks.loadedAlienRace != "none")
              && ((kindDef.race == ThingDefOf.Human && ModSettingsHandler.OverrideAllHumanPawnkinds)
              || (kindDef.race != ThingDefOf.Human && ModSettingsHandler.OverrideAllAlienPawnkinds)))
            {
                //Change this kindDef's race to the selected race temporarily.
                request.KindDef.race = WeightedRaceSelectionProcedure(kindDef, faction);
                HairFixProcedure(kindDef);
                BackstoryInjectionProcedure(kindDef, faction.def);

                justGeneratedRace = true;
                IsPawnOfPlayerFaction = faction.IsPlayer;
                //PawnkindRaceDiversification.Logger.Message("Selecting race...");
            }
        }
        public static void AfterDeterminedRace(PawnGenerationRequest request)
        {
            //Make sure that we don't completely override the race value in the pawnkind def.
            //  Set it back to what it originally was after making the pawn (should always be successful since
            //      this is the same pawn being generated).
            if (justGeneratedRace)
            {
                //Reset this kindDef's race and hairtags after generating the pawn.
                request.KindDef.race = racesLoaded.TryGetValue(pawnKindRaceDefRelations.TryGetValue(request.KindDef));
                if (prevPawnkindHairtags != null)
                    request.KindDef.hairTags = prevPawnkindHairtags;
                //Reset backstory-related lists
                if (generatedBackstoryInfo)
                {
                    request.Faction.def.backstoryFilters = prevFactionBackstoryCategoryFilters.ListFullCopyOrNull();
                    request.KindDef.backstoryCategories = prevPawnkindBackstoryCategories.ListFullCopyOrNull();
                    request.KindDef.backstoryFilters = prevPawnkindBackstoryCategoryFilters.ListFullCopyOrNull();
                    generatedBackstoryInfo = false;
                }

                justGeneratedRace = false;
                IsPawnOfPlayerFaction = false;
                //PawnkindRaceDiversification.Logger.Message("Race selected successfully.");
            }
            //Unpause the weight generator.
            weightGeneratorPaused = false;
            //Reset remembered pawnkind hair tags.
            prevPawnkindHairtags = null;
            //Reset backstory-related things.
            prevFactionBackstoryCategoryFilters = null;
            prevPawnkindBackstoryCategories = null;
            prevPawnkindBackstoryCategoryFilters = null;
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
                if (data.Value.factionWeights != null && faction != null)
                    factionWeight = data.Value.factionWeights.Find(f => f.factionDef == faction.def.defName);
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
                PawnkindWeight pawnkindWeight = null;
                if (data.Value.pawnKindWeights != null)
                    pawnkindWeight = data.Value.pawnKindWeights.Find(p => p.pawnKindDef == pawnKind.defName);
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

        private static void HairFixProcedure(PawnKindDef pawnkindDef)
        {
            //HAR does not handle hair generation for pawnkinds, therefore I will fix this myself.
            //  To revert to default behavior that HAR already does with factions, I can temporarily set
            //  the pawnkind hairtags to null in order to stop forced hair generation.
            //Pawns that are allowed to have forced hair are pawns that already do spawn with hair (will change this later).
            //  However, pawns that are not supposed to spawn with hair should not have forced pawnkind hair gen.
            if (pawnkindDef.hairTags != null)
            {
                List<string> loadedHairTags = raceHairTagData[pawnkindDef.race.defName];
                if (loadedHairTags != null
                    && loadedHairTags.Count > 0
                    && loadedHairTags[0] == "nohair")
                {
                    prevPawnkindHairtags = pawnkindDef.hairTags;
                    pawnkindDef.hairTags = null;
                }
            }
        }

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
                //Back up the previous backstory information, so that we are not overriding it afterwards
                if (factionDef != null)
                    prevFactionBackstoryCategoryFilters = factionDef.backstoryFilters.ListFullCopyOrNull();
                prevPawnkindBackstoryCategories = pawnkindDef.backstoryCategories.ListFullCopyOrNull();
                prevPawnkindBackstoryCategoryFilters = pawnkindDef.backstoryFilters.ListFullCopyOrNull();

                //Extension data
                RaceDiversificationPool raceExtensionData = racesDiversified[race];
                FactionWeight factionWeightData = null;
                PawnkindWeight pawnkindWeightData = null;
                if (factionDef != null && raceExtensionData.factionWeights != null)
                    factionWeightData = raceExtensionData.factionWeights.FirstOrFallback(w => w.factionDef == factionDef.defName);
                if (raceExtensionData.pawnKindWeights != null)
                    pawnkindWeightData = raceExtensionData.pawnKindWeights.FirstOrFallback(w => w.pawnKindDef == pawnkindDef.defName);
                

                //Handled backstory data
                List<string> backstoryCategories = new List<string>();
                List<BackstoryCategoryFilter> backstoryPawnkindFilters = new List<BackstoryCategoryFilter>();
                List<BackstoryCategoryFilter> backstoryFactionFilters = new List<BackstoryCategoryFilter>();
                bool pawnkindBackstoryOverride = false;
                bool factionBackstoryOverride = false;
                bool addedOriginalPawnkindStuff = false;
                bool addedOriginalFactionStuff = false;

                //Procedure
                if (pawnkindWeightData != null)
                {
                    if (pawnkindWeightData.backstoryCategories != null)
                        backstoryCategories.AddRange(pawnkindWeightData.backstoryCategories);
                    if (pawnkindWeightData.backstoryFilters != null)
                        backstoryPawnkindFilters.AddRange(pawnkindWeightData.backstoryFilters);
                    pawnkindBackstoryOverride = pawnkindWeightData.overrideBackstories;
                    if (!pawnkindBackstoryOverride
                    && !raceExtensionData.overrideBackstories)
                    {
                        if (pawnkindDef.backstoryCategories != null)
                            backstoryCategories.AddRange(pawnkindDef.backstoryCategories);
                        if (pawnkindDef.backstoryFilters != null)
                            backstoryPawnkindFilters.AddRange(pawnkindDef.backstoryFilters);
                        addedOriginalPawnkindStuff = true;
                    }
                }
                if (factionDef != null
                    && factionWeightData != null
                    && !pawnkindBackstoryOverride)
                {
                    if (factionWeightData.backstoryCategories != null)
                        backstoryCategories.AddRange(factionWeightData.backstoryCategories);
                    if (factionWeightData.backstoryFilters != null)
                        backstoryFactionFilters.AddRange(factionWeightData.backstoryFilters);
                    factionBackstoryOverride = factionWeightData.overrideBackstories;
                    if (!factionBackstoryOverride
                    && !raceExtensionData.overrideBackstories)
                    {
                        if (factionDef.backstoryFilters != null)
                            backstoryFactionFilters.AddRange(factionDef.backstoryFilters);
                        addedOriginalFactionStuff = true;
                    }
                }
                if ((raceExtensionData.backstoryCategories != null || raceExtensionData.backstoryFilters != null)
                    && !pawnkindBackstoryOverride
                    && !factionBackstoryOverride)
                {
                    if (raceExtensionData.backstoryCategories != null)
                        backstoryCategories.AddRange(raceExtensionData.backstoryCategories);
                    if (raceExtensionData.backstoryFilters != null)
                        backstoryFactionFilters.AddRange(raceExtensionData.backstoryFilters);
                }
                else if ((raceExtensionData.backstoryCategories == null && raceExtensionData.backstoryFilters == null)
                        && !pawnkindBackstoryOverride
                        && !factionBackstoryOverride)
                {
                    if (!addedOriginalFactionStuff && !raceExtensionData.overrideBackstories)
                    {
                        if (pawnkindDef.backstoryCategories != null)
                            backstoryCategories.AddRange(pawnkindDef.backstoryCategories);
                        if (pawnkindDef.backstoryFilters != null)
                            backstoryPawnkindFilters.AddRange(pawnkindDef.backstoryFilters);
                        addedOriginalPawnkindStuff = true;
                    }
                    if (!addedOriginalPawnkindStuff && !raceExtensionData.overrideBackstories)
                    {
                        if (factionDef.backstoryFilters != null)
                            backstoryFactionFilters.AddRange(factionDef.backstoryFilters);
                        addedOriginalFactionStuff = true;
                    }
                }

                //Assignment
                if (factionDef != null)
                    factionDef.backstoryFilters = backstoryFactionFilters;
                pawnkindDef.backstoryCategories = backstoryCategories;
                pawnkindDef.backstoryFilters = backstoryPawnkindFilters;
                generatedBackstoryInfo = true;
            }
        }
    }
}
