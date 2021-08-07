using PawnkindRaceDiversification.Handlers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using Verse;

namespace PawnkindRaceDiversification.UI
{
    public class SelectWorldSettingWindow : Window
    {
        public override Vector2 InitialSize => new Vector2(300f, 250f);

        public SelectWorldSettingWindow()
        {
            this.absorbInputAroundWindow = true;
            this.closeOnClickedOutside = true;
            this.onlyOneOfTypeAllowed = true;
            this.doCloseX = true;
        }

        public override void DoWindowContents(Rect inRect)
        {
            //Default text settings
            GameFont prevFontSize = Text.Font;
            TextAnchor prevAnchor = Text.Anchor;

            //Window title
            windowTitleRect = new Rect(new Vector2(
                inRect.x, inRect.y + 8f),
                new Vector2(
                inRect.width, 40f)
                );
            Text.Font = GameFont.Medium;
            Text.Anchor = TextAnchor.MiddleCenter;
            Widgets.Label(windowTitleRect, Translator.Translate("PawnkindRaceDiversity_SelectSetting"));

            Text.Font = GameFont.Small;
            Text.Anchor = TextAnchor.MiddleCenter;
            float yOffset = 32f;
            //World settings button
            btnWorldAdjustments = new Rect(new Vector2(
                inRect.x + (inRect.width - regularButtonSize.x),
                regularButtonSize.y + yOffset),
                regularButtonSize);
            bool openedWorldAdjustments = Widgets.ButtonText(btnWorldAdjustments, Translator.Translate("PawnkindRaceDiversity_WeightWindowTitle_FlatWeightsPerWorldGen").CapitalizeFirst(), true, true, true);
            if (openedWorldAdjustments)
                Find.WindowStack.Add(new WeightSettingsWindow(HandleContext.WORLD));
            

            yOffset += regularButtonSize.y + 14f;
            //Starting pawns button
            btnStartingAdjustments = new Rect(new Vector2(
                inRect.x + (inRect.width - regularButtonSize.x),
                regularButtonSize.y + yOffset),
                regularButtonSize);
            bool openedStartingAdjustments = Widgets.ButtonText(btnStartingAdjustments, Translator.Translate("PawnkindRaceDiversity_WeightWindowTitle_FlatWeightsStartingPawns").CapitalizeFirst(), true, true, true);
            if (openedStartingAdjustments)
                Find.WindowStack.Add(new WeightSettingsWindow(HandleContext.STARTING));

            yOffset += regularButtonSize.y + 14f;
            //Override starting alien pawnkinds checkmark
            overrideStartingAlienPawnkindsRect = new Rect(new Vector2(
                inRect.x + (inRect.width - regularButtonSize.x),
                regularButtonSize.y + yOffset),
                regularButtonSize);
            if (ModSettingsHandler.OverrideAllAlienPawnkinds)
                GUI.color = Color.gray * new Color(1f, 1f, 1f, 0.3f);
            Widgets.CheckboxLabeled(overrideStartingAlienPawnkindsRect, Translator.Translate("PawnkindRaceDiversity_OverrideAllStartingAlienPawnkinds_label"), ref ModSettingsHandler.OverrideAllAlienPawnkindsFromStartingPawns, ModSettingsHandler.OverrideAllAlienPawnkinds);
            GUI.color = Color.white;

            Text.Font = prevFontSize;
            Text.Anchor = prevAnchor;
        }

        private Rect windowTitleRect;
        private Rect btnWorldAdjustments;
        private Rect btnStartingAdjustments;
        private Rect overrideStartingAlienPawnkindsRect;
        private Vector2 regularButtonSize = new Vector2(260f, 28f);
    }
}
