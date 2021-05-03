using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PawnkindRaceDiversification.Patches
{
    public static class AlteredCarbonGeneratedAPawn
    {
        public static bool didAlteredCarbonGeneratePawn = false;

        //Harmony manual prefix method
        public static void OnAlteredCarbonGeneratePawn()
        {
            //PawnkindRaceDiversification.Logger.Message("Altered Carbon is generating");
            didAlteredCarbonGeneratePawn = true;
        }
    }
}
