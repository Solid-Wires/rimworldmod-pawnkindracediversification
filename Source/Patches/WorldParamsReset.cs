using PawnkindRaceDiversification.Handlers;

namespace PawnkindRaceDiversification.Patches
{
    public static class WorldParamsReset
    {
        //Harmony manual postfix method
        public static void OnResetCreateWorldParams()
        {
            ModSettingsHandler.ResetHandle(ref ModSettingsHandler.setLocalWorldWeights, HandleContext.WORLD);
            ModSettingsHandler.ResetHandle(ref ModSettingsHandler.setLocalStartingPawnWeights, HandleContext.STARTING);
            ModSettingsHandler.OverrideAllAlienPawnkindsFromStartingPawns = false;
        }
    }
}
