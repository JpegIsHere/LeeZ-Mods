using System;
using HarmonyLib;

namespace LeezGrowLights
{
    /// <summary>
    /// 7 Days to Die mod entry point.
    /// V3.x uses IModApi.InitMod(Mod).
    /// </summary>
    public sealed class ModApi : IModApi
    {
        public void InitMod(Mod _modInstance)
        {
            try
            {
                LeezLog.Info("Loading V3.1 underground-farming runtime candidate v0.5.1.0");
                Harmony harmony = new Harmony(_modInstance.Name);

                // Keep electrical/UI fixes independent from the crop runtime installer so an
                // unrelated crop API failure cannot prevent normal grow-light operation.
                GrowLightPowerPatches.Install(harmony);
                GrowLightActivationPatches.Install(harmony);
                PatchInstaller.Install(harmony);
            }
            catch (Exception ex)
            {
                LeezLog.Error("Runtime initialisation failed. Grow-light acceleration will remain disabled.\n" + ex);
            }
        }
    }
}
