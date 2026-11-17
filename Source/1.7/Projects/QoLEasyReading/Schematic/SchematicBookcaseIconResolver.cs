using RimWorld;
using Verse;

namespace ZCN.QoL.EasyReading
{
    /// <summary>
    /// S3-A2: preserve the S3-A1 bookcase API and exact result mapping while
    /// sharing classification with other UI surfaces. No state or book cache.
    /// </summary>
    internal static class SchematicBookcaseIconResolver
    {
        internal static bool IsSchematic(Book book)
        {
            return SchematicStatusIconResolver.IsSchematic(book);
        }

        internal static bool TryResolve(Book book, out SchematicIconKind kind,
            out int finishedCount, out int projectCount,
            out SchematicResearchClassification classification)
        {
            return SchematicStatusIconResolver.TryResolve(book, out kind,
                out finishedCount, out projectCount, out classification);
        }
    }
}
