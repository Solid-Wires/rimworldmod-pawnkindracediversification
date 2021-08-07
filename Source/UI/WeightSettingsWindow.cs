using System.Collections.Generic;
using UnityEngine;
using Verse;
using PawnkindRaceDiversification.Handlers;
using HugsLib.Settings;
using PawnkindRaceDiversification.Extensions;
using static PawnkindRaceDiversification.Extensions.ExtensionDatabase;

namespace PawnkindRaceDiversification.UI
{
    public class WeightSettingsWindow : Window
    {
        public HandleContext windowContext = HandleContext.NONE;
        private string windowTitle = "Weight Settings Window";
        private string windowDesc = "No description";
        public override Vector2 InitialSize => new Vector2(760f, 730f);

        private Vector2 scrollPosition = new Vector2(0f, 0f);
        private Dictionary<string, float> spawnChancesVisual = new Dictionary<string, float>();
        private Dictionary<string, bool> prevAdjustedRaces = new Dictionary<string, bool>();
        private Dictionary<string, string> inputBoxRaceValue = new Dictionary<string, string>();
        public List<SettingHandle<float>> windowHandles = new List<SettingHandle<float>>();
        private bool quickAdjust = false;
        private bool quickAdjustInitializedFlag = false;

        public WeightSettingsWindow(HandleContext windowContext)
        {
            this.windowContext = windowContext;
            this.absorbInputAroundWindow = true;
            this.closeOnClickedOutside = false;
            this.onlyOneOfTypeAllowed = true;
            switch(windowContext)
            {
                case HandleContext.GENERAL:
                    windowTitle = "PawnkindRaceDiversity_WeightWindowTitle_FlatWeights";
                    windowDesc = "PawnkindRaceDiversity_WeightWindowDesc_FlatWeights";
                    break;
                case HandleContext.WORLD:
                    windowTitle = "PawnkindRaceDiversity_WeightWindowTitle_FlatWeightsPerWorldGen";
                    windowDesc = "PawnkindRaceDiversity_WeightWindowDesc_FlatWeightsPerWorldGen";
                    break;
                case HandleContext.STARTING:
                    windowTitle = "PawnkindRaceDiversity_WeightWindowTitle_FlatWeightsStartingPawns";
                    windowDesc = "PawnkindRaceDiversity_WeightWindowDesc_FlatWeightsStartingPawns";
                    break;
                case HandleContext.LOCAL:
                    windowTitle = "PawnkindRaceDiversity_WeightWindowTitle_FlatWeightsLocal";
                    windowDesc = "PawnkindRaceDiversity_WeightWindowDesc_FlatWeightsLocal";
                    break;
            }
            windowHandles = ModSettingsHandler.allHandleReferences;
            EvaluateWhichDefsAreAdjusted();
        }

        public override void DoWindowContents(Rect inRect)
        {
            //Default text settings
            GameFont prevFontSize = Text.Font;
            TextAnchor prevAnchor = Text.Anchor;

            //Window title
            windowTitleRect = new Rect(new Vector2(
                inRect.x, inRect.y),
                new Vector2(
                inRect.width, 40f)
                );
            Text.Font = GameFont.Medium;
            Text.Anchor = TextAnchor.MiddleCenter;
            Widgets.Label(windowTitleRect, Translator.Translate(windowTitle));
            float windowTitleElementsYOffset = 28f;
            //Window description
            windowDescRect = new Rect(new Vector2(
                inRect.x, inRect.y + windowTitleElementsYOffset),
                new Vector2(
                inRect.width, 40f)
                );
            Text.Font = GameFont.Small;
            Text.Anchor = TextAnchor.MiddleCenter;
            Widgets.Label(windowDescRect, Translator.Translate(windowDesc));
            Text.Font = prevFontSize;
            Text.Anchor = prevAnchor;

            Rect listingStandardRectOuter 
                = new Rect(new Vector2(inRect.x + 5f, inRect.y + 40f + windowTitleElementsYOffset), 
                            new Vector2(inRect.width - 10f, inRect.height - 120f - windowTitleElementsYOffset));
            Rect listingStandardRectInner
                = new Rect(new Vector2(inRect.x + 10f, inRect.y + 45f + windowTitleElementsYOffset),
                            new Vector2(inRect.width - 20f, inRect.height - 130f - windowTitleElementsYOffset));
            Widgets.DrawMenuSection(listingStandardRectOuter);
            //Column title delimiter
            float tableStart = listingStandardRectOuter.x + 2f;
            float YrowStart = listingStandardRectOuter.y + 2f;
            float YrowEnd = listingStandardRectOuter.height - 2f;
            Widgets.DrawLineHorizontal(tableStart, listingStandardRectOuter.y + 20f, listingStandardRectOuter.width - 24f);
            //Name column delimiter
            float columnStart1 = tableStart + 180f;
            Widgets.DrawLineVertical(columnStart1, YrowStart, YrowEnd);
            //Flat weight column delimiter
            float columnStart2 = columnStart1 + 78f;
            Widgets.DrawLineVertical(columnStart2, YrowStart, YrowEnd);
            //Spawn chance column delimiter
            float columnStart3 = columnStart2 + 118f;
            Widgets.DrawLineVertical(columnStart3, YrowStart, YrowEnd);
            //Def adjusted delimiter
            float columnStartLast = columnStart3 + 118f;
            Widgets.DrawLineVertical(columnStartLast, YrowStart, YrowEnd);

            float tableEnd = listingStandardRectOuter.width;

            float elementSize = 36f;
            UnityEngine.GUI.BeginGroup(listingStandardRectInner);
            Widgets.BeginScrollView(new Rect(0f, 20f, listingStandardRectInner.width - 2f, listingStandardRectInner.height - 2f), ref scrollPosition, 
                new Rect(listingStandardRectInner.x, listingStandardRectInner.y - (elementSize * 1.6f),
                        listingStandardRectInner.width, 
                        (ModSettingsHandler.evaluatedRaces.Count * elementSize) + (elementSize * (1.6f / 4f) - (windowTitleElementsYOffset / 2f))), 
                    true);
            int element = 0;
            float yPos = 0f;
            CalculateSpawnChances();
            foreach (string race in ModSettingsHandler.evaluatedRaces)
            {
                yPos = (element * elementSize) + (windowTitleElementsYOffset / 2f);
                element++;
                Rect innerContentRect = new Rect(listingStandardRectInner.x, yPos, 180f, elementSize);
                Text.Anchor = TextAnchor.MiddleLeft;
                //Race
                Widgets.Label(innerContentRect, race);
                Text.Anchor = TextAnchor.MiddleCenter;
                //Weight
                innerContentRect = new Rect(columnStart1, yPos, 78f, elementSize);
                if (!quickAdjust)
                    Widgets.Label(innerContentRect, GrabWeightReference(race, windowContext).ToString("0.0##"));
                else
                {
                    if (quickAdjust && !quickAdjustInitializedFlag)
                        inputBoxRaceValue.Add(race, GrabWeightReference(race, windowContext).ToString("0.0##"));
                    innerContentRect = new Rect(columnStart1 + 2f, yPos + 6f, 78f - 4f, elementSize / 2f + 6f);
                    float value = 0.0f;
                    string inp = Widgets.TextField(innerContentRect, inputBoxRaceValue[race]);
                    inputBoxRaceValue[race] = inp;
                    bool valid = float.TryParse(inputBoxRaceValue[race], out value);
                    if (valid && value != GrabWeightReference(race, windowContext))
                        SetWeightReference(race, value);
                }
                //Spawn Chance
                innerContentRect = new Rect(columnStart2 + 2f, yPos, 118f, elementSize);
                Widgets.Label(innerContentRect, spawnChancesVisual[race].ToStringPercent());
                //Prev Adjusted
                innerContentRect = new Rect(columnStart3 + 2f, yPos, 114f, elementSize);
                if (!(race.ToLower() == "human" && windowContext == HandleContext.GENERAL))
                {
                    bool checkboxTmp = prevAdjustedRaces[race];
                    Widgets.Checkbox(new Vector2(innerContentRect.x + (118f / 3f) + 5f, innerContentRect.y + 5f), ref checkboxTmp, 24f, false, true);
                    prevAdjustedRaces[race] = checkboxTmp;
                }
                //Actions
                innerContentRect = new Rect(columnStartLast + 4f, yPos, tableEnd - columnStartLast - 28f, elementSize);
                bool showAdjustment = Widgets.ButtonText(innerContentRect, Translator.Translate("PawnkindRaceDiversity_Category_ShowAdjustments"));
                if (showAdjustment)
                    Find.WindowStack.Add(new WeightAdjustmentWindow(this, race));
            }
            Text.Anchor = prevAnchor;
            Widgets.EndScrollView();
            UnityEngine.GUI.EndGroup();

            //Column labels
            Text.Anchor = TextAnchor.MiddleCenter;
            Rect columnLabel = new Rect(tableStart + 2f, YrowStart,
                columnStart1 - 14f, 18f);
            Widgets.Label(columnLabel, Translator.Translate("PawnkindRaceDiversity_WeightSettingColumnLabel_RaceDef"));
            HighlightableInfoMouseover(columnLabel, "PawnkindRaceDiversity_WeightSettingColumnTooltip_RaceDef");
            columnLabel = new Rect(columnStart1 + 4f, YrowStart,
                72f, 18f);
            Widgets.Label(columnLabel, Translator.Translate("PawnkindRaceDiversity_WeightSettingColumnLabel_FlatWeight"));
            HighlightableInfoMouseover(columnLabel, "PawnkindRaceDiversity_WeightSettingColumnTooltip_FlatWeight");
            columnLabel = new Rect(columnStart2 + 4f, YrowStart,
                112f, 18f);
            Widgets.Label(columnLabel, Translator.Translate("PawnkindRaceDiversity_WeightSettingColumnLabel_SpawnChance"));
            HighlightableInfoMouseover(columnLabel, "PawnkindRaceDiversity_WeightSettingColumnTooltip_SpawnChance");
            columnLabel = new Rect(columnStart3 + 4f, YrowStart,
                112f, 18f);
            Widgets.Label(columnLabel, Translator.Translate("PawnkindRaceDiversity_WeightSettingColumnLabel_AllowPrevAdjusted"));
            HighlightableInfoMouseover(columnLabel, "PawnkindRaceDiversity_WeightSettingColumnTooltip_AllowPrevAdjusted");
            columnLabel = new Rect(columnStartLast + 4f, YrowStart,
                listingStandardRectOuter.x + (tableEnd - columnStartLast - 28f), 18f);
            Widgets.Label(columnLabel, Translator.Translate("PawnkindRaceDiversity_WeightSettingColumnLabel_Action"));
            HighlightableInfoMouseover(columnLabel, "PawnkindRaceDiversity_WeightSettingColumnTooltip_Action");
            Text.Anchor = prevAnchor;

            //Other actions
            //Accept button
            btnAccept = new Rect(new Vector2(
                inRect.x + (inRect.width / 2.6f),
                inRect.height - regularButtonSize.y - 10),
                regularButtonSize);
            bool exitFlag = Widgets.ButtonText(btnAccept, Translator.Translate("Accept").CapitalizeFirst(), true, true, true);
            if (exitFlag)
                this.Close();

            //Quick adjust
            if (quickAdjust && !quickAdjustInitializedFlag)
                quickAdjustInitializedFlag = true;
            quickAdjustRect = new Rect(inRect.x + 12f, inRect.height - 65f, 116f, 28f);
            Widgets.CheckboxLabeled(quickAdjustRect, Translator.Translate("PawnkindRaceDiversity_Checkbox_QuickAdjust"), ref quickAdjust);
            if (Mouse.IsOver(quickAdjustRect))
                TooltipHandler.TipRegion(quickAdjustRect, Translator.Translate("PawnkindRaceDiversity_CheckboxTooltip_QuickAdjust"));
            if (!quickAdjust && quickAdjustInitializedFlag)
            {
                inputBoxRaceValue.Clear();
                quickAdjustInitializedFlag = false;
            }

            //Set all to 0
            Rect resetRect = new Rect(inRect.width - 200f, inRect.height - 65f, 185f, 28f);
            bool setToZero = Widgets.ButtonText(resetRect, Translator.Translate("PawnkindRaceDiversity_Button_SetToZero"));
            if (setToZero)
                Find.WindowStack.Add(new Dialog_MessageBox
                    (
                        Translator.Translate("PawnkindRaceDiversity_Button_SetToZero_Confirmation"),
                        Translator.Translate("Yes"),
                        delegate ()
                        {
                            foreach (string race in ModSettingsHandler.evaluatedRaces)
                                SetWeightReference(race, 0.0f);
                        },
                        Translator.Translate("No"),
                        null,
                        null, true
                    ));
            //Reset all settings here
            resetRect = new Rect(inRect.width - 200f, inRect.height - 30f, 185f, 28f);
            bool resetAll = Widgets.ButtonText(resetRect, Translator.Translate("PawnkindRaceDiversity_Button_ResetToDefaults"));
            if (resetAll)
                Find.WindowStack.Add(new Dialog_MessageBox
                    (
                        Translator.Translate("PawnkindRaceDiversity_Button_ResetToDefaults_Confirmation"),
                        Translator.Translate("Yes"),
                        delegate ()
                        {
                            foreach (string race in ModSettingsHandler.evaluatedRaces)
                                SetWeightReference(race, -1.0f);
                        },
                        Translator.Translate("No"),
                        null,
                        null, true
                    ));
        }

        private void HighlightableInfoMouseover(Rect rect, string label)
        {
            if (Mouse.IsOver(rect))
            {
                Widgets.DrawHighlight(rect);
                TooltipHandler.TipRegion(rect, Translator.Translate(label));
            }
        }

        public float GrabWeightReference(string race, HandleContext context, bool returnNegative = false)
        {
            string fullID = ModSettingsHandler.GetRaceSettingWeightID(context, race);
            if (fullID != null)
            {
                SettingHandle<float> handle = windowHandles.FirstOrFallback(h => h.Name == fullID, null);
                if (handle != null)
                {
                    if ((prevAdjustedRaces[race] && windowContext == context) 
                        || (handle.Value < 0.0f && windowContext != context))
                    {
                        if (returnNegative)
                            return -1.0f;

                        switch (context)
                        {
                            case HandleContext.STARTING:
                                return GrabWeightReference(race, HandleContext.WORLD);
                            case HandleContext.WORLD:
                            case HandleContext.LOCAL:
                                //This is performed so that it can also return the human's race setting
                                return GrabWeightReference(race, HandleContext.GENERAL);
                            case HandleContext.GENERAL:
                                KeyValuePair<string, RaceDiversificationPool> data = racesDiversified.FirstOrFallback(r => r.Key == race);
                                if (data.Key != null)
                                    return data.Value.flatGenerationWeight;
                                break;
                        }
                    }
                    //This does not display negatives (checkbox will check for that instead)
                    if (handle.Value >= 0.0f)
                        return handle.Value;
                    else
                        return 0.0f;
                }
            }
            return 0.0f;
        }
        public void SetWeightReference(string race, float value)
        {
            string fullID = ModSettingsHandler.GetRaceSettingWeightID(windowContext, race);
            if (fullID != null)
            {
                SettingHandle<float> handle = windowHandles.FirstOrFallback(h => h.Name == fullID, null);
                if (handle != null)
                {
                    if (value > 1.0f)
                    {
                        value = 1.0f;
                    }
                    if (value >= 0.0f)
                        prevAdjustedRaces[race] = false;
                    else if (value < 0.0f)
                    {
                        if (race.ToLower() == "human" && windowContext == HandleContext.GENERAL)
                            value = handle.DefaultValue;
                        else
                            prevAdjustedRaces[race] = true;
                    }
                    handle.Value = value;
                }
            }
        }

        private void EvaluateWhichDefsAreAdjusted()
        {
            foreach (string race in ModSettingsHandler.evaluatedRaces)
            {
                string fullID = ModSettingsHandler.GetRaceSettingWeightID(windowContext, race);
                SettingHandle<float> handle = windowHandles.FirstOrFallback(h => h.Name == fullID, null);
                if (handle.Value < 0.0f)
                    prevAdjustedRaces[race] = true;
                else
                    prevAdjustedRaces[race] = false;
            }
        }

        private void CalculateSpawnChances()
        {
            spawnChancesVisual.Clear();
            float sum = 0;
            foreach (string race in ModSettingsHandler.evaluatedRaces)
                sum += GrabWeightReference(race, windowContext);
            if (sum == 0)
            {
                foreach (string race in ModSettingsHandler.evaluatedRaces)
                    spawnChancesVisual.Add(race, 0.0f);
                return;
            }
            foreach (string race in ModSettingsHandler.evaluatedRaces)
                spawnChancesVisual.Add(race, GrabWeightReference(race, windowContext) / sum);
        }

        public override void PreClose()
        {
            foreach (SettingHandle<float> handle in windowHandles)
            {
                //Should only update values within this window's context
                if (ModSettingsHandler.WhatContextIsID(handle.Name) == windowContext)
                {
                    float value = handle.Value;
                    if (prevAdjustedRaces[handle.Title])
                        value = -1f;
                    else
                        value = GrabWeightReference(handle.Title, windowContext);
                    ModSettingsHandler.allHandleReferences.Find(h => h.Name == handle.Name).Value = value;
                }

            }
            base.PreClose();
        }

        private Rect windowTitleRect;
        private Rect windowDescRect;
        private Rect btnAccept;
        private Rect quickAdjustRect;
        private Vector2 regularButtonSize = new Vector2(160f, 46f);
        private Vector2 listButtonSize = new Vector2(84f, 24f);
    }
}
