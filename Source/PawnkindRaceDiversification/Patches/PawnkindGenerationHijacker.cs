using PawnkindRaceDiversification.Extensions;
using PawnkindRaceDiversification.Handlers;
using RimWorld;
using System;
using System.Collections.Generic;
using Verse;
using static PawnkindRaceDiversification.Extensions.ExtensionDatabase;

namespace PawnkindRaceDiversification.Patches
{
    public static class PawnkindGenerationHijacker
    {
        //This can be set to true to prevent pawns from being generated with race weights.
        private static bool weightGeneratorPaused = false;
        public static void PauseWeightGeneration()
        {
            weightGeneratorPaused = true;
        }

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
             *  7.) kindDef is human and settings want to override all human pawnkinds
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
              && ((kindDef.race == ThingDefOf.Human && ModSettingsHandler.OverrideAllHumanPawnkinds)
              || (kindDef.race != ThingDefOf.Human && ModSettingsHandler.OverrideAllAlienPawnkinds)))
            {
                //Change this kindDef's race to the selected race temporarily.
                request.KindDef.race = WeightedRaceSelectionProcedure(kindDef, faction);
                //PawnkindRaceDiversification.Logger.Message("Race selected: " + request.KindDef.race.label);
            }
        }
        public static void AfterDeterminedRace(PawnGenerationRequest request)
        {
            //Make sure that we don't completely override the race value in the pawnkind def.
            //  Set it back to what it originally was after making the pawn.
            //  Does not do anything if the weight generator was paused before.
            if (!weightGeneratorPaused)
            {
                PawnKindDef kindDef = request.KindDef;
                if (kindDef != null
                  && kindDef.RaceProps.Humanlike)
                    //Reset this kindDef's race after generating the pawn.
                    request.KindDef.race = racesLoaded.TryGetValue(pawnKindRaceDefRelations.TryGetValue(request.KindDef));
            }
            //Unpause the weight generator.
            weightGeneratorPaused = false;
        }

        public static ThingDef WeightedRaceSelectionProcedure(PawnKindDef pawnKind, Faction faction)
        {
            /*      Precedences for weights:
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
    }
}
