using System;
using System.Text;
using RimWorld;
using Verse;

namespace ZCN.QoL.EasyReading
{
    /// <summary>
    /// Read-only bridge to the game's own per-research benefit text.
    /// No research status is inferred from the text; the status resolver
    /// remains the only owner of ? / checkmark classification.
    /// This formatter is independent of FloatMenu and can be reused by trade UI.
    /// </summary>
    internal static class SchematicResearchBenefitTooltip
    {
        internal static bool TryBuild(Book book, out string tooltip, out string reason)
        {
            tooltip = null;
            reason = string.Empty;
            if (book == null) { reason = "BookNull"; return false; }
            if (!SchematicStatusIconResolver.IsSchematic(book))
            {
                reason = "NotSchematic";
                return false;
            }

            try
            {
                CompBook comp = book.BookComp;
                if (comp == null || comp.Doers == null)
                {
                    reason = "BookDoersMissing";
                    return false;
                }

                StringBuilder lines = new StringBuilder();
                bool foundResearchDoer = false;
                foreach (BookOutcomeDoer doer in comp.Doers)
                {
                    ReadingOutcomeDoerGainResearch researchDoer =
                        doer as ReadingOutcomeDoerGainResearch;
                    if (researchDoer == null)
                        continue;

                    foundResearchDoer = true;
                    // Verified against RimWorld 1.6.4871 Assembly-CSharp.dll:
                    // public override string GetBenefitsString(Pawn pawn).
                    // Vanilla's implementation does not read the Pawn argument.
                    // Preserve the native localized lines, including prefixes,
                    // already-unlocked notes, hidden-project notes and colors.
                    string value = researchDoer.GetBenefitsString((Pawn)null);
                    if (string.IsNullOrEmpty(value))
                        continue;
                    if (lines.Length > 0)
                        lines.AppendLine();
                    lines.Append(value);
                }

                if (!foundResearchDoer)
                {
                    reason = "NoGainResearchDoer";
                    return false;
                }
                if (lines.Length == 0)
                {
                    reason = "VanillaBenefitTextEmpty";
                    return false;
                }

                tooltip = lines.ToString();
                return true;
            }
            catch (Exception ex)
            {
                // A foreign Harmony patch may expect a non-null Pawn or throw.
                // Do not propagate an exception into Book.GetFloatMenuOptions.
                reason = "VanillaBenefitInvocation:" + ex.GetType().Name;
                return false;
            }
        }
    }
}
