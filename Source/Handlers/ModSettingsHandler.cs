using HugsLib.Settings;
using RimWorld;
using RimWorld.Planet;
using System;
using System.Collections.Generic;
using UnityEngine;
using Verse;
using Verse.Sound;
using PawnkindRaceDiversification.UI;

namespace PawnkindRaceDiversification.Handlers
{
    internal class ModSettingsHandler
    {
        internal static SettingHandle<bool> DebugMode = null;
        internal static SettingHandle<bool> OverrideAllHumanPawnkinds = null;
        internal static SettingHandle<bool> OverrideAllAlienPawnkinds = null;
        internal static bool OverrideAllAlienPawnkindsFromStartingPawns = false;
        internal static SettingHandle<bool> OverridePawnsWithInconsistentAges = null;
        internal static Dictionary<string, float> setFlatWeights = new Dictionary<string, float>();
        internal static Dictionary<string, float> setLocalFlatWeights = new Dictionary<string, float>();
        internal static Dictionary<string, float> setLocalWorldWeights = new Dictionary<string, float>();
        internal static Dictionary<string, float> setLocalStartingPawnWeights = new Dictionary<string, float>();
        internal static List<SettingHandle<float>> allHandleReferences = new List<SettingHandle<float>>();
        internal static List<string> evaluatedRaces = new List<string>();
        internal const string showSettingsValid = "PawnkindRaceDiversity_Category_ShowSettings";

        internal void PrepareSettingHandles(ModSettingsPack pack, List<string> races)
        {
            evaluatedRaces = races;
            DebugMode = pack.GetHandle("DebugMode", Translator.Translate("PawnkindRaceDiversity_DebugMode_label"), Translator.Translate("PawnkindRaceDiversity_DebugMode_description"), false);
            OverrideAllHumanPawnkinds = pack.GetHandle("OverrideAllHumanPawnkinds", Translator.Translate("PawnkindRaceDiversity_OverrideAllHumanPawnkinds_label"), Translator.Translate("PawnkindRaceDiversity_OverrideAllHumanPawnkinds_description"), true);
            OverrideAllAlienPawnkinds = pack.GetHandle("OverrideAllAlienPawnkinds", Translator.Translate("PawnkindRaceDiversity_OverrideAllAlienPawnkinds_label"), Translator.Translate("PawnkindRaceDiversity_OverrideAllAlienPawnkinds_description"), false);
            OverridePawnsWithInconsistentAges = pack.GetHandle("OverridePawnsWithInconsistentAges", Translator.Translate("PawnkindRaceDiversity_OverridePawnsWithInconsistentAges_label"), Translator.Translate("PawnkindRaceDiversity_OverridePawnsWithInconsistentAges_description"), false);

            //Global weights
            ConstructRaceAdjustmentHandles(pack, HandleContext.GENERAL);
            //Per-world weights
            ConstructRaceAdjustmentHandles(pack, HandleContext.WORLD);
            //Starting pawn weights
            ConstructRaceAdjustmentHandles(pack, HandleContext.STARTING);
            //Local weights
            ConstructRaceAdjustmentHandles(pack, HandleContext.LOCAL);

            //Settings category buttons construction
            //Flat weights
            this.SettingsButtonCategoryConstructor(pack,
                "PawnkindRaceDiversity_WeightWindowTitle_FlatWeights", 
                showSettingsValid,
                "PawnkindRaceDiversity_FlatWeights_Category_description", 
                delegate
                {
                    Find.WindowStack.Add(new WeightSettingsWindow(HandleContext.GENERAL));
                });
            //Local weights
            this.SettingsButtonCategoryConstructor(pack,
                "PawnkindRaceDiversity_WeightWindowTitle_FlatWeightsLocal",
                showSettingsValid,
                "PawnkindRaceDiversity_FlatWeightsLocal_Category_description",
                delegate
                {
                    Find.WindowStack.Add(new WeightSettingsWindow(HandleContext.LOCAL));
                },
                delegate
                {
                    //Invalid if not in a world
                    return !isInWorld();
                });
        }

        //Constructs a button in the mod settings that handles custom actions.
        //  Specifically, these are made in order to create special windows.
        private void SettingsButtonCategoryConstructor(ModSettingsPack pack, string labelID, string buttonLabel, string desc,
                                                Action buttonAction, Func<bool> invalidCondition = null)
        {
            SettingHandle<bool> handle = pack.GetHandle(labelID, Translator.Translate(labelID), Translator.Translate(desc), false, null, null);
            handle.Unsaved = true;
            handle.CustomDrawer = delegate (Rect rect)
            {
                bool invalid = false;
                if (invalidCondition != null)
                    invalid = invalidCondition();
                if (invalid)
                    GUI.color = new Color(1f, 0.3f, 0.35f);
                bool validButtonRes = Widgets.ButtonText(rect, Translator.Translate(buttonLabel), true, true, true);
                if (validButtonRes)
                {
                    if (!invalid)
                        buttonAction();
                    else
                        SoundDefOf.ClickReject.PlayOneShotOnCamera(null);
                }
                   
                GUI.color = Color.white;
                return false;
            };
        }

        //Make race adjustment settings handles. Are never visible, but are adjusted through other means.
        private void ConstructRaceAdjustmentHandles(ModSettingsPack pack, HandleContext context)
        {
            foreach (string race in evaluatedRaces)
            {
                //ID of handle
                string weightID = GetRaceSettingWeightID(context, race);
                
                //Default value
                float defaultValue = -1f;
                if (race.ToLower() == "human" && context == HandleContext.GENERAL)
                    defaultValue = 0.35f;

                //Handle configuration
                SettingHandle<float> handle = pack.GetHandle<float>(weightID, race, null, defaultValue, Validators.FloatRangeValidator(-1f, 1.0f), null);
                handle.Unsaved = (context == HandleContext.WORLD || context == HandleContext.STARTING || context == HandleContext.LOCAL);
                handle.NeverVisible = true; //Never visible because it is handled by custom GUI instead
                handle.ValueChanged += delegate (SettingHandle newHandle)
                {
                    float val = 0.0f;
                    float.TryParse(newHandle.StringValue, out val);

                    switch (context)
                    {
                        case HandleContext.GENERAL:
                            string handleRace = newHandle.Title;
                            if (val < 0.0f) setFlatWeights.Remove(handleRace);
                            else setFlatWeights.SetOrAdd(handleRace, val);
                            break;
                        case HandleContext.WORLD:
                            setLocalWorldWeights.SetOrAdd(newHandle.Title, val);
                            break;
                        case HandleContext.STARTING:
                            setLocalStartingPawnWeights.SetOrAdd(newHandle.Title, val);
                            break;
                        case HandleContext.LOCAL:
                            setLocalFlatWeights.SetOrAdd(newHandle.Title, val);
                            break;
                    }
                };

                //List constructions
                if (handle.Value >= 0.0f && context == HandleContext.GENERAL)
                    setFlatWeights.SetOrAdd(race, handle.Value);
                else if (context == HandleContext.WORLD)
                    setLocalWorldWeights.SetOrAdd(race, handle.Value);
                else if (context == HandleContext.STARTING)
                    setLocalStartingPawnWeights.SetOrAdd(race, handle.Value);
                else if (context == HandleContext.LOCAL)
                {
                    foreach (SettingHandle<float> lh in allHandleReferences.FindAll(h => WhatContextIsID(h.Name) == HandleContext.LOCAL))
                    {
                        lh.Value = setLocalFlatWeights.TryGetValue(lh.Title);
                        lh.StringValue = lh.Value.ToString();
                    }
                }
                allHandleReferences.Add(handle);
            }
        }

        internal static string GetRaceSettingWeightID(HandleContext context, string race)
        {
            switch (context)
            {
                case HandleContext.GENERAL:
                    return "flatGenerationWeight_" + race;
                case HandleContext.WORLD:
                    return "flatGenerationWeightPerWorld_" + race;
                case HandleContext.STARTING:
                    return "flatGenerationWeightStartingPawns_" + race;
                case HandleContext.LOCAL:
                    return "flatGenerationWeightLocal_" + race;
            }
            return null;
        }
        internal static HandleContext WhatContextIsID(string id)
        {
            if (id.StartsWith("flatGenerationWeightPerWorld"))
                return HandleContext.WORLD;
            else if (id.StartsWith("flatGenerationWeightStartingPawns"))
                return HandleContext.STARTING;
            else if (id.StartsWith("flatGenerationWeightLocal"))
                return HandleContext.LOCAL;
            else if (id.StartsWith("flatGenerationWeight"))
                return HandleContext.GENERAL;
            return HandleContext.NONE;
        }

        internal static void SyncWorldWeightsIntoLocalWeights()
        {
            foreach (KeyValuePair<string, float> wv in setLocalWorldWeights)
            {
                setLocalFlatWeights.SetOrAdd(wv.Key, wv.Value);
                SettingHandle<float> lhandle = allHandleReferences.Find(h => h.Title == wv.Key && WhatContextIsID(h.Name) == HandleContext.LOCAL);
                if (lhandle != null)
                {
                    lhandle.Value = wv.Value;
                    lhandle.StringValue = wv.Value.ToString();
                }
            }
        }

        internal static void UpdateHandleReferencesInAllReferences(ref Dictionary<string, float> handle, HandleContext context)
        {
            foreach (SettingHandle<float> handleInAll in allHandleReferences.FindAll(h => WhatContextIsID(h.Name) == context))
            {
                float weight = -1f;
                bool successful = handle.TryGetValue(handleInAll.Title, out weight);
                if (successful)
                {
                    handleInAll.Value = weight;
                    handleInAll.StringValue = weight.ToString();
                }
            }
        }
        //Races are missing when they are newly loaded.
        //  Therefore, this method exists to fix any handles that are missing races.
        internal static void ResolveMissingRaces(ref Dictionary<string, float> handle, float placedWeight)
        {
            Dictionary<string, float> hcopy = new Dictionary<string, float>(handle);

            //Add new races to handler
            foreach (string evalRace in evaluatedRaces)
                if (!handle.ContainsKey(evalRace))
                    handle.SetOrAdd(evalRace, placedWeight);

            //Remove missing races from handler
            foreach (string key in hcopy.Keys)
                if (!evaluatedRaces.Contains(key))
                    handle.Remove(key);
        }

        internal static void ResetHandle(ref Dictionary<string, float> handle, HandleContext context)
        {
            Dictionary<string, float> hcopy = new Dictionary<string, float>(handle);
            foreach (string key in hcopy.Keys)
                handle[key] = -1.0f;
            UpdateHandleReferencesInAllReferences(ref handle, context);
        }

        private bool isInWorld()
        {
            Game game = Current.Game;
            World world = (game != null) ? game.World : null;
            return (world != null);
        }
    }
}
