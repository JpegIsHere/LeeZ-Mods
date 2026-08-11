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
                LeezLog.Info("Loading V3.1 grow-light runtime v0.7.0-dev12");
                Harmony harmony = new Harmony(_modInstance.Name);
                PatchInstaller.Install(harmony);
                BlockRemovalInstaller.Install(harmony);
                GrowLightColourInstaller.Install(harmony);

                // Install after colour/brightness interaction so the final radial-command
                // postfix can fill the blank icon names at Priority.Last. The same installer
                // also fixes directly wired lamps reserving power while toggled off.
                GrowLightV31Fixes.Install(harmony);

                // Custom T1 prefabs can contain Unity Light components outside the exact
                // MainLight/Point light + LightLOD branches that vanilla updateLightState owns.
                // Mirror the validated powered+toggled state only onto those unmanaged lights.
                GrowLightPowerVisualSync.Install(harmony);

                // Brightness is now fixed by grow-light tier. Keep the legacy selectable
                // brightness implementation compiled for future reuse, but strip its radial
                // command after the existing interaction/icon patches have completed.
                GrowLightTierBrightness.Install(harmony);

                GrowLightColourNetwork.Install(harmony);
            }
            catch (Exception ex)
            {
                LeezLog.Error("Runtime initialisation failed. Grow-light runtime will remain partially disabled.\n" + ex);
            }
        }
    }
}
