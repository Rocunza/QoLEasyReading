using System;
using System.Collections;
using System.Reflection;
using RimWorld;
using Verse;

namespace ZCN.QoL.EasyReading
{
    /// <summary>
    /// RimWorld 1.6 private/protected research-doer API access. Reflection is
    /// resolved once; no game method is patched or game state written.
    /// </summary>
    internal static class SchematicResearchAccess
    {
        private static readonly object InitLock = new object();
        private static bool initialized;
        private static FieldInfo valuesField;
        private static MethodInfo visibilityMethod;
        private static Func<ReadingOutcomeDoerGainResearch, ResearchProjectDef, bool> visibilityDelegate;
        private static string failureReason;

        internal static bool IsReady { get { EnsureInitialized(); return failureReason == null; } }
        internal static string FailureReason { get { EnsureInitialized(); return failureReason ?? string.Empty; } }
        internal static string InvocationMode { get { EnsureInitialized(); return
            visibilityDelegate != null ? "CachedDelegate" : (failureReason == null ? "CachedReflection" : "Unavailable"); } }

        internal static void EnsureInitialized()
        {
            if (initialized) return;
            lock (InitLock)
            {
                if (initialized) return;
                try
                {
                    Type type = typeof(ReadingOutcomeDoerGainResearch);
                    valuesField = type.GetField("values", BindingFlags.Instance | BindingFlags.NonPublic);
                    if (valuesField == null || valuesField.IsStatic ||
                        !typeof(IDictionary).IsAssignableFrom(valuesField.FieldType))
                    {
                        failureReason = "ValuesFieldMissingOrChanged";
                        return;
                    }

                    visibilityMethod = type.GetMethod("IsProjectVisible",
                        BindingFlags.Instance | BindingFlags.NonPublic, null,
                        new[] { typeof(ResearchProjectDef) }, null);
                    if (visibilityMethod == null || visibilityMethod.IsStatic ||
                        visibilityMethod.ReturnType != typeof(bool))
                    {
                        failureReason = "VisibilityMethodMissingOrChanged";
                        return;
                    }

                    // Open-instance delegate: removes per-project MethodInfo.Invoke
                    // allocations where the runtime permits access to private methods.
                    // The cached MethodInfo remains as a safe compatibility fallback.
                    try
                    {
                        visibilityDelegate = (Func<ReadingOutcomeDoerGainResearch, ResearchProjectDef, bool>)
                            Delegate.CreateDelegate(
                                typeof(Func<ReadingOutcomeDoerGainResearch, ResearchProjectDef, bool>),
                                visibilityMethod);
                    }
                    catch (Exception)
                    {
                        visibilityDelegate = null;
                    }
                }
                catch (Exception ex)
                {
                    failureReason = "AccessInitialization:" + ex.GetType().Name;
                }
                finally
                {
                    initialized = true;
                }
            }
        }

        internal static bool TryGetValues(ReadingOutcomeDoerGainResearch doer,
            out IDictionary values, out string reason)
        {
            values = null;
            reason = string.Empty;
            if (doer == null) { reason = "DoerNull"; return false; }
            if (!IsReady) { reason = FailureReason; return false; }
            try
            {
                values = valuesField.GetValue(doer) as IDictionary;
                if (values == null) { reason = "ValuesNullOrWrongType"; return false; }
                return true;
            }
            catch (Exception ex)
            {
                reason = "ValuesRead:" + ex.GetType().Name;
                return false;
            }
        }

        internal static bool TryIsProjectVisible(ReadingOutcomeDoerGainResearch doer,
            ResearchProjectDef project, out bool visible, out string reason)
        {
            visible = false;
            reason = string.Empty;
            if (doer == null || project == null) { reason = "VisibilityArgumentsNull"; return false; }
            if (!IsReady) { reason = FailureReason; return false; }
            try
            {
                if (visibilityDelegate != null)
                    visible = visibilityDelegate(doer, project);
                else
                    visible = (bool)visibilityMethod.Invoke(doer, new object[] { project });
                return true;
            }
            catch (Exception ex)
            {
                reason = "VisibilityCall:" + ex.GetType().Name;
                return false;
            }
        }
    }
}
