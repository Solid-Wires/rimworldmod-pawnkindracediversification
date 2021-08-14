using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Text;
using System.Threading.Tasks;
using Verse;

namespace PawnkindRaceDiversification.Patches
{
    public static class AnyModGeneratedPawn
    {
        //Harmony manual prefix method
        public static void OnModGeneratingPawn()
        {
            PawnkindGenerationHijacker.PauseWeightGeneration();
        }
    }
}
