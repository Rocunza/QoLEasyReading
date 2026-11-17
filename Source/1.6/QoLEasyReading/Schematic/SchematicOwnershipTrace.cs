using System;
using System.Collections.Generic;
using RimWorld;
using Verse;

namespace ZCN.QoL.EasyReading
{
    // S3-C1-L1: diagnostic evidence only. Neither ownership decisions nor game
    // objects are retained in these records. Full keys, not FNV hashes, compare.
    internal sealed class SchematicOwnershipTraceRecord
    {
        internal readonly string BookThingId;
        internal readonly string ProjectFullKey;
        internal readonly string Title;
        internal string OuterThingId;
        internal readonly HashSet<string> Sources = new HashSet<string>(StringComparer.Ordinal);
        internal bool HasVerifiedSource;
        internal bool SourcePromoted;

        internal SchematicOwnershipTraceRecord(string id, string fullKey, string title, string outerId)
        {
            BookThingId = id;
            ProjectFullKey = fullKey;
            Title = title;
            OuterThingId = outerId;
        }

        internal void AddSource(string source)
        {
            if (string.IsNullOrEmpty(source)) return;
            bool wasLoose = Sources.Contains("HOME_MAP_LOOSE");
            Sources.Add(source);
            if (source != "HOME_MAP_LOOSE")
            {
                if (!HasVerifiedSource && wasLoose) SourcePromoted = true;
                HasVerifiedSource = true;
            }
        }

        internal string SourceNames()
        {
            List<string> values = new List<string>(Sources);
            values.Sort(StringComparer.Ordinal);
            return string.Join(",", values.ToArray());
        }
    }

    internal static class SchematicOwnershipTrace
    {
        // Invoked strictly from C1's existing bounded scan. The caller wraps
        // diagnostic exceptions without changing FoundKeys / ConfirmedKeys.
        internal static void Capture(SchematicOwnershipSnapshot result,
            Dictionary<Book, SchematicOwnershipTraceRecord> byBook,
            Dictionary<string, Book> byId,
            Book book, Thing outer, string projectFullKey, string source)
        {
            if (result == null || book == null || string.IsNullOrEmpty(projectFullKey)) return;
            SchematicOwnershipTraceRecord record;
            if (byBook.TryGetValue(book, out record))
            {
                if (!string.Equals(record.ProjectFullKey, projectFullKey, StringComparison.Ordinal))
                    result.TraceFailed = true;
                record.AddSource(source);
                return;
            }

            string id = book.ThingID;
            if (string.IsNullOrEmpty(id))
            {
                result.TraceMissingIds++;
                return;
            }
            Book existing;
            if (byId.TryGetValue(id, out existing) && !object.ReferenceEquals(existing, book))
            {
                result.TraceIdCollisions++;
                result.TraceFailed = true;
                // A repeated ID is not valid for cross-session object comparison.
                result.TraceBooks.Remove(id);
                return;
            }
            byId[id] = book;
            string outerId = outer != null && !object.ReferenceEquals(outer, book)
                ? outer.ThingID : null;
            string label;
            try { label = book.LabelCap == null ? "" : book.LabelCap.ToString(); }
            catch (Exception) { label = "TITLE_UNAVAILABLE"; }
            record = new SchematicOwnershipTraceRecord(id, projectFullKey, label, outerId);
            record.AddSource(source);
            byBook.Add(book, record);
            result.TraceBooks[id] = record;
        }

        // Pure reference-free data checks used by the startup diagnostic audit.
        internal static bool CanDiff(SchematicOwnershipSnapshot before, SchematicOwnershipSnapshot after)
        {
            return before != null && after != null &&
                !before.Truncated && !after.Truncated &&
                !before.InspectionFailed && !after.InspectionFailed &&
                !before.TraceFailed && !after.TraceFailed &&
                before.TraceMissingIds == 0 && after.TraceMissingIds == 0 &&
                before.TraceIdCollisions == 0 && after.TraceIdCollisions == 0 &&
                !before.TraceCountMismatch && !after.TraceCountMismatch;
        }

        internal static bool SameIdentity(SchematicOwnershipTraceRecord left,
            SchematicOwnershipTraceRecord right)
        {
            return left != null && right != null &&
                string.Equals(left.BookThingId, right.BookThingId, StringComparison.Ordinal) &&
                string.Equals(left.ProjectFullKey, right.ProjectFullKey, StringComparison.Ordinal);
        }
    }
}
