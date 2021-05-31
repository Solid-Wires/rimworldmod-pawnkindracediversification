using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PawnkindRaceDiversification.Patches
{
    public static class AnyModGeneratedPawn
    {
        //Harmony manual prefix method
        public static void OnModGeneratedPawn()
        {
            PawnkindGenerationHijacker.PauseWeightGeneration();
        }
    }
}
