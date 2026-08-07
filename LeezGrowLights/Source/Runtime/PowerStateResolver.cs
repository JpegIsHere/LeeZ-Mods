using System;
using System.Collections.Generic;

namespace LeezGrowLights
{
    /// <summary>
    /// V3.1 electrical-state adapter validated against the installed-game API probe.
    ///
    /// Confirmed V3.1 members:
    /// - TileEntityPowered.IsPowered
    /// - TileEntityPoweredBlock.IsToggled
    /// - TileEntityPowered.GetPowerItem()
    /// - PowerConsumerToggle.IsPowered / IsToggled
    ///
    /// A grow light is active only when it is both powered and switched on.
    /// Unknown powered tile shapes fail safe as OFF and are logged once.
    /// </summary>
    internal static class PowerStateResolver
    {
        private static readonly HashSet<string> ReportedTypes = new HashSet<string>();

        public static bool IsActive(WorldBase world, Vector3i blockPos)
        {
            if (world == null) return false;

            TileEntity tileEntity = world.GetTileEntity(blockPos);
            if (tileEntity == null) return false;

            // Normal player-powered block path. ceilingLight01_player is expected to
            // resolve through this shape (or a subclass) in V3.1.
            if (tileEntity is TileEntityPoweredBlock poweredBlock)
                return poweredBlock.IsPowered && poweredBlock.IsToggled;

            // Controlled fallback for another TileEntityPowered implementation that
            // exposes its toggle on the underlying PowerConsumerToggle.
            if (tileEntity is TileEntityPowered poweredTile)
            {
                if (!poweredTile.IsPowered) return false;

                PowerItem powerItem = poweredTile.GetPowerItem();
                if (powerItem is PowerConsumerToggle toggle)
                    return toggle.IsPowered && toggle.IsToggled;

                ReportUnknownShapeOnce(tileEntity,
                    "TileEntityPowered had power but its PowerItem was not PowerConsumerToggle.");
                return false;
            }

            ReportUnknownShapeOnce(tileEntity,
                "Tile entity is not TileEntityPowered/TileEntityPoweredBlock.");
            return false;
        }

        private static void ReportUnknownShapeOnce(TileEntity tileEntity, string reason)
        {
            string typeName = tileEntity.GetType().FullName ?? tileEntity.GetType().Name;
            lock (ReportedTypes)
            {
                if (!ReportedTypes.Add(typeName)) return;
            }

            LeezLog.Warning(
                "Grow-light tile entity '" + typeName + "' could not be validated as powered+switched. " +
                reason + " Lamp will fail safe as OFF.");
        }
    }
}
