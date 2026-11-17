using RimWorld;
using UnityEngine;
using Verse;

namespace ZCN.QoL.EasyReading
{
    internal enum BookContentsIconSource
    {
        None,
        Textbook,
        Schematic
    }

    /// <summary>
    /// S3-A1: immutable per-row draw plan shared by the title hook and postfix.
    /// Textbook keeps exactly the pre-S3-A1 two-slot coordinates; Schematic
    /// reserves only one slot and only when its texture is actually available.
    /// No Harmony patches, drawing, game-state writes or long-lived book cache.
    /// </summary>
    internal struct BookContentsIconLayout
    {
        private const float EjectWidth = 24f;
        private const float LabelStartX = 60f;
        private const float IconStartPadding = 3f;
        private const float SlotGap = 2f;
        private const float TitleGap = 5f;
        private const float MinimumTitleWidth = 65f;

        internal readonly BookContentsIconSource Source;
        internal readonly SkillDef FirstSkill;
        internal readonly SkillDef SecondSkill;
        internal readonly Texture2D SchematicTexture;
        internal readonly SchematicIconKind SchematicKind;
        internal readonly SchematicResearchClassification Classification;
        internal readonly int FinishedCount;
        internal readonly int ProjectCount;
        internal readonly int SlotCount;
        internal readonly float IconX;
        internal readonly float Slot;
        internal readonly float Size;
        internal readonly int Position;

        private BookContentsIconLayout(BookContentsIconSource source,
            SkillDef first, SkillDef second, Texture2D schematicTexture,
            SchematicIconKind schematicKind, SchematicResearchClassification classification,
            int finishedCount, int projectCount, int slotCount, float x,
            float slot, float size, int position)
        {
            Source = source;
            FirstSkill = first;
            SecondSkill = second;
            SchematicTexture = schematicTexture;
            SchematicKind = schematicKind;
            Classification = classification;
            FinishedCount = finishedCount;
            ProjectCount = projectCount;
            SlotCount = slotCount;
            IconX = x;
            Slot = slot;
            Size = size;
            Position = position;
        }

        internal Rect AdjustTitle(Rect rect)
        {
            if (Position == 1)
                rect.xMax = IconX - TitleGap;
            else
                rect.xMin = IconX + SlotCount * Slot + TitleGap;
            return rect;
        }

        internal static bool TryCreate(Book book, Rect titleRect,
            EasyReadingSettings settings, out BookContentsIconLayout plan,
            out string suppressionReason)
        {
            plan = default(BookContentsIconLayout);
            suppressionReason = null;
            if (book == null || settings == null) return false;

            BookContentsIconSource source;
            SkillDef first = null;
            SkillDef second = null;
            Texture2D texture = null;
            SchematicIconKind kind = SchematicIconKind.ResearchAvailable;
            SchematicResearchClassification classification =
                SchematicResearchClassification.NotApplicable;
            int finished = 0;
            int projects = 0;
            int slots;

            if (SchematicBookcaseIconResolver.IsSchematic(book))
            {
                if (!settings.showSchematicBookcaseIcons) return false;
                if (!SchematicBookcaseIconResolver.TryResolve(book, out kind,
                    out finished, out projects, out classification))
                {
                    suppressionReason = classification.ToString();
                    return false;
                }
                texture = SchematicStatusIconCatalog.GetTexture(kind);
                if (texture == null)
                {
                    suppressionReason = "MissingTexture";
                    return false;
                }
                source = BookContentsIconSource.Schematic;
                slots = 1;
            }
            else
            {
                if (!settings.showBookSkillIcons) return false;
                BookIconPresentation bookSkills = BookIconResolver.ResolveTextbook(book);
                first = bookSkills.FirstSkill;
                second = bookSkills.SecondSkill;
                if (first == null) return false;
                source = BookContentsIconSource.Textbook;
                // Keep the original 2-slot reservation even with only one skill.
                slots = 2;
            }

            float size = BookIconMetrics.IconSize(settings.bookSkillIconSizePercent);
            float slot = size + SlotGap;
            float labelEnd = titleRect.xMax;
            // The original layout used (rect.xMax + EjectWidth) - EjectWidth.
            // Retain the identical arithmetic and the vanilla 60px left anchor.
            float rowWidth = labelEnd + EjectWidth;
            float x = settings.bookSkillIconPosition == 1
                ? rowWidth - EjectWidth - slots * slot - IconStartPadding
                : LabelStartX + IconStartPadding;
            float available = settings.bookSkillIconPosition == 1
                ? x - TitleGap - LabelStartX
                : labelEnd - (x + slots * slot + TitleGap);
            if (available < MinimumTitleWidth)
            {
                if (source == BookContentsIconSource.Schematic)
                    suppressionReason = "NoSpace";
                return false;
            }
            plan = new BookContentsIconLayout(source, first, second, texture,
                kind, classification, finished, projects, slots, x, slot,
                size, settings.bookSkillIconPosition);
            return true;
        }
    }
}
