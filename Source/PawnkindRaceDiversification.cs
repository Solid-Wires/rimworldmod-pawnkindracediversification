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
using PawnkindRaceDiversification.UI;
using static PawnkindRaceDiversification.Data.GeneralLoadingDatabase;
using static PawnkindRaceDiversification.Extensions.ExtensionDatabase;
using PawnkindRaceDiversification.Data;

namespace PawnkindRaceDiversification
{
    public class PawnkindRaceDiversification : ModBase
    {
        internal static PawnkindRaceDiversification Instance { get; private set; }
        internal static int versionID = 31;
        internal static Harmony harmony => new Harmony("SEW_PRD_Harmony");
        internal static ModSettingsWorldStorage worldSettings = null;
        internal ModSettingsHandler SettingsHandler { get; private set; }
        internal static List<SeekedMod> activeSeekedMods = new List<SeekedMod>();
        internal enum SeekedMod
        {
            NONE,
            PAWNMORPHER,
            ALTERED_CARBON,
            PREPARE_CAREFULLY,
            ANDROIDS,
            CHARACTER_EDITOR
        }
        private static Dictionary<string, SeekedMod> seekedModAssemblies = new Dictionary<string, SeekedMod>()
        {
            { "Pawnmorph", SeekedMod.PAWNMORPHER },
            { "AlteredCarbon", SeekedMod.ALTERED_CARBON},
            { "EdBPrepareCarefully", SeekedMod.PREPARE_CAREFULLY},
            { "Androids", SeekedMod.ANDROIDS},
            { "CharacterEditor", SeekedMod.CHARACTER_EDITOR }
        };
        internal static Dictionary<SeekedMod, Assembly> referencedModAssemblies = new Dictionary<SeekedMod, Assembly>();

        public static bool IsDebugModeInSettingsActive()
        {
            return ModSettingsHandler.DebugMode.Value;
        }

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

        public override void Initialize()
        {
            base.Initialize();

            //Print the version of PRD
            Logger.Message("Initialized PRD version " + versionID.ToString());

            //Find all active mods that this mod seeks.
            List<Assembly> mods = HugsLibUtility.GetAllActiveAssemblies().ToList();
            foreach (Assembly m in mods)
            {
                //Logger.Message(m.GetName().Name);
                SeekedMod modFound = SeekedMod.NONE;
                bool successful = seekedModAssemblies.TryGetValue(m.GetName().Name, out modFound);
                if (successful)
                {
                    activeSeekedMods.Add(modFound);
                    referencedModAssemblies.Add(modFound, m);
                }
            }
            Patches.HarmonyPatches.PostInitPatches();
        }

        public override void DefsLoaded()
        {
            base.DefsLoaded();
            if (ModIsActive)
            {
                //Finds def of all races currently loaded (courtesy goes to DubWise)
                //  Also selects race settings to cherry pick a few things off of it
                //  and looks through all loaded pawnkind defs to reassign its defaults after modifying them at runtime.
                List<string> raceNames = new List<string>();
                List<ThingDef_AlienRace> alienRaceDefs = (from x in DefDatabase<ThingDef_AlienRace>.AllDefs
                                                          where x.race != null
                                                          select x).ToList();
                List<RaceSettings> raceSettingsDefs = (from x in DefDatabase<RaceSettings>.AllDefs
                                                            select x).ToList();
                List<PawnKindDef> kindDefs = (from x in DefDatabase<PawnKindDef>.AllDefs
                                              select x).ToList();
                //Search through all alien race defs
                foreach (ThingDef_AlienRace def in alienRaceDefs)
                {
                    //Know the file name of the def, this helps a lot sometimes
                    string fileName = "";
                    if (def.fileName != null)
                    {
                        if (def.fileName.Contains('.'))
                            fileName = def.fileName.Substring(0, def.fileName.IndexOf('.'));
                        else
                            fileName = def.fileName;
                    }

                    //Pawnmorpher compatibility
                    if (activeSeekedMods.Contains(SeekedMod.PAWNMORPHER))
                    {
                        //Implied defs are automatically skipped...
                        //  Y'know, it would be a lot of work to patch ALL that author's implied defs!
                        if (fileName == "ImpliedDefs"
                         || fileName == "Cobra_Hybrid")
                        {
                            impliedRacesLoaded.Add(def.defName);
                            continue;
                        }
                    }

                    //Add this race to the databases
                    raceNames.Add(def.defName);
                    racesLoaded.Add(def.defName, def);

                    //Style settings (made obsolete thanks to HAR)
                    /*
                    foreach (KeyValuePair<Type, StyleSettings> style in def.alienRace?.styleSettings)
                        GeneralLoadingDatabase.AddOrInsertStyle(def.defName, style.Key, style.Value);
                    */

                    //Get all values from extensions
                    RaceDiversificationPool ext = def.GetModExtension<RaceDiversificationPool>();
                    if (ext != null)
                    {
                        //You can exclude a race from being modified in the settings by making the flat generation weight negative (-1)
                        if (ext.flatGenerationWeight < 0.0f) raceNames.Remove(def.defName);
                        else racesDiversified.Add(def.defName, ext);

                        //Logger.Message("Def loaded: " + def.defName + ", extension values logged after");
                        //LogValues(ext.factionWeights[0].faction.defName, ext.pawnKindWeights[0].pawnkind.defName, ext.flatGenerationWeight);
                    }
                }
                //Remove irrelevant race settings
                foreach (RaceSettings s in raceSettingsDefs)
                {
                    //I was a complete buffoon for trying to remove these. Just made their chances 0% instead.
                    List<PawnKindEntry> slaveKindEntries = (from w in s.pawnKindSettings.alienslavekinds
                                                          where w.chance > 0
                                                          select w).ToList();
                    List<PawnKindEntry> refugeeKindEntries = (from w in s.pawnKindSettings.alienrefugeekinds
                                                            where w.chance > 0
                                                            select w).ToList();
                    foreach (PawnKindEntry slaveKind in slaveKindEntries)
                        slaveKind.chance = 0.0f;
                    foreach (PawnKindEntry refugeeKind in refugeeKindEntries)
                        refugeeKind.chance = 0.0f;

                    List<FactionPawnKindEntry> startingColonistEntries = (from w in s.pawnKindSettings.startingColonists
                                                            where w.factionDefs.Count > 0
                                                            select w).ToList();
                    List<FactionPawnKindEntry> wandererEntries = (from w in s.pawnKindSettings.alienwandererkinds
                                                          where w.factionDefs.Count > 0
                                                         select w).ToList();
                    /*  So I didn't completely remove these race settings for two reasons:
                     *      1.) Some race mods want these so that they have different pawnkind varieties for their
                     *          specific faction races.
                     *      2.) It would be destructive and barbaric to assume that ALL race mods bother the player
                     *          colony factions.
                     *  Therefore, all this does is remove the player factions from race settings that try to
                     *  modify it. Settings without factions specified don't do anything (therefore, this is a
                     *  safe procedure).
                     * */
                    foreach (FactionPawnKindEntry e in startingColonistEntries)
                        e.factionDefs.RemoveAll(f => (f.defName == "PlayerColony" || f.defName == "PlayerTribe"));
                    foreach (FactionPawnKindEntry e in wandererEntries)
                        e.factionDefs.RemoveAll(f => (f.defName == "PlayerColony" || f.defName == "PlayerTribe"));
                }
                //Look through all existing pawnkind defs
                foreach (PawnKindDef def in kindDefs)
                {
                    pawnKindRaceDefRelations.Add(def.defName, def.race.defName);
                    if (def.GetModExtension<RaceRandomizationExcluded>() != null)
                        pawnKindDefsExcluded.Add(def.defName);
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
    }
}
