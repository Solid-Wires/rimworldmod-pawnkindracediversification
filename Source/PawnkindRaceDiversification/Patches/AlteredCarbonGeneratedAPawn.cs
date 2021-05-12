using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PawnkindRaceDiversification.Patches
{
    public static class AlteredCarbonGeneratedAPawn
    {
        //Harmony manual prefix method
        public static void OnAlteredCarbonGeneratePawn()
        {
            //PawnkindRaceDiversification.Logger.Message("Altered Carbon is generating");
            PawnkindGenerationHijacker.PauseWeightGeneration();
        }
    }
}
