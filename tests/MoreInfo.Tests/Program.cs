using System;
using System.Globalization;
using XiaWorld;

namespace MoreInfo.Tests
{
    internal static class Program
    {
        private static int failures;
        private static PropertyMgr properties;

        private static void Main()
        {
            CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;

            TFMgr.Instance = new TFMgr { Language = "cn" };

            properties = new PropertyMgr();
            properties.Defs["NpcLingMaxValue"] = new PropertyDef
            {
                DisplayName = "灵气最大值",
                DisplayValue = "{0:N2}"
            };
            properties.Defs["NpcFight_SpellPowerAddP"] = new PropertyDef
            {
                DisplayName = "术法威力加成",
                DisplayValue = "{0:P}"
            };
            properties.Defs["MaxAge"] = new PropertyDef
            {
                DisplayName = "寿元",
                DisplayValue = "{0}年"
            };
            properties.Defs["TestProp"] = new PropertyDef
            {
                DisplayName = "测试属性",
                DisplayValue = "{0:N2}"
            };
            PropertyMgr.Instance = properties;

            ModifierMgr modifiers = new ModifierMgr();
            ModifierMgr.Instance = modifiers;

            AddModifier("AddOnly", "+40.00  灵气最大值", "+2  气等级").Add("NpcLingMaxValue").AddV = 40f;
            AddModifier("Gushen", "+12,500.00  灵气最大值").Add("NpcLingMaxValue").AddP = 25f;
            AddModifier("Jiexin", "-120年  寿元", "+0.00  灵气最大值", "+50%  悟性")
                .Add("MaxAge").AddV = -120f;
            modifiers.Defs["Jiexin"].Properties.Add(new ModifierPropertyData
            {
                Name = "NpcLingMaxValue",
                BAddP = -0.1f
            });
            AddModifier("Yuzhen", "+10.00%  术法威力加成").Add("NpcFight_SpellPowerAddP").BAddP = 0.2f;

            AddModifier("Four", "+5.00  测试属性").Properties.Add(new ModifierPropertyData
            {
                Name = "TestProp",
                AddV = 5f,
                AddP = 0.1f,
                BAddV = 10f,
                BAddP = 0.2f
            });

            AddModifier("Unknown", "+1.00  Missing").Add("Missing").AddV = 1f;
            AddModifier("BaseFiveOnly", "+50%  悟性");

            MoreInfo.OnInit();

            EsotericaDef addOnly = new EsotericaDef { Modifier = "AddOnly", EffectDesc = "原描述" };
            string addOnlyResult = addOnly.GetEffectDesc(null);
            Check("AddV uses 总值加值 and keeps native skill lines",
                addOnlyResult.StartsWith("原描述\n")
                && addOnlyResult.Contains("灵气最大值（总值加值）+40.00")
                && addOnlyResult.Contains("+2  气等级")
                && !addOnlyResult.Contains("+40.00  灵气最大值"),
                addOnlyResult);

            EsotericaDef gushen = new EsotericaDef { Modifier = "Gushen", EffectDesc = "原描述" };
            string gushenResult = gushen.GetEffectDesc(null);
            Check("AddP uses 总值倍率 and no derived absolute",
                gushenResult.Contains("灵气最大值（总值倍率）+25倍")
                && !gushenResult.Contains("+12,500.00"),
                gushenResult);

            EsotericaDef jiexin = new EsotericaDef { Modifier = "Jiexin", EffectDesc = "原描述" };
            string jiexinResult = jiexin.GetEffectDesc(null);
            Check("BAddP uses 基础倍率 instead of +0.00%",
                jiexinResult.Contains("灵气最大值（基础倍率）-0.1倍")
                && !jiexinResult.Contains("+0.00%"),
                jiexinResult);
            Check("AddV uses 总值加值 with the game unit and keeps BaseFive",
                jiexinResult.Contains("寿元（总值加值）-120年")
                && jiexinResult.Contains("+50%  悟性")
                && !jiexinResult.Contains("+0.00  灵气最大值"),
                jiexinResult);

            EsotericaDef yuzhen = new EsotericaDef { Modifier = "Yuzhen", EffectDesc = "原描述" };
            string yuzhenResult = yuzhen.GetEffectDesc(null);
            Check("BAddP on another property uses 基础倍率",
                yuzhenResult.Contains("术法威力加成（基础倍率）+0.2倍"),
                yuzhenResult);

            EsotericaDef fourValues = new EsotericaDef { Modifier = "Four", EffectDesc = "原描述" };
            string fourResult = fourValues.GetEffectDesc(null);
            string bAddV = "测试属性（基础加值）+10.00";
            string bAddP = "测试属性（基础倍率）+0.2倍";
            string addV = "测试属性（总值加值）+5.00";
            string addP = "测试属性（总值倍率）+0.1倍";
            Check("all four values use four distinct labels",
                fourResult.Contains(bAddV) && fourResult.Contains(bAddP)
                && fourResult.Contains(addV) && fourResult.Contains(addP),
                fourResult);
            Check("AddP and BAddP do not share the same label",
                fourResult.Contains("基础倍率") && fourResult.Contains("总值倍率"),
                fourResult);
            Check("components keep the BAddV, BAddP, AddV, AddP order",
                fourResult.IndexOf(bAddV, StringComparison.Ordinal) < fourResult.IndexOf(bAddP, StringComparison.Ordinal)
                && fourResult.IndexOf(bAddP, StringComparison.Ordinal) < fourResult.IndexOf(addV, StringComparison.Ordinal)
                && fourResult.IndexOf(addV, StringComparison.Ordinal) < fourResult.IndexOf(addP, StringComparison.Ordinal),
                fourResult);

            EsotericaDef unknown = new EsotericaDef { Modifier = "Unknown", EffectDesc = "原描述" };
            string unknownResult = unknown.GetEffectDesc(null);
            Check("properties without a PropertyDef still render safely",
                unknownResult.Contains("Missing（总值加值）+1.00"),
                unknownResult);

            EsotericaDef baseFiveOnly = new EsotericaDef { Modifier = "BaseFiveOnly", EffectDesc = "原描述" };
            string baseFiveResult = baseFiveOnly.GetEffectDesc(null);
            Check("native BaseFive lines are preserved", baseFiveResult.Contains("+50%  悟性"), baseFiveResult);

            EsotericaDef legacy = new EsotericaDef { Modifier = "AddOnly", EffectDesc = "原描述(AddP=5)" };
            Check("skips when old hardcoded mod text is present", legacy.GetEffectDesc(null) == "原描述(AddP=5)", null);

            EsotericaDef withoutModifier = new EsotericaDef { EffectDesc = "原描述" };
            Check("leaves entries without modifier untouched", withoutModifier.GetEffectDesc(null) == "原描述", null);

            EsotericaDef emptyDescription = new EsotericaDef { Modifier = "AddOnly" };
            string emptyResult = emptyDescription.GetEffectDesc(null);
            Check("handles empty original description", emptyResult != null && emptyResult.StartsWith("[size=10]"), emptyResult);

            EsotericaTemplateData addv = new EsotericaTemplateData
            {
                Kind = g_emEsotericaUnitKind.Property,
                PName = "NpcLingMaxValue",
                IsAddv = true
            };
            string addvResult = addv.GetEffectDesc(new EsotericaData { Value = 5f });
            Check("random: AddV uses 总值加值", addvResult.Contains("灵气最大值（总值加值）+5.00"), addvResult);

            EsotericaTemplateData addp = new EsotericaTemplateData
            {
                Kind = g_emEsotericaUnitKind.Property,
                PName = "NpcLingMaxValue",
                IsAddv = false
            };
            string addpResult = addp.GetEffectDesc(new EsotericaData { Value = 0.3f });
            Check("random: AddP uses 总值倍率", addpResult.Contains("灵气最大值（总值倍率）+0.3倍"), addpResult);

            EsotericaTemplateData zero = new EsotericaTemplateData
            {
                Kind = g_emEsotericaUnitKind.Property,
                PName = "NpcLingMaxValue",
                IsAddv = true
            };
            Check("random: zero values are ignored", zero.GetEffectDesc(new EsotericaData { Value = 0f }) == "大量的NpcLingMaxValue", null);

            EsotericaTemplateData fiveBase = new EsotericaTemplateData
            {
                Kind = g_emEsotericaUnitKind.FiveBase,
                PName = "Physique",
                EffectDesc = "原描述"
            };
            Check("random: FiveBase text is left to the game", fiveBase.GetEffectDesc(new EsotericaData { Value = 1f }) == "原描述", null);

            TFMgr.Instance.Language = "en";
            properties.Defs["NpcLingMaxValue"].DisplayName = "Max Qi";
            ModifierMgr.Instance.Defs["AddOnly"].NativeLines[0] = "+40.00  Max Qi";
            ModifierMgr.Instance.Defs["Gushen"].NativeLines[0] = "+12,500.00  Max Qi";
            string englishAdd = addOnly.GetEffectDesc(null);
            string englishMult = gushen.GetEffectDesc(null);
            string englishFour = fourValues.GetEffectDesc(null);
            Check("English: labels follow the game language",
                englishAdd.Contains("Max Qi (total addition) +40.00")
                && englishMult.Contains("Max Qi (total multiplier) +25x")
                && englishFour.Contains("(base addition)")
                && englishFour.Contains("(base multiplier)")
                && englishFour.Contains("(total addition)")
                && englishFour.Contains("(total multiplier)"),
                englishAdd + " | " + englishMult);

            TFMgr.Instance.Language = "SomeLocalizationMod";
            string localized = addOnly.GetEffectDesc(null);
            Check("a localization mod language falls back to English",
                localized.Contains("Max Qi (total addition) +40.00"),
                localized);

            TFMgr.Instance = null;
            string fallback = addOnly.GetEffectDesc(null);
            Check("unknown language falls back to English",
                fallback.Contains("Max Qi (total addition) +40.00"),
                fallback);

            if (failures > 0)
            {
                Console.WriteLine(failures + " test(s) failed.");
                Environment.Exit(1);
            }

            Console.WriteLine("All tests passed.");
        }

        private static ModifierDef AddModifier(string name, params string[] nativeLines)
        {
            ModifierDef def = new ModifierDef();
            def.NativeLines.AddRange(nativeLines);
            ModifierMgr.Instance.Defs[name] = def;
            return def;
        }

        private static ModifierPropertyData Add(this ModifierDef def, string name)
        {
            ModifierPropertyData data = new ModifierPropertyData { Name = name };
            def.Properties.Add(data);
            return data;
        }

        private static void Check(string name, bool condition, string actual)
        {
            if (condition)
            {
                Console.WriteLine("PASS " + name);
                return;
            }

            failures++;
            Console.WriteLine("FAIL " + name);
            if (actual != null)
            {
                Console.WriteLine("     actual: " + actual.Replace("\n", "\\n"));
            }
        }
    }
}
