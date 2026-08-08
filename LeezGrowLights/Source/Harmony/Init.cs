using System;
using HarmonyLib;

namespace LeezGrowLights
{
    public sealed class ModApi : IModApi
    {
        public void InitMod(Mod _modInstance)
        {
            try
            {
                LeezLog.Info("Loading V3.1 grow-light runtime candidate v0.5.3-dev6");
                Harmony harmony = new Harmony(_modInstance.Name);
                PatchInstaller.Install(harmony);
                BlockRemovalInstaller.Install(harmony);
            }
            catch (Exception ex)
            {
                LeezLog.Error("Runtime initialisation failed. Grow-light acceleration will remain disabled.\n" + ex);
            }
        }
    }
}
