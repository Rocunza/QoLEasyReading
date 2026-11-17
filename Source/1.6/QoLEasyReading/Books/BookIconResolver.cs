using RimWorld;
using Verse;

namespace ZCN.QoL.EasyReading
{
    /// <summary>
    /// R2-A: A single non-UI entry point for the existing textbook skill markers.
    /// Delegates to the original catalog so selection, order, and null behavior
    /// remain unchanged. Schematic evaluation is NOT introduced at this stage.
    /// </summary>
    internal static class BookIconResolver
    {
        internal static BookIconPresentation ResolveTextbook(Book book)
        {
            SkillDef first;
            SkillDef second;
            BookSkillIconCatalog.GetBookSkills(book, out first, out second);
            return new BookIconPresentation(first, second);
        }
    }
}
