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
        internal static SettingHandle<bool> OverridePawnsWithInconsistentAges = null;
        internal static Dictionary<string, float> setFlatWeights = new Dictionary<string, float>();
        internal static Dictionary<string, float> setLocalFlatWeights = new Dictionary<string, float>();
        internal static Dictionary<string, float> setLocalWorldWeights = new Dictionary<string, float>();
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
            ConstructRaceAdjustmentHandles(pack, HandleContext.GLOBALS);
            //Per-world weights
            ConstructRaceAdjustmentHandles(pack, HandleContext.WORLD);
            //Local weights
            ConstructRaceAdjustmentHandles(pack, HandleContext.LOCALS);

            //Settings category buttons construction
            //Flat weights
            this.SettingsButtonCategoryConstructor(pack,
                "PawnkindRaceDiversity_WeightWindowTitleGlobal", 
                showSettingsValid,
                "PawnkindRaceDiversity_FlatWeights_Category_description", 
                delegate
                {
                    Find.WindowStack.Add(new WeightSettingsWindow(HandleContext.GLOBALS));
                });
            //Per-world weights
            this.SettingsButtonCategoryConstructor(pack,
                "PawnkindRaceDiversity_WeightWindowTitleWorld",
                showSettingsValid,
                "PawnkindRaceDiversity_FlatWeightsPerWorldGen_Category_description",
                delegate
                {
                    Find.WindowStack.Add(new WeightSettingsWindow(HandleContext.WORLD));
                },
                delegate
                {
                    //Invalid if in a world
                    return isInWorld();
                });
            //Local weights
            this.SettingsButtonCategoryConstructor(pack,
                "PawnkindRaceDiversity_WeightWindowTitleLocal",
                showSettingsValid,
                "PawnkindRaceDiversity_FlatWeightsLocal_Category_description",
                delegate
                {
                    Find.WindowStack.Add(new WeightSettingsWindow(HandleContext.LOCALS));
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
            //If invalid, hides the widget
            if (invalidCondition != null)
                handle.VisibilityPredicate = () => !invalidCondition();
            handle.CustomDrawer = delegate (Rect rect)
            {
                bool validButtonRes = Widgets.ButtonText(rect, Translator.Translate(buttonLabel), true, true, true);
                if (validButtonRes)
                    buttonAction();
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
                if (race.ToLower() == "human" && context == HandleContext.GLOBALS)
                    defaultValue = 0.35f;

                //Handle configuration
                SettingHandle<float> handle = pack.GetHandle<float>(weightID, race, null, defaultValue, Validators.FloatRangeValidator(-1f, 1.0f), null);
                handle.Unsaved = (context == HandleContext.WORLD || context == HandleContext.LOCALS);
                handle.NeverVisible = true; //Never visible because it is handled by custom GUI instead
                handle.OnValueChanged = delegate (float val)
                {
                    switch(context)
                    {
                        case HandleContext.GLOBALS:
                            string handleRace = handle.Title;
                            if (val < 0.0f) setFlatWeights.Remove(handleRace);
                            else setFlatWeights.SetOrAdd(handleRace, val);
                            break;
                        case HandleContext.WORLD:
                            setLocalWorldWeights.SetOrAdd(handle.Title, val);
                            SyncWorldWeightsIntoLocalWeights();
                            break;
                        case HandleContext.LOCALS:
                            setLocalFlatWeights.SetOrAdd(handle.Title, val);
                            break;
                    }
                };

                //List constructions
                if (handle.Value >= 0.0f && context == HandleContext.GLOBALS)
                    setFlatWeights.SetOrAdd(race, handle.Value);
                else if (context == HandleContext.WORLD)
                    setLocalWorldWeights.SetOrAdd(race, handle.Value);
                else if (context == HandleContext.LOCALS)
                {
                    foreach (SettingHandle<float> lh in allHandleReferences.FindAll(h => WhatContextIsID(h.Name) == HandleContext.LOCALS))
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
                case HandleContext.GLOBALS:
                    return "flatGenerationWeight_" + race;
                case HandleContext.WORLD:
                    return "flatGenerationWeightPerWorld_" + race;
                case HandleContext.LOCALS:
                    return "flatGenerationWeightLocal_" + race;
            }
            return null;
        }
        internal static HandleContext WhatContextIsID(string id)
        {
            if (id.StartsWith("flatGenerationWeightPerWorld"))
                return HandleContext.WORLD;
            else if (id.StartsWith("flatGenerationWeightLocal"))
                return HandleContext.LOCALS;
            else if (id.StartsWith("flatGenerationWeight"))
                return HandleContext.GLOBALS;
            return HandleContext.NONE;
        }

        internal static void SyncWorldWeightsIntoLocalWeights()
        {
            foreach (KeyValuePair<string, float> wv in setLocalWorldWeights)
            {
                setLocalFlatWeights.SetOrAdd(wv.Key, wv.Value);
                SettingHandle<float> lhandle = allHandleReferences.Find(h => h.Title == wv.Key && WhatContextIsID(h.Name) == HandleContext.LOCALS);
                if (lhandle != null)
                {
                    lhandle.Value = wv.Value;
                    lhandle.StringValue = wv.Value.ToString();
                }
            }
        }

        private bool isInWorld()
        {
            Game game = Current.Game;
            World world = (game != null) ? game.World : null;
            return (world != null);
        }
    }
}
