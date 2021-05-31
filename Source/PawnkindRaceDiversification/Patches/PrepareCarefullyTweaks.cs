using AlienRace;
using HarmonyLib;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Text;
using System.Threading.Tasks;
using Verse;

namespace PawnkindRaceDiversification.Patches
{
    //This might seem weird, but it makes sense for what I'm trying to avoid.
    public class PrepareCarefullyTweaks
    {
        public static string loadedAlienRace = "none";
        private static ThingDef prevRaceDef = null;
        private static string PC_Filepath = (string)typeof(GenFilePaths).GetMethod("FolderUnderSaveData", BindingFlags.Static | BindingFlags.NonPublic).Invoke(null, new object[]
		{
			"PrepareCarefully"
		});

        //Transpiler method
        //  Inserts the harmony patch method OnPrepareCarefullySavingPawn(colonistName)
        public static IEnumerable<CodeInstruction> SavingMethodInsertionTranspiler(IEnumerable<CodeInstruction> instructions)
        {
            int insertionIndex = -1;

            var codes = new List<CodeInstruction>(instructions);

            for (int i = 0; i < codes.Count; i++)
            {
                if (codes[i].opcode == OpCodes.Call
                    && codes[i].operand.ToString().Contains("Void Look[SaveRecordPawnV"))
                {
                    //PawnkindRaceDiversification.Logger.Message("Found the method, injecting my save method into it");
                    insertionIndex = i + 1;
                }
            }
            if (insertionIndex > -1)
            {
                codes.Insert(insertionIndex,
                    new CodeInstruction(OpCodes.Call, typeof(PrepareCarefullyTweaks).GetMethod("OnPrepareCarefullySavingPawn")));
                codes.Insert(insertionIndex, new CodeInstruction(OpCodes.Ldarg_1));
                codes.Insert(insertionIndex, new CodeInstruction(OpCodes.Callvirt,
                    AccessTools.Method(PawnkindRaceDiversification.referencedModAssemblies[PawnkindRaceDiversification.SeekedMod.PREPARE_CAREFULLY]
                    .GetTypes().First(t => t.Name == "CustomPawn"), "get_Pawn")));
                codes.Insert(insertionIndex, new CodeInstruction(OpCodes.Ldarg_0));
            }

            return codes.AsEnumerable();
        }
        //Transpiler method
        //  Inserts the harmony patch method IsPrepareCarefullyLoadingPawn(name)
        public static IEnumerable<CodeInstruction> LoadingMethodInsertionTranspiler(IEnumerable<CodeInstruction> instructions)
        {
            List<int> insertionIndices = new List<int>();

            var codes = new List<CodeInstruction>(instructions);

            for (int i = 0; i < codes.Count; i++)
            {
                if (i > 0 && i < codes.Count - 1
                    && codes[i].opcode == OpCodes.Call
                    && codes[i + 1].opcode == OpCodes.Ret &&
                    (codes[i].operand.ToString().Contains("EdB.PrepareCarefully.CustomPawn Load")))
                {
                    //PawnkindRaceDiversification.Logger.Message("Found the method, injecting my load method into it: " + codes[i].operand.ToString());
                    insertionIndices.Add(i - 1);
                }
            }
            if (insertionIndices.Count > 0)
            {
                foreach (int index in insertionIndices)
                {
                    codes.Insert(index,
                        new CodeInstruction(OpCodes.Call, typeof(PrepareCarefullyTweaks).GetMethod("IsPrepareCarefullyLoadingPawn")));
                    codes.Insert(index, new CodeInstruction(OpCodes.Ldarg_1));
                }
            }

            return codes.AsEnumerable();
        }
        //Harmony manual transpiler method
        //  Saves data related to what race the pawn is
        public static void OnPrepareCarefullySavingPawn(Pawn pawn, string colonistName)
        {
            //PawnkindRaceDiversification.Logger.Message("Prepare carefully saved a pawn: " + colonistName);
            //Save this pawn's race (a saving extension for Prepare Carefully)
            Scribe_Values.Look<string>(ref pawn.def.defName, "HAR_AlienRace", null, false);
        }
        //Harmony manual transpiler method
        //  Loads data related to what race the pawn is
        public static void IsPrepareCarefullyLoadingPawn(string name)
        {
            //PawnkindRaceDiversification.Logger.Message("Prepare carefully loaded a pawn: " + name);
            Scribe.loader.InitLoading(Path.Combine(PC_Filepath, name + ".pcc"));
            Scribe_Values.Look<string>(ref loadedAlienRace, "HAR_AlienRace", "none", false);
            Scribe.loader.FinalizeLoading();
        }
        //Simply reassigns the previous pawn's ThingDef to the pawn's copy def that PrepareCarefully makes.
        //  This really seems like an oversight on that mod's end. It doesn't seem to do this because it has
        //  its own way of constructing pawns (for other compatibility or dependency-avoidance reasons?).
        //  Please let me know if I shouldn't be doing this.
        public static void PawnPreCopy(Pawn source)
        {
            //PawnkindRaceDiversification.Logger.Message("PreCopy: " + source.def.defName);
            prevRaceDef = source.def;
        }
        public static void PawnPostCopy(ref Pawn __result)
        {
            //PawnkindRaceDiversification.Logger.Message("PostCopy: " + __result.def.defName);
            __result.def = prevRaceDef;
            prevRaceDef = null;
        }
        //Pawn initialization
        public static void OnInitializeNewPawn(Pawn pawn)
        {
            if (loadedAlienRace != "none")
            {
                //PawnkindRaceDiversification.Logger.Message("Loaded pawn is initializing with race " + loadedAlienRace);
                pawn.def = (from x in DefDatabase<ThingDef_AlienRace>.AllDefs
                            where x.race != null
                            select x).First(a => a.defName == loadedAlienRace);
            }
            loadedAlienRace = "none";
        }
    }
}
