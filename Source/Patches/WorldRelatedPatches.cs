using HarmonyLib;
using PawnkindRaceDiversification.Handlers;
using PawnkindRaceDiversification.UI;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;
using UnityEngine;
using Verse;

namespace PawnkindRaceDiversification.Patches
{
    public static class WorldRelatedPatches
    {
        public static void OnGeneratingWorld() => ModSettingsHandler.SyncWorldWeightsIntoLocalWeights();

        public static IEnumerable<CodeInstruction> WorldWeightSettingsInWorldPage(IEnumerable<CodeInstruction> instructions)
        {
            var codes = new List<CodeInstruction>(instructions);

            for (int i = 0; i < codes.Count; i++)
            {
                //To be honest, I took MyLittlePlanet's transpiler operation to do this.
                if (codes[i].opcode == OpCodes.Ldstr
                    && codes[i].operand.Equals("PlanetCoverageTip"))
                {
                    int num = 2;
                    codes.InsertRange(i + num, new List<CodeInstruction>
                        {
                            new CodeInstruction(OpCodes.Ldloc_S, 6),
                            new CodeInstruction(OpCodes.Ldc_R4, 40f),
                            new CodeInstruction(OpCodes.Add, null),
                            new CodeInstruction(OpCodes.Stloc_S, 6),
                            new CodeInstruction(OpCodes.Ldloc_S, 6),
                            new CodeInstruction(OpCodes.Ldloc_S, 7),
                            new CodeInstruction(OpCodes.Call, typeof(WorldRelatedPatches).GetMethod("DrawWorldWeightButton"))
                    });
                    break;
                }
            }

            return codes.AsEnumerable();
        }

        public static void DrawWorldWeightButton(float num, float width2)
        {
            Widgets.Label(new Rect(0f, num, width2, 30f), "PawnkindRaceDiversity_PageCreateWorldParams_AlienWeights_Label".Translate());
            if (Widgets.ButtonText(new Rect(200f, num, width2, 30f), "PawnkindRaceDiversity_Category_ShowAdjustments".Translate()))
                Find.WindowStack.Add(new SelectWorldSettingWindow());
        }
    }
}
