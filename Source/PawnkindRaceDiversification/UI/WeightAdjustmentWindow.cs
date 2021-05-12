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
    public class WeightAdjustmentWindow : Window
    {
        private string windowTitle = "PawnkindRaceDiversity_AdjustmentWindowTitle";
        public override Vector2 InitialSize => new Vector2(300f, 380f);
        private string raceAdjusting = null;
        private string textField = null;
        private float outFlatWeight = 0.0f;
        private WeightSettingsWindow parent = null;

        public WeightAdjustmentWindow(WeightSettingsWindow parent, string raceAdjusting)
        {
            this.parent = parent;
            this.raceAdjusting = raceAdjusting;
            this.outFlatWeight = parent.windowHandles.Find(h => h.Title == raceAdjusting && ModSettingsHandler.WhatContextIsID(h.Name) == parent.windowContext).Value;
            this.textField = outFlatWeight.ToString("0.0##");
            this.absorbInputAroundWindow = true;
            this.closeOnClickedOutside = false;
            this.onlyOneOfTypeAllowed = true;
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
            Text.Font = prevFontSize;
            Text.Anchor = prevAnchor;

            Rect weightAdjustmentRectLabel = new Rect(inRect.width - 210f, inRect.y + 60f, 105f, 24f);
            Rect weightAdjustmentRect = new Rect(inRect.width - 110f, inRect.y + 60f, 76f, 24f);
            float value = 0.0f;
            Widgets.Label(weightAdjustmentRectLabel, Translator.Translate("PawnkindRaceDiversity_TextboxLabel_SetFlatWeight"));
            string inp = Widgets.TextField(weightAdjustmentRect, textField);
            textField = inp;
            bool valid = float.TryParse(textField, out value);
            if (valid)
                outFlatWeight = value;

            Rect moreComingSoonRect = new Rect(inRect.x + 20f, inRect.y + 100f, 220f, 60f);
            Widgets.Label(moreComingSoonRect, "More coming soon on this window. Please be patient.");

            //Accept button
            btnAccept = new Rect(new Vector2(
                inRect.x + (inRect.width / 4.6f),
                inRect.height - regularButtonSize.y - 10),
                regularButtonSize);
            bool exitFlag = Widgets.ButtonText(btnAccept, Translator.Translate("Accept").CapitalizeFirst(), true, true, true);
            if (exitFlag)
                this.Close();
        }

        public override void PreClose()
        {
            parent.windowHandles.Find(h => h.Title == raceAdjusting && ModSettingsHandler.WhatContextIsID(h.Name) == parent.windowContext).Value = outFlatWeight;
            base.PreClose();
        }

        private Rect windowTitleRect;
        private Rect btnAccept;
        private Vector2 regularButtonSize = new Vector2(160f, 46f);
    }
}
