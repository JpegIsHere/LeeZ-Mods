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
                LeezLog.Info("Loading V3.1 underground-farming runtime candidate v0.5.2-dev1");
                Harmony harmony = new Harmony(_modInstance.Name);
                PatchInstaller.Install(harmony);

                // Temporary development diagnostic for the exact V3.1 world block ticker API.
                // Remove after the mid-stage rescheduling implementation is validated.
                TickerApiDiagnostics.DumpOnce();
            }
            catch (Exception ex)
            {
                LeezLog.Error("Runtime initialisation failed. Grow-light acceleration will remain disabled.\n" + ex);
            }
        }
    }
}
