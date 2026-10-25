using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
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
        private static FieldInfo modifierPropertiesField;
        private static FieldInfo kindField;
        private static FieldInfo pNameField;
        private static FieldInfo isAddvField;
        private static FieldInfo valueField;

        private static FieldInfo propertyNameField;
        private static FieldInfo propertyAddVField;
        private static FieldInfo propertyAddPField;
        private static FieldInfo propertyBAddVField;
        private static FieldInfo propertyBAddPField;
        private static FieldInfo propertyDisplayNameField;
        private static FieldInfo propertyDisplayValueField;
        private static FieldInfo propertyDisplayValueRelativeField;

        private static PropertyInfo modifierMgrInstance;
        private static PropertyInfo propertyMgrInstance;
        private static FieldInfo tfmgrInstanceField;
        private static PropertyInfo tfmgrInstanceProperty;
        private static PropertyInfo tfmgrLanguage;
        private static MethodInfo modifierGetDef;
        private static MethodInfo simpleDesc;
        private static MethodInfo propertyGetDef;
        private static MethodInfo debugLog;

        private sealed class Labels
        {
            public string Base;
            public string Total;
            public string Add;
            public string Mult;
            public string Join;
            public string Wrap;
            public string Suffix;

            public string Line(string name, string value, bool isBase, bool isMult)
            {
                string stage = string.Format(Join, isBase ? Base : Total, isMult ? Mult : Add);
                return string.Format(Wrap, name, stage, value);
            }
        }

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
                Type propertyData = AccessTools.TypeByName("XiaWorld.ModifierPropertyData");
                Type propertyDef = AccessTools.TypeByName("XiaWorld.PropertyDef");
                Type modifierMgr = AccessTools.TypeByName("XiaWorld.ModifierMgr");
                Type propertyMgr = AccessTools.TypeByName("XiaWorld.PropertyMgr");
                Type npc = AccessTools.TypeByName("XiaWorld.Npc");

                if (esotericaDef == null || template == null || data == null || modifierDef == null ||
                    propertyData == null || propertyDef == null || modifierMgr == null ||
                    propertyMgr == null || npc == null)
                {
                    Fail("required game types are missing");
                    return false;
                }

                modifierField = AccessTools.Field(esotericaDef, "Modifier");
                modifierPropertiesField = AccessTools.Field(modifierDef, "Properties");
                kindField = AccessTools.Field(template, "Kind");
                pNameField = AccessTools.Field(template, "PName");
                isAddvField = AccessTools.Field(template, "IsAddv");
                valueField = AccessTools.Field(data, "Value");

                propertyNameField = AccessTools.Field(propertyData, "Name");
                propertyAddVField = AccessTools.Field(propertyData, "AddV");
                propertyAddPField = AccessTools.Field(propertyData, "AddP");
                propertyBAddVField = AccessTools.Field(propertyData, "BAddV");
                propertyBAddPField = AccessTools.Field(propertyData, "BAddP");

                propertyDisplayNameField = AccessTools.Field(propertyDef, "DisplayName");
                propertyDisplayValueField = AccessTools.Field(propertyDef, "DisplayValue");
                propertyDisplayValueRelativeField = AccessTools.Field(propertyDef, "DisplayValueRelative");

                modifierMgrInstance = AccessTools.Property(modifierMgr, "Instance");
                propertyMgrInstance = AccessTools.Property(propertyMgr, "Instance");
                modifierGetDef = AccessTools.Method(modifierMgr, "GetDef", new[] { typeof(string) });
                simpleDesc = AccessTools.Method(modifierDef, "GetSimpleDesc", new[] { typeof(float) });
                propertyGetDef = AccessTools.Method(propertyMgr, "GetDef", new[] { typeof(string) });

                Type tfmgr = AccessTools.TypeByName("TFMgr");
                if (tfmgr == null)
                {
                    tfmgr = AccessTools.TypeByName("XiaWorld.TFMgr");
                }
                if (tfmgr != null)
                {
                    tfmgrInstanceField = AccessTools.Field(tfmgr, "Instance");
                    if (tfmgrInstanceField == null)
                    {
                        tfmgrInstanceProperty = AccessTools.Property(tfmgr, "Instance");
                    }
                    tfmgrLanguage = AccessTools.Property(tfmgr, "Language");
                    Log(tfmgrInstanceField != null || tfmgrInstanceProperty != null
                        ? "TFMgr language binding ok."
                        : "TFMgr language binding failed.");
                }

                Type debugType = AccessTools.TypeByName("UnityEngine.Debug");
                if (debugType != null)
                {
                    debugLog = AccessTools.Method(debugType, "Log", new[] { typeof(object) });
                }

                if (modifierField == null || modifierPropertiesField == null ||
                    kindField == null || pNameField == null || isAddvField == null || valueField == null ||
                    propertyNameField == null || propertyAddVField == null || propertyAddPField == null ||
                    propertyBAddVField == null || propertyBAddPField == null ||
                    propertyDisplayNameField == null || propertyDisplayValueField == null ||
                    propertyDisplayValueRelativeField == null || modifierMgrInstance == null ||
                    propertyMgrInstance == null || modifierGetDef == null || simpleDesc == null ||
                    propertyGetDef == null)
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

        private static Labels GetLabels()
        {
            if (IsChinese())
            {
                return new Labels
                {
                    Base = "基础",
                    Total = "总值",
                    Add = "加值",
                    Mult = "倍率",
                    Join = "{0}{1}",
                    Wrap = "{0}（{1}）{2}",
                    Suffix = "倍"
                };
            }

            return new Labels
            {
                Base = "base",
                Total = "total",
                Add = "addition",
                Mult = "multiplier",
                Join = "{0} {1}",
                Wrap = "{0} ({1}) {2}",
                Suffix = "x"
            };
        }

        private static bool IsChinese()
        {
            string language = GetGameLanguage();

            if (string.IsNullOrEmpty(language))
            {
                LogOnce("game language unavailable, defaulting to English");
                return false;
            }

            bool chinese = string.Equals(language, "cn", StringComparison.OrdinalIgnoreCase);
            LogOnce("game language=" + language + " labels=" + (chinese ? "zh" : "en"));
            return chinese;
        }

        private static string GetGameLanguage()
        {
            try
            {
                object manager = GetTfmgrInstance();
                if (manager != null && tfmgrLanguage != null)
                {
                    string language = tfmgrLanguage.GetValue(manager, null) as string;
                    if (!string.IsNullOrEmpty(language))
                    {
                        return language;
                    }
                }
            }
            catch
            {
            }

            return ReadLanguageFile();
        }

        private static string ReadLanguageFile()
        {
            try
            {
                string file = Path.Combine(Directory.GetCurrentDirectory(), "language.txt");
                if (!File.Exists(file))
                {
                    return null;
                }

                string text = File.ReadAllText(file);
                int end = text.IndexOfAny(new[] { '\r', '\n' });
                if (end >= 0)
                {
                    text = text.Substring(0, end);
                }
                return text.Trim();
            }
            catch
            {
                return null;
            }
        }

        private static object GetTfmgrInstance()
        {
            if (tfmgrInstanceField != null)
            {
                return tfmgrInstanceField.GetValue(null);
            }
            if (tfmgrInstanceProperty != null)
            {
                return tfmgrInstanceProperty.GetValue(null, null);
            }
            return null;
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

                string body = BuildModifierBody(definition);
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

                object propertyDefinition = GetPropertyDefinition(propertyName);
                string displayName = GetDisplayName(propertyDefinition, propertyName);
                Labels labels = GetLabels();

                string line = isAddv
                    ? labels.Line(displayName, FormatAbsolute(value, propertyDefinition), false, false)
                    : labels.Line(displayName, FormatMultiplier(value, labels), false, true);

                __result = Append(__result, line);
            }
            catch (Exception e)
            {
                LogOnce("TemplatePostfix: " + e.Message);
            }
        }

        private static string BuildModifierBody(object definition)
        {
            List<string> propertyLines = new List<string>();
            HashSet<string> propertyNames = new HashSet<string>();
            Labels labels = GetLabels();

            object rawProperties = modifierPropertiesField.GetValue(definition);
            IEnumerable properties = rawProperties as IEnumerable;
            if (properties != null)
            {
                foreach (object property in properties)
                {
                    if (property == null)
                    {
                        continue;
                    }

                    string name = propertyNameField.GetValue(property) as string;
                    if (string.IsNullOrEmpty(name))
                    {
                        continue;
                    }

                    float addv = (float)propertyAddVField.GetValue(property);
                    float addp = (float)propertyAddPField.GetValue(property);
                    float baddv = (float)propertyBAddVField.GetValue(property);
                    float baddp = (float)propertyBAddPField.GetValue(property);

                    object propertyDefinition = GetPropertyDefinition(name);
                    string displayName = GetDisplayName(propertyDefinition, name);
                    propertyNames.Add(displayName);

                    if (baddv != 0f)
                    {
                        propertyLines.Add(labels.Line(displayName, FormatAbsolute(baddv, propertyDefinition), true, false));
                    }
                    if (baddp != 0f)
                    {
                        propertyLines.Add(labels.Line(displayName, FormatMultiplier(baddp, labels), true, true));
                    }
                    if (addv != 0f)
                    {
                        propertyLines.Add(labels.Line(displayName, FormatAbsolute(addv, propertyDefinition), false, false));
                    }
                    if (addp != 0f)
                    {
                        propertyLines.Add(labels.Line(displayName, FormatMultiplier(addp, labels), false, true));
                    }
                }
            }

            List<string> nativeLines = ReadNativeLines(definition);
            return MergeLines(nativeLines, propertyLines, propertyNames);
        }

        private static List<string> ReadNativeLines(object definition)
        {
            List<string> result = new List<string>();
            try
            {
                IEnumerable lines = simpleDesc.Invoke(definition, new object[] { 1f }) as IEnumerable;
                if (lines == null)
                {
                    return result;
                }

                foreach (object raw in lines)
                {
                    string line = raw as string;
                    if (string.IsNullOrEmpty(line))
                    {
                        continue;
                    }
                    line = line.Trim();
                    if (line.Length > 0)
                    {
                        result.Add(line);
                    }
                }
            }
            catch (Exception e)
            {
                LogOnce("native: " + e.Message);
            }

            return result;
        }

        private static string MergeLines(List<string> nativeLines, List<string> propertyLines, HashSet<string> propertyNames)
        {
            StringBuilder builder = new StringBuilder();
            bool inserted = propertyLines.Count == 0;

            foreach (string line in nativeLines)
            {
                if (IsPropertyLine(line, propertyNames))
                {
                    if (!inserted)
                    {
                        AppendLines(builder, propertyLines);
                        inserted = true;
                    }
                    continue;
                }

                AppendLine(builder, line);
            }

            if (!inserted)
            {
                AppendLines(builder, propertyLines);
            }

            return builder.Length == 0 ? null : builder.ToString();
        }

        private static bool IsPropertyLine(string line, HashSet<string> propertyNames)
        {
            foreach (string name in propertyNames)
            {
                if (name.Length > 0 && line.EndsWith("  " + name, StringComparison.Ordinal))
                {
                    return true;
                }
            }
            return false;
        }

        private static void AppendLines(StringBuilder builder, List<string> lines)
        {
            foreach (string line in lines)
            {
                AppendLine(builder, line);
            }
        }

        private static void AppendLine(StringBuilder builder, string line)
        {
            if (string.IsNullOrEmpty(line))
            {
                return;
            }
            if (builder.Length > 0)
            {
                builder.Append('\n');
            }
            builder.Append(line);
        }

        private static object GetPropertyDefinition(string name)
        {
            try
            {
                object manager = propertyMgrInstance.GetValue(null, null);
                if (manager == null)
                {
                    return null;
                }
                return propertyGetDef.Invoke(manager, new object[] { name });
            }
            catch
            {
                return null;
            }
        }

        private static string GetDisplayName(object definition, string fallback)
        {
            if (definition != null)
            {
                string name = propertyDisplayNameField.GetValue(definition) as string;
                if (!string.IsNullOrEmpty(name))
                {
                    return name.Trim();
                }
            }
            return fallback;
        }

        private static string FormatAbsolute(float value, object propertyDefinition)
        {
            string format = null;
            if (propertyDefinition != null)
            {
                format = propertyDisplayValueRelativeField.GetValue(propertyDefinition) as string;
                if (string.IsNullOrEmpty(format))
                {
                    format = propertyDisplayValueField.GetValue(propertyDefinition) as string;
                }
            }
            if (string.IsNullOrEmpty(format))
            {
                format = "{0:N2}";
            }

            string text;
            try
            {
                text = string.Format(CultureInfo.CurrentCulture, format, value);
            }
            catch
            {
                text = string.Format(CultureInfo.CurrentCulture, "{0:N2}", value);
            }

            return (value >= 0f ? "+" : "") + text;
        }

        private static string FormatMultiplier(float value, Labels labels)
        {
            string text = value.ToString("0.####", CultureInfo.CurrentCulture);
            return (value >= 0f ? "+" : "") + text + labels.Suffix;
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
