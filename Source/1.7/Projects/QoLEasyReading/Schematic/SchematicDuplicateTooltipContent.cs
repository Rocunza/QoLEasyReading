using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using Verse;

namespace ZCN.QoL.EasyReading
{
    /// <summary>
    /// S3-C2-T1: read-only, fail-closed naming of already verified player-owned
    /// Schematic books. Never scans game objects or changes ownership decisions.
    /// The caller may show a duplicate warning even when names are unavailable.
    /// </summary>
    internal static class SchematicDuplicateTooltipContent
    {
        private const int MaxDistinctTitles = 3;
        private const int MaxTitleCharacters = 144;
        private const int MaxTooltipCharacters = 900;
        // Limit work on malformed/hostile foreign labels. Styled text is removed
        // before counting the 144 visible characters, not after truncation.
        private const int MaxTitleScanCharacters = 8192;
        private const int MaxStyleTagCharacters = 48;

        internal sealed class Result
        {
            internal readonly string Text;
            internal readonly int DistinctTitles;
            internal readonly int VerifiedBooks;
            internal readonly string Status;
            internal Result(string text, int distinct, int books, string status)
            {
                Text = text;
                DistinctTitles = distinct;
                VerifiedBooks = books;
                Status = status;
            }
        }

        private sealed class NameCount
        {
            internal string Name;
            internal int Count;
            internal NameCount(string name, int count) { Name = name; Count = count; }
        }

        internal static Result Build(string fullKey, SchematicOwnershipSnapshot snapshot)
        {
            // The live GUI is the only caller allowed to resolve RimWorld translations.
            // The startup self-test below MUST NOT call Translate before activeLanguage exists.
            return BuildCore(fullKey, snapshot, EasyReadingI18n.Get, EasyReadingI18n.Format);
        }

        private static Result BuildCore(string fullKey, SchematicOwnershipSnapshot snapshot,
            Func<string, string> getText, Func<string, object[], string> formatText)
        {
            string body = getText("ER_DuplicateTooltipTitle") + "\n" +
                getText("ER_DuplicateTooltipBody");
            if (snapshot == null || string.IsNullOrEmpty(fullKey) ||
                snapshot.GetDecision(fullKey) != SchematicOwnershipDecision.Owned)
                return new Result(body, 0, 0, "NO_VERIFIED_OWNERSHIP");
            // The result may still prove 'Owned', while its metadata trail is
            // incomplete. Never invent names, counts or precise ownership then.
            if (snapshot.TraceFailed || snapshot.TraceCountMismatch ||
                snapshot.TraceMissingIds != 0 || snapshot.TraceIdCollisions != 0)
                return new Result(body, 0, 0, "TRACE_UNRELIABLE");

            Dictionary<string, int> counts = new Dictionary<string, int>(StringComparer.Ordinal);
            HashSet<string> ids = new HashSet<string>(StringComparer.Ordinal);
            int verified = 0;
            foreach (KeyValuePair<string, SchematicOwnershipTraceRecord> entry in snapshot.TraceBooks)
            {
                SchematicOwnershipTraceRecord rec = entry.Value;
                if (rec == null || !rec.HasVerifiedSource ||
                    !string.Equals(rec.ProjectFullKey, fullKey, StringComparison.Ordinal) ||
                    string.IsNullOrEmpty(rec.BookThingId) ||
                    !ids.Add(rec.BookThingId))
                    continue;
                string name = CleanTitle(rec.Title);
                if (string.IsNullOrEmpty(name))
                    continue;
                int oldCount;
                counts.TryGetValue(name, out oldCount);
                counts[name] = oldCount + 1;
                verified++;
            }
            if (verified == 0)
                return new Result(body, 0, 0, "VERIFIED_NAMES_MISSING");

            List<NameCount> titles = new List<NameCount>(counts.Count);
            foreach (KeyValuePair<string, int> entry in counts)
                titles.Add(new NameCount(entry.Key, entry.Value));
            titles.Sort(delegate(NameCount a, NameCount b)
            {
                int compare = StringComparer.OrdinalIgnoreCase.Compare(a.Name, b.Name);
                return compare == 0 ? StringComparer.Ordinal.Compare(a.Name, b.Name) : compare;
            });

            StringBuilder text = new StringBuilder(body);
            text.Append("\n\n").Append(getText("ER_DuplicateTooltipOwnedHeader"));
            int shown = Math.Min(titles.Count, MaxDistinctTitles);
            for (int i = 0; i < shown; i++)
            {
                NameCount title = titles[i];
                text.Append("\n• ").Append(title.Name);
                if (title.Count > 1)
                    text.Append(" ×").Append(title.Count);
            }
            if (titles.Count > shown)
                text.Append("\n").Append(formatText("ER_DuplicateTooltipMoreTitles", new object[] {
                    titles.Count - shown }));
            // A long foreign/mod-generated title must never create a huge UI panel.
            string value = text.ToString();
            if (value.Length > MaxTooltipCharacters)
                value = value.Substring(0, MaxTooltipCharacters - 1) + "…";
            return new Result(value, titles.Count, verified, "VERIFIED_TITLES");
        }

        private static string CleanTitle(string source)
        {
            if (string.IsNullOrWhiteSpace(source) ||
                string.Equals(source, "TITLE_UNAVAILABLE", StringComparison.Ordinal))
                return "";
            // S3-QC-HF1: labels can contain Quality Colors/Unity rich-text tags.
            // Strip only recognized presentation tags; never strip arbitrary
            // <...> text from real titles or send untrusted markup to the GUI.
            string trimmed = source.Trim();
            StringBuilder sb = new StringBuilder(Math.Min(trimmed.Length, MaxTitleCharacters + 1));
            int scanLength = Math.Min(trimmed.Length, MaxTitleScanCharacters);
            for (int i = 0; i < scanLength; i++)
            {
                int nextIndex;
                if (trimmed[i] == '<' && TrySkipStyleTag(trimmed, i, scanLength, out nextIndex))
                {
                    i = nextIndex - 1;
                    continue;
                }

                if (sb.Length >= MaxTitleCharacters)
                {
                    sb.Append('…');
                    break;
                }
                char c = trimmed[i];
                if (c == '<') c = '‹';
                if (c == '>') c = '›';
                if (char.IsControl(c)) c = ' ';
                sb.Append(c);
            }
            if (trimmed.Length > scanLength && sb.Length <= MaxTitleCharacters)
                sb.Append('…');
            return sb.ToString().Trim();
        }

        // This deliberately does not invoke Quality Colors or any RimWorld API.
        // The only recognized tags are Unity color, bold, and italic formatting;
        // unknown or malformed brackets remain visible as harmless characters.
        private static bool TrySkipStyleTag(string text, int start, int scanLength, out int nextIndex)
        {
            nextIndex = start;
            int close = -1;
            int limit = Math.Min(scanLength, start + MaxStyleTagCharacters);
            for (int pos = start + 1; pos < limit; pos++)
            {
                if (text[pos] == '<') return false;
                if (text[pos] == '>') { close = pos; break; }
            }
            if (close < 0) return false;

            string tag = text.Substring(start + 1, close - start - 1);
            bool known = string.Equals(tag, "b", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(tag, "/b", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(tag, "i", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(tag, "/i", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(tag, "/color", StringComparison.OrdinalIgnoreCase) ||
                (tag.StartsWith("color=", StringComparison.OrdinalIgnoreCase) &&
                    IsRecognizedColor(tag.Substring(6)));
            if (!known) return false;
            nextIndex = close + 1;
            return true;
        }

        private static bool IsRecognizedColor(string value)
        {
            if (string.IsNullOrEmpty(value)) return false;
            if (value[0] == '#')
            {
                int digits = value.Length - 1;
                if (digits != 3 && digits != 4 && digits != 6 && digits != 8)
                    return false;
                for (int i = 1; i < value.Length; i++)
                {
                    char c = value[i];
                    if (!((c >= '0' && c <= '9') ||
                          (c >= 'A' && c <= 'F') || (c >= 'a' && c <= 'f')))
                        return false;
                }
                return true;
            }
            // Unity's conventional named colors. Do not treat arbitrary
            // <color=foreign-label> text as formatting.
            switch (value.ToLowerInvariant())
            {
                case "red": case "green": case "blue": case "yellow":
                case "black": case "white": case "cyan": case "magenta":
                case "grey": case "gray": case "orange": case "purple":
                case "brown": case "clear":
                    return true;
                default:
                    return false;
            }
        }

#if DEBUG
        // Pure fixtures: never access RimWorld's translator from the mod constructor.
        // This also lets startup tests run before the user's active language is loaded.
        private static string TestText(string key)
        {
            switch (key)
            {
                case "ER_DuplicateTooltipTitle": return "Duplicate schematic";
                case "ER_DuplicateTooltipBody": return "Same research already owned.";
                case "ER_DuplicateTooltipOwnedHeader": return "Owned schematics:";
                case "ER_DuplicateTooltipMoreTitles": return "+{0} more titles";
                default: throw new ArgumentException("Unknown tooltip fixture key: " + key);
            }
        }

        private static string TestFormat(string key, object[] args)
        {
            return string.Format(CultureInfo.CurrentCulture, TestText(key), args);
        }

        internal static void RunPureSelfTests()
        {
            int checks = 0, failures = 0;
            Action<bool> check = delegate(bool ok) { checks++; if (!ok) failures++; };
            // Exercises the exact same production text/grouping logic with fixed
            // strings, without touching LanguageDatabase or Translate().
            Func<string, SchematicOwnershipSnapshot, Result> testBuild =
                delegate(string k, SchematicOwnershipSnapshot s)
                {
                    return BuildCore(k, s, TestText, TestFormat);
                };
            const string key = "Autodoors";
            SchematicOwnershipSnapshot data = new SchematicOwnershipSnapshot();
            data.ConfirmedKeys[key] = 1;
            SchematicOwnershipTraceRecord a = new SchematicOwnershipTraceRecord("id1", key, "Archaic Autodoors", null);
            a.AddSource("TRADE_COLONY");
            SchematicOwnershipTraceRecord b = new SchematicOwnershipTraceRecord("id2", key, "Archaic Autodoors", null);
            b.AddSource("PLAYER_MAP_HOLDER");
            SchematicOwnershipTraceRecord c = new SchematicOwnershipTraceRecord("id3", key, "Reverie and Autodoors", null);
            c.AddSource("TRADE_COLONY");
            SchematicOwnershipTraceRecord loose = new SchematicOwnershipTraceRecord("id4", key, "Visitor book", null);
            loose.AddSource("HOME_MAP_LOOSE");
            SchematicOwnershipTraceRecord foreign = new SchematicOwnershipTraceRecord("id5", "Other", "Foreign project", null);
            foreign.AddSource("TRADE_COLONY");
            data.TraceBooks.Add(a.BookThingId, a);
            data.TraceBooks.Add(b.BookThingId, b);
            data.TraceBooks.Add(c.BookThingId, c);
            data.TraceBooks.Add(loose.BookThingId, loose);
            data.TraceBooks.Add(foreign.BookThingId, foreign);
            Result r = testBuild(key, data);
            check(r.DistinctTitles == 2);
            check(r.VerifiedBooks == 3);
            check(r.Text.Contains("Archaic Autodoors ×2"));
            check(r.Text.Contains("Reverie and Autodoors"));
            check(!r.Text.Contains("Visitor book"));
            check(!r.Text.Contains("Foreign project"));
            check(r.Status == "VERIFIED_TITLES");
            check(testBuild("Other", data).DistinctTitles == 0);
            check(testBuild(null, data).DistinctTitles == 0);
            check(testBuild(key, null).DistinctTitles == 0);
            data.TraceFailed = true;
            check(testBuild(key, data).DistinctTitles == 0);
            data.TraceFailed = false;
            data.TraceCountMismatch = true;
            check(testBuild(key, data).DistinctTitles == 0);
            data.TraceCountMismatch = false;
            data.TraceIdCollisions = 1;
            check(testBuild(key, data).DistinctTitles == 0);
            data.TraceIdCollisions = 0;
            data.TraceMissingIds = 1;
            check(testBuild(key, data).DistinctTitles == 0);
            data.TraceMissingIds = 0;
            SchematicOwnershipTraceRecord d = new SchematicOwnershipTraceRecord("id6", key, "A third title", null);
            d.AddSource("TRADE_COLONY");
            SchematicOwnershipTraceRecord e = new SchematicOwnershipTraceRecord("id7", key, "A fourth title", null);
            e.AddSource("TRADE_COLONY");
            data.TraceBooks.Add(d.BookThingId, d);
            data.TraceBooks.Add(e.BookThingId, e);
            r = testBuild(key, data);
            check(r.DistinctTitles == 4);
            check(r.Text.Contains("A fourth title"));
            check(r.Text.Contains("A third title"));
            check(r.Text.Contains("Archaic Autodoors ×2"));
            check(!r.Text.Contains("Reverie and Autodoors"));
            check(r.Text.Contains(TestFormat("ER_DuplicateTooltipMoreTitles", new object[] { 1 })));
            check(!r.Text.Contains("Visitor book"));
            check(r.VerifiedBooks == 5);
            // S3-QC-HF1 fixtures: real Quality Colors labels, nested Unity
            // formatting, malformed markup and visible-character truncation.
            check(CleanTitle("<b>Title</b>\n") == "Title");
            check(CleanTitle("<i>Title</i>") == "Title");
            check(CleanTitle("Archaic Autodoors Vol. I (<color=#FF0000>빈약</color>)") ==
                "Archaic Autodoors Vol. I (빈약)");
            check(CleanTitle("(<color=#9E6700>빈약</color>)") == "(빈약)");
            check(CleanTitle("<COLOR=#00ff00>Book</COLOR>") == "Book");
            check(CleanTitle("<color=#12A>Book</color>") == "Book");
            check(CleanTitle("<color=#12AF>Book</color>") == "Book");
            check(CleanTitle("<color=#11223344>Book</color>") == "Book");
            check(CleanTitle("<color=red>Book</color>") == "Book");
            check(CleanTitle("<color=#123456><b>Book</b></color>") == "Book");
            check(CleanTitle("</color>Book") == "Book");
            check(CleanTitle("Book (빈약)") == "Book (빈약)");
            check(CleanTitle("A < B > C") == "A ‹ B › C");
            check(CleanTitle("<mod_tag>Book</mod_tag>") == "‹mod_tag›Book‹/mod_tag›");
            check(CleanTitle("<color=other>Book</color>") == "‹color=other›Book");
            check(CleanTitle("<color=#GGGGGG>Book</color>") == "‹color=#GGGGGG›Book");
            check(CleanTitle("<color=#FF0000Book") == "‹color=#FF0000Book");
            check(CleanTitle("Book\nName\r\t") == "Book Name");
            check(CleanTitle("TITLE_UNAVAILABLE") == "");
            check(CleanTitle("  ") == "");
            check(CleanTitle(new string('x', 144)) == new string('x', 144));
            check(CleanTitle(new string('x', 145)) == new string('x', 144) + "…");
            check(CleanTitle("<color=#FF0000>" + new string('x', 144) + "</color>") ==
                new string('x', 144));
            check(CleanTitle("<color=#FF0000>" + new string('x', 145) + "</color>") ==
                new string('x', 144) + "…");
            check(CleanTitle(new string('<', 10000)).Length <= MaxTitleCharacters + 1);
            check(CleanTitle(new string('x', 300)).Length <= MaxTitleCharacters + 1);

            SchematicOwnershipSnapshot qcData = new SchematicOwnershipSnapshot();
            qcData.ConfirmedKeys[key] = 1;
            SchematicOwnershipTraceRecord qcA = new SchematicOwnershipTraceRecord(
                "qc1", key, "Autodoors (<color=#FF0000>빈약</color>)", null);
            qcA.AddSource("TRADE_COLONY");
            SchematicOwnershipTraceRecord qcB = new SchematicOwnershipTraceRecord(
                "qc2", key, "Autodoors (<color=#9E6700>빈약</color>)", null);
            qcB.AddSource("PLAYER_MAP_HOLDER");
            qcData.TraceBooks.Add(qcA.BookThingId, qcA);
            qcData.TraceBooks.Add(qcB.BookThingId, qcB);
            Result qcResult = testBuild(key, qcData);
            check(qcResult.Status == "VERIFIED_TITLES");
            check(qcResult.DistinctTitles == 1);
            check(qcResult.VerifiedBooks == 2);
            check(qcResult.Text.Contains("Autodoors (빈약) ×2"));
            check(!qcResult.Text.Contains("<color") && !qcResult.Text.Contains("‹color"));
            check(qcResult.Text.Length <= MaxTooltipCharacters);
            Log.Message("[QoL: Easy Reading][S3C2T1][PURE_AUDIT] status=" +
                (failures == 0 ? "PASS" : "FAIL") + "; checks=" + checks +
                "; failures=" + failures + "; debugIndependent=True; " +
                "noScansOrGameWrites=True; colorMarkup=STRIPPED; " +
                "plainTitle=PRESERVED; build=" + EasyReadingBuildIdentity.BuildId + ";");
        }
#endif
    }
}
