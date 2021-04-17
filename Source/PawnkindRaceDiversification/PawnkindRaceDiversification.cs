using System;
using System.Reflection;
using RimWorld;
using HarmonyLib;
using Verse;
using HugsLib;
using PawnkindRaceDiversification.Handlers;
using AlienRace;
using HugsLib.Utils;
using System.Linq;
using System.Collections.Generic;
using PawnkindRaceDiversification.Extensions;
using UnityEngine.SceneManagement;

namespace PawnkindRaceDiversification
{
    public class PawnkindRaceDiversification : ModBase
    {
        internal static PawnkindRaceDiversification Instance { get; private set; }
        internal static Harmony harmony => new Harmony("SEW_PRD_Harmony");
        internal static ModSettingsWorldStorage worldSettings = null;
        internal ModSettingsHandler SettingsHandler { get; private set; }

        public const bool DEBUG_MODE = false;

        public override string ModIdentifier => "PawnkindRaceDiversification";

        protected override bool HarmonyAutoPatch => false;

        private ModLogger GetLogger => base.Logger;
        internal static ModLogger Logger => Instance.GetLogger;
        internal static void LogValues(params object[] values)
        {
            if (values.Length > 0)
            {
                string ind = "\n\t";
                string msg = "";
                msg += "Value output: " + ind;
                foreach (object v in values)
                {
                    try
                    {
                        msg += v.GetType().Name + ": " + v.ToString() + ind;
                    }
                    catch (Exception)
                    {
                        msg += "Value errored" + ind;
                    }
                }
                Logger.Message(msg);
            }
            else
            {
                Logger.Warning("No values to output.");
            }
        }

        private PawnkindRaceDiversification() => Instance = this;

        public override void DefsLoaded()
        {
            base.DefsLoaded();
            if (ModIsActive)
            {
                //Finds def of all races currently loaded (courtesy goes to DubWise)
                List<string> raceNames = new List<string>();
                List<ThingDef_AlienRace> alienRaceDefs = (from x in DefDatabase<ThingDef_AlienRace>.AllDefs
                                                          where x.race != null
                                                          select x).ToList();
                List<PawnKindDef> kindDefs = (from x in DefDatabase<PawnKindDef>.AllDefs
                                              select x).ToList();
                //Search through all alien race defs
                foreach (ThingDef_AlienRace def in alienRaceDefs)
                {
                    //Add this race to the database
                    raceNames.Add(def.defName);
                    ExtensionDatabase.racesLoaded.Add(def.defName, def);

                    //Get all values from extensions
                    RaceDiversificationPool ext = def.GetModExtension<RaceDiversificationPool>();
                    if (ext != null)
                    {
                        //You can exclude a race from being modified in the settings by making the flat generation weight negative (-1)
                        if (ext.flatGenerationWeight < 0.0f) raceNames.Remove(def.defName);
                        else ExtensionDatabase.racesDiversified.Add(def.defName, ext);

                        //Logger.Message("Def loaded: " + def.defName + ", extension values logged after");
                        //LogValues(ext.factionWeights[0].faction.defName, ext.pawnKindWeights[0].pawnkind.defName, ext.flatGenerationWeight);
                    }
                }
                //Look through all existing pawnkind defs
                foreach (PawnKindDef def in kindDefs)
                {
                    ExtensionDatabase.pawnKindRaceDefRelations.Add(def, def.race.defName);
                    if (def.GetModExtension<RaceRandomizationExcluded>() != null)
                    {
                        ExtensionDatabase.pawnKindDefsExcluded.Add(def);
                    }
                }

                SettingsHandler = new ModSettingsHandler();
                SettingsHandler.PrepareSettingHandles(Instance.Settings, raceNames);
            }
        }

        public override void WorldLoaded()
        {
            base.WorldLoaded();
            worldSettings = UtilityWorldObjectManager.GetUtilityWorldObject<ModSettingsWorldStorage>();
        }
        public override void SceneLoaded(Scene scene)
        {
            base.SceneLoaded(scene);
            SettingsHandler.HideAllVolatileCategories();
        }
    }
}
