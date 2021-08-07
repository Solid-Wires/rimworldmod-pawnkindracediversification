using HarmonyLib;
using RimWorld;
using RimWorld.Planet;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;
using Verse;

namespace PawnkindRaceDiversification.Patches
{
    [StaticConstructorOnStartup]
    internal static class HarmonyPatches
    {
        static Harmony harmony => PawnkindRaceDiversification.harmony;

        static HarmonyPatches()
        {
            //Pawn generation hijacker
            Patch(AccessTools.Method(typeof(PawnGenerator), "GeneratePawn", new Type[]
            {
                typeof(PawnGenerationRequest)
            }), typeof(PawnkindGenerationHijacker).GetMethod("DetermineRace"), typeof(PawnkindGenerationHijacker).GetMethod("AfterDeterminedRace"));
            //World related settings
            Patch(AccessTools.Method(typeof(WorldGenerator), "GenerateWorld", null),
                typeof(WorldRelatedPatches).GetMethod("OnGeneratingWorld"));
            Patch(AccessTools.Method(typeof(Page_CreateWorldParams), "DoWindowContents", new Type[]
            {
                typeof(Rect)
            }),
            null, null, typeof(WorldRelatedPatches).GetMethod("WorldWeightSettingsInWorldPage"));
            //World params will reset on CreateWorldParams resetting, ConfigureStartingPawns going next,
            //  or from entering the main menu.
            Patch(AccessTools.Method(typeof(Page_CreateWorldParams), "Reset", null),
                null, typeof(WorldParamsReset).GetMethod("OnResetCreateWorldParams"));
            Patch(AccessTools.Method(typeof(Page_ConfigureStartingPawns), "DoNext", null),
                typeof(WorldParamsReset).GetMethod("OnResetCreateWorldParams"));
            Patch(AccessTools.Method(typeof(GameDataSaveLoader), "LoadGame", new Type[] { typeof(string) }),
                typeof(WorldParamsReset).GetMethod("OnResetCreateWorldParams"));
        }
        internal static void PostInitPatches()
        {
            //Altered Carbon
            ApplyPatchIntoMod(PawnkindRaceDiversification.SeekedMod.ALTERED_CARBON, "CustomizeSleeveWindow", "GetNewPawn", null,
                typeof(AnyModGeneratedPawn).GetMethod("OnModGeneratedPawn"));
            //Prepare Carefully
            ApplyPatchIntoMod(PawnkindRaceDiversification.SeekedMod.PREPARE_CAREFULLY, "ColonistSaver", "SaveToFile", null,
                null, null, typeof(PrepareCarefullyTweaks).GetMethod("SavingMethodInsertionTranspiler"));
            ApplyPatchIntoMod(PawnkindRaceDiversification.SeekedMod.PREPARE_CAREFULLY, "ColonistLoader", "LoadFromFile", null,
                null, null, typeof(PrepareCarefullyTweaks).GetMethod("LoadingMethodInsertionTranspiler"));
            ApplyPatchIntoMod(PawnkindRaceDiversification.SeekedMod.PREPARE_CAREFULLY, "ExtensionsPawn", "Copy", null,
                typeof(PrepareCarefullyTweaks).GetMethod("PawnPreCopy"), typeof(PrepareCarefullyTweaks).GetMethod("PawnPostCopy"));
            ApplyPatchIntoMod(PawnkindRaceDiversification.SeekedMod.PREPARE_CAREFULLY, "CustomPawn", "InitializeWithPawn", null,
                typeof(PrepareCarefullyTweaks).GetMethod("OnInitializeNewPawn"));
            //Chjee's Androids
            ApplyPatchIntoMod(PawnkindRaceDiversification.SeekedMod.ANDROIDS, "DroidUtility", "MakeDroidTemplate", null,
                null, null, typeof(ChjeeDroidFixes).GetMethod("PawnHostilitySettingFix"));
            //Character Editor
            ApplyPatchIntoMod(PawnkindRaceDiversification.SeekedMod.CHARACTER_EDITOR, "PresetPawnNew", "GeneratePawn", null,
                typeof(AnyModGeneratedPawn).GetMethod("OnModGeneratedPawn"));
        }

        private static void ApplyPatchIntoMod(PawnkindRaceDiversification.SeekedMod modToPatch, string className, string targetMethod, Type[] parameters = null,
            MethodInfo prefixMethod = null, 
            MethodInfo postfixMethod = null,
            MethodInfo transpiler = null,
            MethodInfo finalizer = null)
        {
            if (PawnkindRaceDiversification.activeSeekedMods.Contains(modToPatch))
            {
                //If this specific method is called, then Altered Carbon generated a pawn. We don't want to touch
                //  this pawn.
                Assembly a = PawnkindRaceDiversification.referencedModAssemblies[modToPatch];
                Patch(AccessTools.Method(a.GetTypes().First(t => t.Name == className), targetMethod, parameters), 
                    prefixMethod, postfixMethod, transpiler, finalizer);
            }
        }

        //A more straightforward way to patch things.
        private static void Patch(MethodInfo methodToPatch,
            MethodInfo prefixMethod = null, MethodInfo postfixMethod = null,
            MethodInfo transpiler = null, MethodInfo finalizer = null)
        {
            //Set up basic method patches
            HarmonyMethod prem = prefixMethod != null ? new HarmonyMethod(prefixMethod) : null;
            HarmonyMethod pom = postfixMethod != null ? new HarmonyMethod(postfixMethod) : null;
            HarmonyMethod trans = transpiler != null ? new HarmonyMethod(transpiler) : null;
            HarmonyMethod fin = finalizer != null ? new HarmonyMethod(finalizer) : null;
            //Use harmony to manually patch the given method in the given type
            //Logger.Message("Patching " + type.Name + "...");
            harmony.Patch(methodToPatch, prem, pom, trans, fin);
        }
        //Patch all classes that inherit from and have overrides on a certain method (in one assembly).
        private static void MultipatchInherited(Type masterType, IEnumerable<Assembly> otherAssemblies, string methodToPatch,
            MethodInfo prefixMethod = null, MethodInfo postfixMethod = null,
            MethodInfo transpiler = null, MethodInfo finalizer = null)
        {
            Assembly typeAssembly = Assembly.GetAssembly(masterType);
            List<Type> classes = new List<Type>();
            //Look through each inherited class in this type's assembly
            foreach (Type type in
                typeAssembly.GetTypes()
                .Where(type => type.IsClass && !type.IsAbstract && type.IsSubclassOf(masterType)))
            {
                classes.Add(type);
            }
            //Look for this inherited class in other assemblies (if provided)
            foreach (Assembly otherAssembly in otherAssemblies)
            {
                foreach (Type type in
                otherAssembly.GetTypes()
                .Where(type => type.IsClass && !type.IsAbstract && type.IsSubclassOf(masterType)))
                {
                    classes.Add(type);
                }
            }
            //Patch each class found
            foreach (Type c in classes)
            {
                //Make sure that this method originates from this type
                if (c.GetMethod(methodToPatch).DeclaringType == c)
                {
                    Patch(c.GetMethod(methodToPatch), prefixMethod, postfixMethod, transpiler, finalizer);
                }
            }
        }
    }
}
