using System;
using System.Collections;
using System.Reflection;
using HarmonyLib;

namespace LeezGrowLights
{
    /// <summary>
    /// Narrow adapter over the live V3.1 WorldBlockTicker API.
    ///
    /// Public scheduling/invalidation methods are invoked through reflection so the mod
    /// does not depend on the accessibility of the ticker's internal dictionaries/entry
    /// fields at compile time. The exact member names/signatures were captured from the
    /// installed V3.1.0 b14 runtime.
    /// </summary>
    internal static class TickerScheduleAccessor
    {
        private static readonly Type TickerType = AccessTools.TypeByName("WorldBlockTicker");
        private static readonly Type EntryType = AccessTools.TypeByName("WorldBlockTickerEntry");
        private static readonly Type TimerType = AccessTools.TypeByName("GameTimer");

        private static readonly MethodInfo GetTickerMethod =
            AccessTools.Method(typeof(WorldBase), "GetWBT");

        private static readonly MethodInfo InvalidateMethod =
            TickerType == null
                ? null
                : AccessTools.Method(
                    TickerType,
                    "InvalidateScheduledBlockUpdate",
                    new[] { typeof(Vector3i), typeof(int) });

        private static readonly MethodInfo AddMethod =
            TickerType == null
                ? null
                : AccessTools.Method(
                    TickerType,
                    "AddScheduledBlockUpdate",
                    new[] { typeof(Vector3i), typeof(int), typeof(ulong) });

        private static readonly FieldInfo ScheduledDictionaryField =
            TickerType == null ? null : AccessTools.Field(TickerType, "scheduledTicksDict");

        private static readonly FieldInfo TickerLockField =
            TickerType == null ? null : AccessTools.Field(TickerType, "lockObject");

        private static readonly FieldInfo ScheduledTimeField =
            EntryType == null ? null : AccessTools.Field(EntryType, "scheduledTime");

        private static readonly MethodInfo EntryHashMethod =
            EntryType == null
                ? null
                : AccessTools.Method(
                    EntryType,
                    "ToHashCode",
                    new[] { typeof(Vector3i), typeof(int) });

        private static readonly PropertyInfo TimerInstanceProperty =
            TimerType == null ? null : AccessTools.Property(TimerType, "Instance");

        private static readonly FieldInfo TimerTicksField =
            TimerType == null ? null : AccessTools.Field(TimerType, "ticks");

        private static bool reportedUnavailable;

        internal static bool RescheduleRemainingWork(
            WorldBase world,
            Vector3i plantPos,
            int blockId,
            float oldMultiplier,
            float newMultiplier)
        {
            if (world == null || world.IsRemote())
                return false;

            oldMultiplier = Math.Max(1f, oldMultiplier);
            newMultiplier = Math.Max(1f, newMultiplier);

            if (Math.Abs(oldMultiplier - newMultiplier) <= 0.0001f)
                return false;

            if (!TryGetTicker(world, out object ticker) ||
                !TryGetCurrentTicks(out ulong now) ||
                !TryGetScheduledEnd(ticker, plantPos, blockId, out ulong scheduledEnd))
            {
                return false;
            }

            if (scheduledEnd <= now)
                return false;

            ulong oldRemainingTicks = scheduledEnd - now;

            // The queued duration was generated after GetTickRate had already been divided
            // by oldMultiplier (including vanilla growth deviation/randomisation). Multiplying
            // its remaining ticks by oldMultiplier converts it back to equivalent vanilla work.
            double remainingVanillaWork = oldRemainingTicks * (double)oldMultiplier;
            ulong newRemainingTicks = (ulong)Math.Max(
                1d,
                Math.Round(
                    remainingVanillaWork / newMultiplier,
                    MidpointRounding.AwayFromZero));

            try
            {
                InvalidateMethod.Invoke(ticker, new object[] { plantPos, blockId });
                AddMethod.Invoke(ticker, new object[] { plantPos, blockId, newRemainingTicks });

                LeezLog.Info(
                    "Mid-stage crop rescheduled at " + plantPos +
                    ": " + oldMultiplier.ToString("0.###") + "x -> " +
                    newMultiplier.ToString("0.###") + "x, remaining " +
                    oldRemainingTicks + " -> " + newRemainingTicks + " ticks.");

                return true;
            }
            catch (Exception ex)
            {
                ReportUnavailableOnce("ticker reschedule failed: " + ex.Message);
                return false;
            }
        }

        private static bool TryGetTicker(WorldBase world, out object ticker)
        {
            ticker = null;

            if (!MembersAvailable())
                return false;

            try
            {
                ticker = GetTickerMethod.Invoke(world, null);
                return ticker != null;
            }
            catch (Exception ex)
            {
                ReportUnavailableOnce("GetWBT failed: " + ex.Message);
                return false;
            }
        }

        private static bool TryGetCurrentTicks(out ulong ticks)
        {
            ticks = 0;

            if (!MembersAvailable())
                return false;

            try
            {
                object timer = TimerInstanceProperty.GetValue(null, null);
                if (timer == null)
                    return false;

                object value = TimerTicksField.GetValue(timer);
                if (value is ulong tickValue)
                {
                    ticks = tickValue;
                    return true;
                }
            }
            catch (Exception ex)
            {
                ReportUnavailableOnce("GameTimer tick read failed: " + ex.Message);
            }

            return false;
        }

        private static bool TryGetScheduledEnd(
            object ticker,
            Vector3i plantPos,
            int blockId,
            out ulong scheduledEnd)
        {
            scheduledEnd = 0;

            try
            {
                int hash = (int)EntryHashMethod.Invoke(
                    null,
                    new object[] { plantPos, blockId });

                object dictionaryObject = ScheduledDictionaryField.GetValue(ticker);
                if (!(dictionaryObject is IDictionary dictionary))
                    return false;

                object lockObject = TickerLockField.GetValue(ticker) ?? ticker;

                lock (lockObject)
                {
                    if (!dictionary.Contains(hash))
                        return false;

                    object entry = dictionary[hash];
                    if (entry == null)
                        return false;

                    object value = ScheduledTimeField.GetValue(entry);
                    if (value is ulong time)
                    {
                        scheduledEnd = time;
                        return true;
                    }
                }
            }
            catch (Exception ex)
            {
                ReportUnavailableOnce("scheduled-entry lookup failed: " + ex.Message);
            }

            return false;
        }

        private static bool MembersAvailable()
        {
            bool ok =
                TickerType != null &&
                EntryType != null &&
                TimerType != null &&
                GetTickerMethod != null &&
                InvalidateMethod != null &&
                AddMethod != null &&
                ScheduledDictionaryField != null &&
                TickerLockField != null &&
                ScheduledTimeField != null &&
                EntryHashMethod != null &&
                TimerInstanceProperty != null &&
                TimerTicksField != null;

            if (!ok)
                ReportUnavailableOnce(
                    "one or more validated V3.1 ticker members were not found; " +
                    "mid-stage rescheduling is disabled.");

            return ok;
        }

        private static void ReportUnavailableOnce(string reason)
        {
            if (reportedUnavailable)
                return;

            reportedUnavailable = true;
            LeezLog.Warning("Mid-stage rescheduler unavailable: " + reason);
        }
    }
}
