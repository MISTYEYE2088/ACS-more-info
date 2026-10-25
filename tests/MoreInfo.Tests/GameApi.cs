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
        public string DisplayName;
        public string DisplayValue;
        public string DisplayValueRelative;
    }

    public class ModifierPropertyData
    {
        public string Name;
        public float AddV;
        public float AddP;
        public float BAddV;
        public float BAddP;
    }

    public class ModifierDef
    {
        public readonly List<ModifierPropertyData> Properties = new List<ModifierPropertyData>();
        public readonly List<string> NativeLines = new List<string>();

        [MethodImpl(MethodImplOptions.NoInlining)]
        public List<string> GetSimpleDesc(float scale = 1f)
        {
            return new List<string>(NativeLines);
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

        public readonly Dictionary<string, PropertyDef> Defs = new Dictionary<string, PropertyDef>();

        public PropertyDef GetDef(string name)
        {
            PropertyDef def;
            return Defs.TryGetValue(name, out def) ? def : null;
        }
    }

    public class TFMgr
    {
        public static TFMgr Instance = new TFMgr();

        public string Language { get; set; }
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
