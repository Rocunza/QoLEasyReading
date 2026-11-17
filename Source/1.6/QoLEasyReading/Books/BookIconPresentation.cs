using RimWorld;
using Verse;

namespace ZCN.QoL.EasyReading
{
    /// <summary>
    /// R2-A: Allocation-free description of the existing textbook skill markers.
    /// Does not own textures, GUI state, or gameplay state.
    /// Default value represents no markers, matching GetBookSkills(null, ...).
    /// </summary>
    internal struct BookIconPresentation
    {
        internal readonly SkillDef FirstSkill;
        internal readonly SkillDef SecondSkill;

        internal BookIconPresentation(SkillDef firstSkill, SkillDef secondSkill)
        {
            FirstSkill = firstSkill;
            SecondSkill = secondSkill;
        }

        internal bool HasSkillIcons
        {
            get { return FirstSkill != null; }
        }

        internal int SkillIconCount
        {
            get { return FirstSkill == null ? 0 : (SecondSkill == null ? 1 : 2); }
        }
    }
}
