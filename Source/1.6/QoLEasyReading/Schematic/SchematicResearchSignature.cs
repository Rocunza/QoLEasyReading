using System;
using System.Collections.Generic;

namespace ZCN.QoL.EasyReading
{
    /// <summary>
    /// S3-C1: research-project identity only, independent of the current research
    /// progress, book quality, translated label, or reading reward amount.
    /// The string encoding matches S3-B0's existing BuildSignatures project key.
    /// Full ordinal strings are compared; FNV fingerprints are diagnostic ONLY.
    /// </summary>
    internal static class SchematicResearchSignature
    {
        internal static bool TryCreate(SchematicResearchState state, out string fullKey)
        {
            fullKey = null;
            if (state == null || state.Validity != SchematicResearchValidity.Valid ||
                state.ProjectCount == 0 || state.Projects == null)
                return false;
            List<string> names = new List<string>(state.ProjectCount);
            foreach (SchematicResearchProject entry in state.Projects)
            {
                if (entry == null || entry.ProjectDef == null ||
                    string.IsNullOrEmpty(entry.ProjectDef.defName))
                    return false;
                names.Add(entry.ProjectDef.defName);
            }
            return TryCreateFromNames(names, out fullKey);
        }

        internal static bool TryCreateFromNames(IEnumerable<string> names, out string fullKey)
        {
            fullKey = null;
            if (names == null) return false;
            List<string> keys = new List<string>();
            HashSet<string> unique = new HashSet<string>(StringComparer.Ordinal);
            foreach (string name in names)
            {
                if (string.IsNullOrEmpty(name)) return false;
                if (unique.Add(name)) keys.Add(name.Length + ":" + name);
                if (keys.Count > 256) return false; // bounded invalid input
            }
            if (keys.Count == 0) return false;
            keys.Sort(StringComparer.Ordinal);
            fullKey = string.Join("|", keys.ToArray());
            return true;
        }

        internal static string Fingerprint(string fullKey)
        {
            if (string.IsNullOrEmpty(fullKey)) return "NONE";
            unchecked
            {
                uint hash = 2166136261;
                for (int i = 0; i < fullKey.Length; i++)
                    hash = (hash ^ fullKey[i]) * 16777619;
                return hash.ToString("X8");
            }
        }
    }
}
