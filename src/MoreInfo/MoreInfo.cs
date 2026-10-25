using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using HarmonyLib;

namespace MoreInfo
{
    public static class MoreInfo
    {
        private const string HarmonyId = "MistyEye.ACS.MoreInfo";
        private const string ValueColor = "#D06508";
        private const float Epsilon = 0.000001f;

        private static readonly HashSet<string> LoggedErrors = new HashSet<string>();
        private static Harmony harmony;
        private static bool prepared;
        private static bool prepareFailed;

        private static FieldInfo modifierField;
        private static FieldInfo kindField;
        private static FieldInfo pNameField;
        private static FieldInfo isAddvField;
        private static FieldInfo valueField;

        private static PropertyInfo modifierMgrInstance;
        private static PropertyInfo propertyMgrInstance;
        private static MethodInfo modifierGetDef;
        private static MethodInfo simpleDesc;
        private static MethodInfo propertyGetDef;
        private static MethodInfo additionDesc;
        private static MethodInfo debugLog;

        public static void OnInit()
        {
            try
            {
                harmony = new Harmony(HarmonyId);
                Patch("XiaWorld.EsotericaDef", "GetEffectDesc", "EsotericaPostfix");
                Patch("XiaWorld.EsotericaTemplateData", "GetEffectDesc", "TemplatePostfix");
                Log(Prepare() ? "MoreInfo ready." : "MoreInfo is not ready.");
            }
            catch (Exception e)
            {
                Log("MoreInfo failed to initialize: " + e.Message);
            }
        }

        private static void Patch(string typeName, string methodName, string postfixName)
        {
            Type type = AccessTools.TypeByName(typeName);
            if (type == null)
            {
                Log("MoreInfo: type not found: " + typeName);
                return;
            }

            MethodInfo original = AccessTools.Method(type, methodName);
            if (original == null)
            {
                Log("MoreInfo: method not found: " + typeName + "." + methodName);
                return;
            }

            MethodInfo postfix = typeof(MoreInfo).GetMethod(postfixName, BindingFlags.Static | BindingFlags.NonPublic);
            if (postfix == null)
            {
                Log("MoreInfo: postfix not found: " + postfixName);
                return;
            }

            harmony.Patch(original, postfix: new HarmonyMethod(postfix));
            Log("patched " + typeName + "." + methodName);
        }

        private static bool Prepare()
        {
            if (prepared)
            {
                return true;
            }
            if (prepareFailed)
            {
                return false;
            }

            try
            {
                Type esotericaDef = AccessTools.TypeByName("XiaWorld.EsotericaDef");
                Type template = AccessTools.TypeByName("XiaWorld.EsotericaTemplateData");
                Type data = AccessTools.TypeByName("XiaWorld.EsotericaData");
                Type modifierDef = AccessTools.TypeByName("XiaWorld.ModifierDef");
                Type modifierMgr = AccessTools.TypeByName("XiaWorld.ModifierMgr");
                Type propertyMgr = AccessTools.TypeByName("XiaWorld.PropertyMgr");
                Type npc = AccessTools.TypeByName("XiaWorld.Npc");

                if (esotericaDef == null || template == null || data == null || modifierDef == null ||
                    modifierMgr == null || propertyMgr == null || npc == null)
                {
                    Fail("required game types are missing");
                    return false;
                }

                modifierField = AccessTools.Field(esotericaDef, "Modifier");
                kindField = AccessTools.Field(template, "Kind");
                pNameField = AccessTools.Field(template, "PName");
                isAddvField = AccessTools.Field(template, "IsAddv");
                valueField = AccessTools.Field(data, "Value");

                modifierMgrInstance = AccessTools.Property(modifierMgr, "Instance");
                propertyMgrInstance = AccessTools.Property(propertyMgr, "Instance");
                modifierGetDef = AccessTools.Method(modifierMgr, "GetDef", new[] { typeof(string) });
                simpleDesc = AccessTools.Method(modifierDef, "GetSimpleDesc", new[] { typeof(float) });
                propertyGetDef = AccessTools.Method(propertyMgr, "GetDef", new[] { typeof(string) });
                additionDesc = AccessTools.Method(propertyMgr, "GetPropertyAddtionDesc", new[]
                {
                    typeof(string), npc, typeof(float), typeof(float), typeof(float), typeof(float)
                });

                Type debugType = AccessTools.TypeByName("UnityEngine.Debug");
                if (debugType != null)
                {
                    debugLog = AccessTools.Method(debugType, "Log", new[] { typeof(object) });
                }

                if (modifierField == null || kindField == null || pNameField == null || isAddvField == null ||
                    valueField == null || modifierMgrInstance == null || propertyMgrInstance == null ||
                    modifierGetDef == null || simpleDesc == null || propertyGetDef == null || additionDesc == null)
                {
                    Fail("required game members are missing");
                    return false;
                }

                prepared = true;
                return true;
            }
            catch (Exception e)
            {
                Fail(e.Message);
                return false;
            }
        }

        private static void Fail(string reason)
        {
            prepareFailed = true;
            Log("MoreInfo disabled: " + reason);
        }

        private static void EsotericaPostfix(object __instance, ref string __result)
        {
            try
            {
                if (__instance == null || !Prepare() || AlreadyHasRawNumbers(__result))
                {
                    return;
                }

                string modifierName = modifierField.GetValue(__instance) as string;
                if (string.IsNullOrEmpty(modifierName))
                {
                    return;
                }

                object manager = modifierMgrInstance.GetValue(null, null);
                if (manager == null)
                {
                    return;
                }

                object definition = modifierGetDef.Invoke(manager, new object[] { modifierName });
                if (definition == null)
                {
                    return;
                }

                string body = Join(InvokeSimpleDesc(definition));
                if (body == null)
                {
                    return;
                }

                __result = Append(__result, body);
            }
            catch (Exception e)
            {
                LogOnce("EsotericaPostfix: " + e.Message);
            }
        }

        private static void TemplatePostfix(object __instance, object __0, ref string __result)
        {
            try
            {
                if (__instance == null || __0 == null || !Prepare())
                {
                    return;
                }

                object kind = kindField.GetValue(__instance);
                if (kind == null || kind.ToString() != "Property")
                {
                    return;
                }

                string propertyName = pNameField.GetValue(__instance) as string;
                if (string.IsNullOrEmpty(propertyName))
                {
                    return;
                }

                object isAddvValue = isAddvField.GetValue(__instance);
                bool isAddv = isAddvValue is bool && (bool)isAddvValue;

                object rawValue = valueField.GetValue(__0);
                if (!(rawValue is float))
                {
                    return;
                }

                float value = (float)rawValue;
                if (Math.Abs(value) < Epsilon)
                {
                    return;
                }

                object propertyManager = propertyMgrInstance.GetValue(null, null);
                if (propertyManager == null)
                {
                    return;
                }

                if (propertyGetDef.Invoke(propertyManager, new object[] { propertyName }) == null)
                {
                    return;
                }

                object description = additionDesc.Invoke(propertyManager, new object[]
                {
                    propertyName,
                    null,
                    isAddv ? value : 0f,
                    isAddv ? 0f : value,
                    0f,
                    0f
                });

                string line = description as string;
                if (string.IsNullOrEmpty(line))
                {
                    return;
                }

                line = line.Trim();
                if (line.Length == 0)
                {
                    return;
                }

                __result = Append(__result, line);
            }
            catch (Exception e)
            {
                LogOnce("TemplatePostfix: " + e.Message);
            }
        }

        private static IEnumerable<string> InvokeSimpleDesc(object definition)
        {
            return simpleDesc.Invoke(definition, new object[] { 1f }) as IEnumerable<string>;
        }

        private static string Join(IEnumerable<string> lines)
        {
            if (lines == null)
            {
                return null;
            }

            StringBuilder builder = new StringBuilder();
            foreach (string raw in lines)
            {
                if (string.IsNullOrEmpty(raw))
                {
                    continue;
                }

                string line = raw.Trim();
                if (line.Length == 0)
                {
                    continue;
                }

                if (builder.Length > 0)
                {
                    builder.Append('\n');
                }
                builder.Append(line);
            }

            return builder.Length == 0 ? null : builder.ToString();
        }

        private static string Append(string original, string body)
        {
            string block = "[size=10][color=" + ValueColor + "]" + body + "[/color][/size]";
            return string.IsNullOrEmpty(original) ? block : original + "\n" + block;
        }

        private static bool AlreadyHasRawNumbers(string text)
        {
            if (string.IsNullOrEmpty(text))
            {
                return false;
            }

            return text.IndexOf("AddP=", StringComparison.Ordinal) >= 0
                || text.IndexOf("AddV=", StringComparison.Ordinal) >= 0;
        }

        private static void LogOnce(string message)
        {
            if (LoggedErrors.Add(message))
            {
                Log(message);
            }
        }

        private static void Log(string message)
        {
            try
            {
                if (debugLog != null)
                {
                    debugLog.Invoke(null, new object[] { "[MoreInfo] " + message });
                    return;
                }

                Console.WriteLine("[MoreInfo] " + message);
            }
            catch
            {
            }
        }
    }
}
