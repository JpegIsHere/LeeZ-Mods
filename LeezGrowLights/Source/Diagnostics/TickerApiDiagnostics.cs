using System;
using System.Linq;
using System.Reflection;
using HarmonyLib;

namespace LeezGrowLights
{
    /// <summary>
    /// Temporary V3.1 runtime diagnostic used to discover the exact live
    /// WorldBlockTicker / WorldBlockTickerEntry API before implementing
    /// progress-preserving mid-stage crop rescheduling.
    ///
    /// This is intentionally reflection-only so the diagnostic still builds
    /// even if the V3.1 ticker types changed names or accessibility.
    /// </summary>
    internal static class TickerApiDiagnostics
    {
        private static bool dumped;

        public static void DumpOnce()
        {
            if (dumped) return;
            dumped = true;

            LeezLog.Info("=== V3.1 MID-STAGE TICKER API DIAGNOSTIC START ===");

            DumpType("WorldBlockTicker");
            DumpType("WorldBlockTickerEntry");
            DumpType("WorldBase");
            DumpType("World");
            DumpType("GameTimer");

            LeezLog.Info("=== V3.1 MID-STAGE TICKER API DIAGNOSTIC END ===");
        }

        private static void DumpType(string typeName)
        {
            Type type = AccessTools.TypeByName(typeName);
            if (type == null)
            {
                LeezLog.Warning("[TickerProbe] TYPE NOT FOUND: " + typeName);
                return;
            }

            LeezLog.Info("[TickerProbe] TYPE " + type.FullName +
                         " base=" + (type.BaseType != null ? type.BaseType.FullName : "<none>"));

            const BindingFlags flags = BindingFlags.Instance |
                                       BindingFlags.Static |
                                       BindingFlags.Public |
                                       BindingFlags.NonPublic |
                                       BindingFlags.DeclaredOnly;

            try
            {
                foreach (FieldInfo field in type.GetFields(flags).OrderBy(f => f.Name))
                {
                    string fieldType;
                    try { fieldType = field.FieldType.FullName; }
                    catch { fieldType = "<unavailable>"; }

                    LeezLog.Info("[TickerProbe] FIELD " + type.Name + "." +
                                 field.Name + " : " + fieldType);
                }
            }
            catch (Exception ex)
            {
                LeezLog.Warning("[TickerProbe] field enumeration failed for " +
                                typeName + ": " + ex.Message);
            }

            try
            {
                foreach (PropertyInfo property in type.GetProperties(flags).OrderBy(p => p.Name))
                {
                    string propertyType;
                    try { propertyType = property.PropertyType.FullName; }
                    catch { propertyType = "<unavailable>"; }

                    LeezLog.Info("[TickerProbe] PROPERTY " + type.Name + "." +
                                 property.Name + " : " + propertyType);
                }
            }
            catch (Exception ex)
            {
                LeezLog.Warning("[TickerProbe] property enumeration failed for " +
                                typeName + ": " + ex.Message);
            }

            try
            {
                foreach (ConstructorInfo ctor in type.GetConstructors(flags)
                             .OrderBy(c => c.MetadataToken))
                {
                    LeezLog.Info("[TickerProbe] CTOR " + type.Name +
                                 FormatParameters(ctor));
                }
            }
            catch (Exception ex)
            {
                LeezLog.Warning("[TickerProbe] constructor enumeration failed for " +
                                typeName + ": " + ex.Message);
            }

            try
            {
                foreach (MethodInfo method in type.GetMethods(flags)
                             .OrderBy(m => m.Name)
                             .ThenBy(m => m.MetadataToken))
                {
                    string returnType;
                    try { returnType = method.ReturnType.FullName; }
                    catch { returnType = "<unavailable>"; }

                    LeezLog.Info("[TickerProbe] METHOD " + returnType + " " +
                                 type.Name + "." + method.Name +
                                 FormatParameters(method));
                }
            }
            catch (Exception ex)
            {
                LeezLog.Warning("[TickerProbe] method enumeration failed for " +
                                typeName + ": " + ex.Message);
            }
        }

        private static string FormatParameters(MethodBase method)
        {
            try
            {
                ParameterInfo[] parameters = method.GetParameters();
                return "(" + string.Join(", ", parameters.Select(p =>
                {
                    string parameterType;
                    try { parameterType = p.ParameterType.FullName; }
                    catch { parameterType = "<unavailable>"; }
                    return parameterType + " " + p.Name;
                })) + ")";
            }
            catch (Exception ex)
            {
                return "(<parameters unavailable: " + ex.GetType().Name +
                       ": " + ex.Message + ">)";
            }
        }
    }
}
