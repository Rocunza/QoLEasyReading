using System;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using HarmonyLib;
using RimWorld;
using UnityEngine;
using Verse;

namespace ZCN.QoL.EasyReading
{
    internal static partial class BookTradeSkillIcons
    {
        [HarmonyPostfix]
        private static void Postfix(RowState __state)
        {
            // Clear the title shift before painting tooltips/icons, avoiding
            // any spillover to later GUI rows or unrelated label rendering.
            activeLeftTitleShift = 0f;
            if (!__state.Paint || Event.current == null)
                return;
            try
            {
                if (__state.Schematic)
                {
                    // Never gate painting behind diagnostic logging.
                    bool painted = SchematicTradeIconRenderer.Paint(__state.SchematicTexture,
                        __state.FirstIcon);
#if DEBUG
                    if (painted && EasyReadingDebug.Enabled &&
                        LoggedSchematicPaints.Add(__state.SchematicKind))
                    {
                        Log.Message("[QoL: Easy Reading][S3B2][TradeIcon][PAINT] kind=" +
                            __state.SchematicKind + "; position=" +
                            (__state.Left ? "Left" : "Right") +
                            "; slots=1; originalControlsUntouched=True; BUILD_ID=" +
                            EasyReadingBuildIdentity.BuildId + ";");
                    }
#endif
                    if (__state.SchematicKind == SchematicIconKind.DuplicateOwned &&
                        __state.SchematicTexture != null)
                        SchematicTradeDuplicateTooltip.TryRegister(__state.FirstIcon,
                            __state.DuplicateProjectKey, __state.DuplicateOwnershipSnapshot);
                    return;
                }
                PaintOne(__state.First, __state.FirstIcon);
                if (__state.Second != null)
                    PaintOne(__state.Second, __state.SecondIcon);
#if DEBUG
                if (EasyReadingDebug.Enabled && !LoggedFirstPaint &&
                    Event.current.type == EventType.Repaint)
                {
                    LoggedFirstPaint = true;
                    Log.Message("[QoL: Easy Reading][TradeUI] First trade textbook icons " +
                        "painted: " + __state.First.defName +
                        (__state.Second == null ? "" : ", " + __state.Second.defName) +
                        "; position=" + (__state.Left ? "Left" : "Right") +
                        "; titleReserved=True; input/price/count untouched=True.");
                }
#endif
            }
            catch (Exception ex)
            {
                Log.ErrorOnce("[QoL: Easy Reading][TradeUI] Skill painting skipped: " + ex,
                    1726194621);
            }
        }

        private static void PaintOne(SkillDef skill, Rect rect)
        {
            if (skill == null)
                return;
            Texture2D texture = BookSkillIconCatalog.GetTexture(skill);
            if (texture != null && Event.current.type == EventType.Repaint)
            {
                Color oldColor = GUI.color;
                try
                {
                    GUI.color = Color.white;
                    GUI.DrawTexture(rect, texture);
                }
                finally
                {
                    GUI.color = oldColor;
                }
            }
            // Passive decoration only. Preserve vanilla/trading book tooltips;
            // no additional tooltip or mouse/key handling is registered here.
        }
    }
}
