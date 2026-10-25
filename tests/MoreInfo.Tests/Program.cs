using System;
using XiaWorld;

namespace MoreInfo.Tests
{
    internal static class Program
    {
        private static int failures;

        private static void Main()
        {
            ModifierMgr.Instance = new ModifierMgr();
            ModifierMgr.Instance.Defs["TestMod"] = new ModifierDef();
            ModifierMgr.Instance.Defs["TestMod"].Lines.Add("+10.00 测试属性");
            ModifierMgr.Instance.Defs["TestMod"].Lines.Add("+0.30点/秒 饮气效率");
            PropertyMgr.Instance = new PropertyMgr();

            MoreInfo.OnInit();

            EsotericaDef plain = new EsotericaDef { Modifier = "TestMod", EffectDesc = "原始描述" };
            string plainResult = plain.GetEffectDesc(null);
            Check(
                "base: keeps original text and appends values",
                plainResult.StartsWith("原始描述\n") && plainResult.Contains("+10.00 测试属性") && plainResult.Contains("+0.30点/秒 饮气效率"),
                plainResult);

            EsotericaDef again = new EsotericaDef { Modifier = "TestMod", EffectDesc = "原始描述" };
            string againResult = again.GetEffectDesc(null);
            Check("base: no accumulation across calls", againResult == plainResult, againResult);

            EsotericaDef legacy = new EsotericaDef { Modifier = "TestMod", EffectDesc = "原始描述(AddP=5)" };
            string legacyResult = legacy.GetEffectDesc(null);
            Check("base: skips when old hardcoded mod text is present", legacyResult == "原始描述(AddP=5)", legacyResult);

            EsotericaDef withoutModifier = new EsotericaDef { EffectDesc = "原始描述" };
            string withoutModifierResult = withoutModifier.GetEffectDesc(null);
            Check("base: leaves entries without modifier untouched", withoutModifierResult == "原始描述", withoutModifierResult);

            EsotericaDef unknownModifier = new EsotericaDef { Modifier = "Missing", EffectDesc = "原始描述" };
            string unknownModifierResult = unknownModifier.GetEffectDesc(null);
            Check("base: unknown modifier is ignored", unknownModifierResult == "原始描述", unknownModifierResult);

            EsotericaDef emptyDescription = new EsotericaDef { Modifier = "TestMod" };
            string emptyResult = emptyDescription.GetEffectDesc(null);
            Check("base: handles empty original description", emptyResult != null && emptyResult.StartsWith("[size=10]"), emptyResult);

            EsotericaTemplateData addv = new EsotericaTemplateData
            {
                Kind = g_emEsotericaUnitKind.Property,
                PName = "TestProp",
                IsAddv = true
            };
            string addvResult = addv.GetEffectDesc(new EsotericaData { Value = 5f });
            Check("random: appends exact AddV value", addvResult.Contains("大量的TestProp") && addvResult.Contains("+5.00  测试属性"), addvResult);

            EsotericaTemplateData addp = new EsotericaTemplateData
            {
                Kind = g_emEsotericaUnitKind.Property,
                PName = "TestProp",
                IsAddv = false
            };
            string addpResult = addp.GetEffectDesc(new EsotericaData { Value = 0.3f });
            Check("random: appends exact AddP value", addpResult.Contains("+0.30  测试属性"), addpResult);

            EsotericaTemplateData unknownProperty = new EsotericaTemplateData
            {
                Kind = g_emEsotericaUnitKind.Property,
                PName = "Missing",
                IsAddv = true
            };
            string unknownPropertyResult = unknownProperty.GetEffectDesc(new EsotericaData { Value = 1f });
            Check("random: unknown property is ignored", unknownPropertyResult == "大量的Missing", unknownPropertyResult);

            EsotericaTemplateData zero = new EsotericaTemplateData
            {
                Kind = g_emEsotericaUnitKind.Property,
                PName = "TestProp",
                IsAddv = true
            };
            string zeroResult = zero.GetEffectDesc(new EsotericaData { Value = 0f });
            Check("random: zero values are ignored", zeroResult == "大量的TestProp", zeroResult);

            EsotericaTemplateData fiveBase = new EsotericaTemplateData
            {
                Kind = g_emEsotericaUnitKind.FiveBase,
                PName = "Physique",
                EffectDesc = "原始描述"
            };
            string fiveBaseResult = fiveBase.GetEffectDesc(new EsotericaData { Value = 1f });
            Check("random: FiveBase text is left to the game", fiveBaseResult == "原始描述", fiveBaseResult);

            if (failures > 0)
            {
                Console.WriteLine(failures + " test(s) failed.");
                Environment.Exit(1);
            }

            Console.WriteLine("All tests passed.");
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
            Console.WriteLine("     actual: " + (actual == null ? "<null>" : actual.Replace("\n", "\\n")));
        }
    }
}
