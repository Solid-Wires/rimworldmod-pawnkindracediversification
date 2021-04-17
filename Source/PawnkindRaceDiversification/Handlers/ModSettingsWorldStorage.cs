using HugsLib.Settings;
using HugsLib.Utils;
using Verse;

namespace PawnkindRaceDiversification.Handlers
{
    internal class ModSettingsWorldStorage : UtilityWorldObject
    {
        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Collections.Look(ref ModSettingsHandler.setLocalFlatWeights, "localFlatWeights", LookMode.Value, LookMode.Value);
            foreach (SettingHandle<float> handle in ModSettingsHandler.localHandles)
            {
                float weight = -1f;
                bool successful = ModSettingsHandler.setLocalFlatWeights.TryGetValue(handle.Title, out weight);
                if (successful)
                {
                    handle.Value = weight;
                    handle.StringValue = weight.ToString();
                }
            }
        }
    }
}
