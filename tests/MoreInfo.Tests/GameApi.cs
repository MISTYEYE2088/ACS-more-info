using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace XiaWorld
{
    public enum g_emEsotericaUnitKind
    {
        None,
        Property,
        SkillExp,
        Flag,
        FiveBase,
        FightSkill,
        Dan,
        Spell,
        Refining
    }

    public class Npc
    {
    }

    public class EsotericaData
    {
        public float Value;
    }

    public class PropertyDef
    {
        public string Name;
    }

    public class ModifierDef
    {
        public readonly List<string> Lines = new List<string>();

        [MethodImpl(MethodImplOptions.NoInlining)]
        public List<string> GetSimpleDesc(float scale = 1f)
        {
            return new List<string>(Lines);
        }
    }

    public class ModifierMgr
    {
        public static ModifierMgr Instance { get; set; }

        public readonly Dictionary<string, ModifierDef> Defs = new Dictionary<string, ModifierDef>();

        public ModifierDef GetDef(string name)
        {
            ModifierDef def;
            return Defs.TryGetValue(name, out def) ? def : null;
        }
    }

    public class PropertyMgr
    {
        public static PropertyMgr Instance { get; set; }

        public PropertyDef GetDef(string name)
        {
            return name == "TestProp" ? new PropertyDef { Name = name } : null;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public string GetPropertyAddtionDesc(string name, Npc npc, float addv = 0f, float addp = 0f, float baddv = 0f, float baddp = 0f)
        {
            float value = addv != 0f ? addv : addp;
            return (value >= 0f ? "+" : "") + value.ToString("0.00") + "  测试属性";
        }
    }

    public class EsotericaDef
    {
        public string Modifier;
        public string EffectDesc;

        [MethodImpl(MethodImplOptions.NoInlining)]
        public virtual string GetEffectDesc(EsotericaData data, Npc npc = null)
        {
            return EffectDesc;
        }
    }

    public class EsotericaTemplateData : EsotericaDef
    {
        public g_emEsotericaUnitKind Kind;
        public string PName;
        public bool IsAddv;

        [MethodImpl(MethodImplOptions.NoInlining)]
        public override string GetEffectDesc(EsotericaData data, Npc npc = null)
        {
            if (Kind == g_emEsotericaUnitKind.Property)
            {
                return "大量的" + PName;
            }
            return EffectDesc;
        }
    }
}
