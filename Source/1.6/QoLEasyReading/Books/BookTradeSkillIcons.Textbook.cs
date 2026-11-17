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
        private static bool TryGetUniformSkills(Tradeable tradeable,
            out SkillDef first, out SkillDef second)
        {
            first = null;
            second = null;
            Book sample = tradeable.AnyThing as Book;
            if (sample == null)
                return false;
            BookIconPresentation samplePresentation = BookIconResolver.ResolveTextbook(sample);
            first = samplePresentation.FirstSkill;
            second = samplePresentation.SecondSkill;
            if (first == null)
                return false; // Ordinary books remain entirely vanilla.

            bool matching = GroupMatches(tradeable.thingsColony, first, second) &&
                GroupMatches(tradeable.thingsTrader, first, second);
#if DEBUG
            if (!matching && EasyReadingDebug.Enabled && !LoggedMixedGroup &&
                Event.current.type == EventType.Repaint)
            {
                LoggedMixedGroup = true;
                Log.Warning("[QoL: Easy Reading][TradeUI] Mixed book rewards in " +
                    "one Tradeable; icons deliberately hidden for accuracy.");
            }
#endif
            return matching;
        }

        private static bool GroupMatches(List<Thing> group,
            SkillDef expectedFirst, SkillDef expectedSecond)
        {
            if (group == null)
                return true;
            for (int i = 0; i < group.Count; i++)
            {
                Thing thing = group[i];
                if (thing == null)
                    return false;
                Book book = thing.GetInnerIfMinified() as Book;
                if (book == null)
                    return false;
                BookIconPresentation presentation = BookIconResolver.ResolveTextbook(book);
                SkillDef first = presentation.FirstSkill;
                SkillDef second = presentation.SecondSkill;
                if (!((first == expectedFirst && second == expectedSecond) ||
                      (expectedSecond != null && first == expectedSecond &&
                       second == expectedFirst)))
                    return false;
            }
            return true;
        }

    }
}
