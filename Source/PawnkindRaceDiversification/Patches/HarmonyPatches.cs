using HarmonyLib;
using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Verse;

namespace PawnkindRaceDiversification.Patches
{
    [StaticConstructorOnStartup]
    internal static class HarmonyPatches
    {
        static Harmony harmony => PawnkindRaceDiversification.harmony;

        static HarmonyPatches()
        {
            Patch(AccessTools.Method(typeof(PawnGenerator), "GeneratePawn", new Type[]
            {
                typeof(PawnGenerationRequest)
            }), typeof(PawnkindGenerationHijacker).GetMethod("DetermineRace"), typeof(PawnkindGenerationHijacker).GetMethod("AfterDeterminedRace"));

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
