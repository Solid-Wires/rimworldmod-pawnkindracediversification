using HugsLib.Settings;
using RimWorld;
using RimWorld.Planet;
using System;
using System.Collections.Generic;
using UnityEngine;
using Verse;
using Verse.Sound;

namespace PawnkindRaceDiversification.Handlers
{
    internal class ModSettingsHandler
    {
        internal static SettingHandle<bool> OverrideAllHumanPawnkinds = null;
        internal static SettingHandle<bool> OverrideAllAlienPawnkinds = null;
        internal static Dictionary<string, float> setFlatWeights = new Dictionary<string, float>();
        internal static Dictionary<string, float> setLocalFlatWeights = new Dictionary<string, float>();
        internal static List<SettingHandle<float>> localHandles = new List<SettingHandle<float>>();

        internal void PrepareSettingHandles(ModSettingsPack pack, List<string> races)
        {
            OverrideAllHumanPawnkinds = pack.GetHandle("OverrideAllHumanPawnkinds", Translator.Translate("PawnkindRaceDiversity_OverrideAllHumanPawnkinds_label"), Translator.Translate("PawnkindRaceDiversity_OverrideAllHumanPawnkinds_description"), true);
            OverrideAllAlienPawnkinds = pack.GetHandle("OverrideAllAlienPawnkinds", Translator.Translate("PawnkindRaceDiversity_OverrideAllAlienPawnkinds_label"), Translator.Translate("PawnkindRaceDiversity_OverrideAllAlienPawnkinds_description"), false);

            this.MakeSettingsCategoryToggle(pack, "PawnkindRaceDiversity_FlatWeightsGlobal_Category_label", "PawnkindRaceDiversity_FlatWeights_Category_description", 
                delegate
                {
                    this.globalFlatWeightsShown = !this.globalFlatWeightsShown;
                });

            //Global weights
            foreach (string race in races)
            {
                float defaultValue = -1f;
                if (race.ToLower() == "human")
                    defaultValue = 0.35f;

                SettingHandle<float> handle = pack.GetHandle<float>("flatGenerationWeight_" + race, race, null, defaultValue, Validators.FloatRangeValidator(-1f, 1.0f), null);
                handle.VisibilityPredicate = () => this.globalFlatWeightsShown;
                handle.OnValueChanged = delegate (float val)
                {
                    string handleRace = handle.Title;
                    if (val < 0.0f) setFlatWeights.Remove(handleRace);
                    else setFlatWeights.SetOrAdd(handleRace, val);
                };
                if (handle.Value >= 0.0f) setFlatWeights.SetOrAdd(race, handle.Value);
            }

            this.MakeNonLocalSettingsHandleToggle(pack, "PawnkindRaceDiversity_FlatWeightsPerWorldGen_Category_label", "PawnkindRaceDiversity_FlatWeightsPerWorldGen_Category_description",
                delegate
                {
                    this.perWorldGenFlatWeightsShown = !this.perWorldGenFlatWeightsShown;
                });

            //Per-world weights
            foreach (string race in races)
            {
                SettingHandle<float> handle = pack.GetHandle<float>("flatGenerationWeightPerWorld_" + race, race, null, -1f, Validators.FloatRangeValidator(-1f, 1.0f), null);
                handle.Unsaved = true;
                handle.VisibilityPredicate = () => this.perWorldGenFlatWeightsShown;
                handle.OnValueChanged = delegate (float val)
                {
                    setLocalFlatWeights.SetOrAdd(handle.Title, val);
                    SettingHandle<float> lhandle = localHandles.Find(h => h.Title == handle.Title);
                    if (lhandle != null)
                    {
                        lhandle.Value = val;
                        lhandle.StringValue = val.ToString();
                    }
                };
                setLocalFlatWeights.SetOrAdd(race, handle.Value);
            }

            this.MakeLocalSettingsHandleToggle(pack, "PawnkindRaceDiversity_FlatWeightsLocal_Category_label", "PawnkindRaceDiversity_FlatWeightsLocal_Category_description",
                delegate
                {
                    this.localFlatWeightsShown = !this.localFlatWeightsShown;
                });

            //Local weights
            foreach (string race in races)
            {
                SettingHandle<float> handle = pack.GetHandle<float>("flatGenerationWeightLocal_" + race, race, null, -1f, Validators.FloatRangeValidator(-1f, 1.0f), null);
                localHandles.Add(handle);
                handle.Unsaved = true;
                handle.VisibilityPredicate = () => this.localFlatWeightsShown;
                handle.OnValueChanged = delegate (float val)
                {
                    setLocalFlatWeights.SetOrAdd(handle.Title, val);
                };
            }
            foreach (SettingHandle<float> handle in localHandles)
            {
                handle.Value = setLocalFlatWeights.TryGetValue(handle.Title);
                handle.StringValue = handle.Value.ToString();
            }
        }

        private void MakeSettingsCategoryToggle(ModSettingsPack pack, string labelId, string desc, Action buttonAction)
        {
            SettingHandle<bool> handle = pack.GetHandle<bool>(labelId, Translator.Translate(labelId), Translator.Translate(desc), false, null, null);
            handle.Unsaved = true;
            handle.CustomDrawer = delegate(Rect rect)
            {
                bool flag = Widgets.ButtonText(rect, Translator.Translate("PawnkindRaceDiversity_Category_ShowDropdownSettings"), true, true, true);
                if (flag)
                {
                    buttonAction();
                }
                return false;
            };
        }

        private void MakeNonLocalSettingsHandleToggle(ModSettingsPack pack, string labelId, string desc, Action buttonAction)
        {
            SettingHandle<bool> handle = pack.GetHandle<bool>(labelId, Translator.Translate(labelId), Translator.Translate(desc), false, null, null);
            handle.Unsaved = true;
            handle.CustomDrawer = delegate (Rect rect)
            {
                Game game = Current.Game;
                World world = (game != null) ? game.World : null;
                bool flag = world != null;
                if (flag)
                {
                    bool flag2 = Widgets.ButtonText(rect, Translator.Translate("PawnkindRaceDiversity_FlatWeightsLocal_CategoryCannotOpenInSave_label"), true, true, true);
                    if (flag2)
                    {
                        SoundStarter.PlayOneShotOnCamera(SoundDefOf.ClickReject);
                    }
                }
                else
                {
                    bool flag3 = Widgets.ButtonText(rect, Translator.Translate("PawnkindRaceDiversity_Category_ShowDropdownSettings"), true, true, true);
                    if (flag3)
                    {
                        buttonAction();
                    }
                }
                return false;
            };
        }

        private void MakeLocalSettingsHandleToggle(ModSettingsPack pack, string labelId, string desc, Action buttonAction)
        {
            SettingHandle<bool> handle = pack.GetHandle<bool>(labelId, Translator.Translate(labelId), Translator.Translate(desc), false, null, null);
            handle.Unsaved = true;
            handle.CustomDrawer = delegate (Rect rect)
            {
                Game game = Current.Game;
                World world = (game != null) ? game.World : null;
                bool flag = world == null;
                if (flag)
                {
                    bool flag2 = Widgets.ButtonText(rect, Translator.Translate("PawnkindRaceDiversity_FlatWeightsLocal_CategoryCannotOpenNoSave_label"), true, true, true);
                    if (flag2)
                    {
                        SoundStarter.PlayOneShotOnCamera(SoundDefOf.ClickReject);
                    }
                }
                else
                {
                    bool flag3 = Widgets.ButtonText(rect, Translator.Translate("PawnkindRaceDiversity_Category_ShowDropdownSettings"), true, true, true);
                    if (flag3)
                    {
                        buttonAction();
                    }
                }
                return false;
            };
        }

        public void HideAllVolatileCategories()
        {
            perWorldGenFlatWeightsShown = false;
            localFlatWeightsShown = false;
        }

        private bool globalFlatWeightsShown;

        private bool perWorldGenFlatWeightsShown;

        private bool localFlatWeightsShown;
    }
}
