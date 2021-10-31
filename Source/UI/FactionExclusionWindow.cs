using static PawnkindRaceDiversification.Data.GeneralLoadingDatabase;
using PawnkindRaceDiversification.Handlers;
using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using Verse;
using HugsLib.Settings;

namespace PawnkindRaceDiversification.UI
{
    public class FactionExclusionWindow : Window
    {
        protected string windowTitle = "PawnkindRaceDiversity_FactionExclusionWindowTitle";
        protected string windowDescription = "PawnkindRaceDiversity_FactionExclusionWindowDescription";
        private Vector2 scrollPosition = new Vector2(0f, 0f);
        public override Vector2 InitialSize => new Vector2(340f, 720f);

        public FactionExclusionWindow()
        {
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
            if (windowTitle != null)
            {
                windowTitleRect = new Rect(new Vector2(
                    inRect.x, inRect.y),
                    new Vector2(
                    inRect.width, 40f)
                    );
                Text.Font = GameFont.Medium;
                Text.Anchor = TextAnchor.MiddleCenter;
                Widgets.Label(windowTitleRect, Translator.Translate(windowTitle));
            }
            float windowTitleElementsYOffset = 28f;
            //Window description
            if (windowDescription != null)
            {
                windowDescRect = new Rect(new Vector2(
                    inRect.x, inRect.y + 32f),
                    new Vector2(
                    inRect.width, 68f)
                );
                Text.Font = GameFont.Small;
                Text.Anchor = TextAnchor.MiddleCenter;
                Widgets.Label(windowDescRect, Translator.Translate(windowDescription));
            }
            windowTitleElementsYOffset += 38f;
            Text.Font = prevFontSize;
            Text.Anchor = prevAnchor;

            if (factionsWithHumanlikesLoaded.Count != 0)
            {
                Rect listingStandardRectInner = new Rect(new Vector2(inRect.x + 10f, inRect.y + 45f + windowTitleElementsYOffset),
                                new Vector2(inRect.width - 20f, inRect.height - 130f - windowTitleElementsYOffset));
                float elementSize = 24f;
                UnityEngine.GUI.BeginGroup(listingStandardRectInner);
                Widgets.BeginScrollView(new Rect(0f, 20f, listingStandardRectInner.width - 2f, listingStandardRectInner.height - 2f), ref scrollPosition,
                    new Rect(listingStandardRectInner.x, listingStandardRectInner.y - (elementSize * 1.6f),
                            listingStandardRectInner.width,
                            (ModSettingsHandler.excludedFactions.Count * elementSize) + ((elementSize * 1.6f) - (windowTitleElementsYOffset / 2f))),
                        true);
                int element = 0;
                float yPos = 0f;
                foreach (SettingHandle<bool> handle in ModSettingsHandler.excludedFactions.Values)
                {
                    FactionDef def = factionsWithHumanlikesLoaded.Find(f => f.defName == handle.Title);
                    if (def != null)
                    {
                        yPos = (element * elementSize) + windowTitleElementsYOffset;
                        element++;
                        Rect innerContentRect = new Rect(listingStandardRectInner.x, yPos, 180f, elementSize);
                        Text.Anchor = TextAnchor.MiddleLeft;
                        //Faction
                        Widgets.Label(innerContentRect, def.label);
                        Text.Anchor = TextAnchor.MiddleCenter;
                        //Excluded
                        innerContentRect = new Rect(80f, yPos, 114f, elementSize);
                        bool tmpExcludedCheck = handle.Value;
                        Widgets.Checkbox(new Vector2(innerContentRect.x + 160f, innerContentRect.y + 5f), ref tmpExcludedCheck, 24f, false, true);
                        handle.Value = tmpExcludedCheck;
                    }
                }
                Text.Anchor = prevAnchor;
                Widgets.EndScrollView();
                UnityEngine.GUI.EndGroup();
            }

            //Accept button
            btnAccept = new Rect(new Vector2(
                inRect.x + (inRect.width / 4.6f),
                inRect.height - regularButtonSize.y - 10),
                regularButtonSize);
            bool exitFlag = Widgets.ButtonText(btnAccept, Translator.Translate("Accept").CapitalizeFirst(), true, true, true);
            if (exitFlag)
                this.Close();
        }

        private Rect windowTitleRect;
        private Rect windowDescRect;
        private Rect btnAccept;
        private Vector2 regularButtonSize = new Vector2(160f, 46f);
    }
}
