#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using UnityEditor;
using UnityEngine;

namespace BFunCoreKit
{
    public static class CommandGenerator
    {
        private const string GENERATED_FILE_PATH = GlobalConst.SettingFolder + "/DebugConsole/GeneratedCommands.cs";

        private static string NormalizeContentForComparison(string content)
        {
            if (string.IsNullOrEmpty(content)) return string.Empty;

            var lines = content.Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.None);
            var filteredLines = lines.Where(line => !line.Trim().StartsWith("// Generated on:"));
            return string.Join("\n", filteredLines);
        }

        public static void GenerateCommandsFile(CustomCommandData data)
        {
            var sb = new StringBuilder();

            // --- Header ---
            sb.AppendLine("// AUTO-GENERATED FILE. DO NOT EDIT MANUALLY.");
            sb.AppendLine("// This file is optimized for performance using delegate caching.");
            sb.AppendLine($"// Generated on: {DateTime.Now}");
            sb.AppendLine();
            sb.AppendLine("using UnityEngine;");
            sb.AppendLine("using UnityEngine.Scripting;");
            sb.AppendLine("using System;");
            sb.AppendLine("using System.Reflection;");
            sb.AppendLine();
            sb.AppendLine("namespace BFunCoreKit");
            sb.AppendLine("{");
            sb.AppendLine("[Preserve]");
            sb.AppendLine("public static class GeneratedCommands");
            sb.AppendLine("{");

            foreach (var command in data.Commands)
            {
                if (string.IsNullOrWhiteSpace(command.CommandID) || string.IsNullOrWhiteSpace(command._selectedMethodPath))
                    continue;

                int lastDotIndex = command._selectedMethodPath.LastIndexOf('.');
                if (lastDotIndex == -1) continue;

                string assemblyQualifiedTypeName = command._selectedMethodPath.Substring(0, lastDotIndex);
                string memberName = command._selectedMethodPath.Substring(lastDotIndex + 1);
                string sanitizedCommandID = command.CommandID.Replace(" ", "_").Replace("-", "_");

                Type targetTypeInEditor = Type.GetType(assemblyQualifiedTypeName);
                MethodInfo methodInfoInEditor = null;
                if (targetTypeInEditor != null)
                    methodInfoInEditor = targetTypeInEditor.GetMethods(BindingFlags.Public | BindingFlags.Instance)
                                                           .FirstOrDefault(m => m.Name == memberName);

                if (methodInfoInEditor == null)
                    continue;

                var parameters = methodInfoInEditor.GetParameters();
                string delegateType;
                string delegateFieldName = $"_cachedDelegate_{sanitizedCommandID}";
                string instanceFieldName = $"_cachedInstance_{sanitizedCommandID}";

                if (parameters.Length == 0)
                    delegateType = "Action";
                else if (parameters.Length == 1)
                    delegateType = $"Action<{parameters[0].ParameterType.FullName.Replace('+', '.')}>";
                else
                    continue;

                // Cache instance + delegate
                sb.AppendLine($"    private static UnityEngine.MonoBehaviour {instanceFieldName};");
                sb.AppendLine($"    private static {delegateType} {delegateFieldName};");
                sb.AppendLine();

                sb.AppendLine($"    [DebugCommand(\"{command.CommandID}\", \"{command.Description}\")]");

                // ==========================================================================================
                // LOGIC SINH CODE MỚI: KIỂM TRA DYNAMIC INPUT
                // ==========================================================================================
                bool isDynamic = parameters.Length > 0 && command.UseDynamicInput;

                if (isDynamic)
                {
                    // Case 1: Dynamic - Hàm có tham số
                    string paramTypeName = parameters[0].ParameterType.FullName.Replace('+', '.');
                    sb.AppendLine($"    private static void Command_{sanitizedCommandID}({paramTypeName} value)");
                }
                else
                {
                    // Case 2: Fixed - Hàm không tham số
                    sb.AppendLine($"    private static void Command_{sanitizedCommandID}()");
                }

                sb.AppendLine("    {");
                sb.AppendLine($"        if ({delegateFieldName} == null)");
                sb.AppendLine("        {");
                sb.AppendLine($"            var targetType = Type.GetType(\"{assemblyQualifiedTypeName}\");");
                sb.AppendLine("            if (targetType == null) { Debug.LogError(\"[GeneratedCommand] Could not find type.\"); return; }");
                sb.AppendLine();

                // --- FindObjectOfType caching ---
                sb.AppendLine($"            if ({instanceFieldName} == null)");
                sb.AppendLine("            {");
                sb.AppendLine($"                {instanceFieldName} = UnityEngine.Object.FindObjectOfType(targetType) as UnityEngine.MonoBehaviour;");
                sb.AppendLine($"                if ({instanceFieldName} == null)");
                sb.AppendLine($"                {{ Debug.LogError($\"[GeneratedCommand] Could not find instance of type '{{targetType.Name}}' in scene.\"); return; }}");
                sb.AppendLine("            }");
                sb.AppendLine();

                sb.AppendLine($"            var method = targetType.GetMethod(\"{memberName}\", BindingFlags.Public | BindingFlags.Instance);");
                sb.AppendLine($"            if (method == null) {{ Debug.LogError(\"[GeneratedCommand] Could not find method '{memberName}'.\"); return; }}");
                sb.AppendLine();

                sb.AppendLine($"            {delegateFieldName} = ({delegateType})Delegate.CreateDelegate(typeof({delegateType}), {instanceFieldName}, method);");
                sb.AppendLine("        }");
                sb.AppendLine();

                // --- Try invoke ---
                sb.AppendLine("        try");
                sb.AppendLine("        {");

                if (parameters.Length == 0)
                {
                    sb.AppendLine($"            {delegateFieldName}();");
                }
                else if (isDynamic)
                {
                    // Truyền tham số 'value' từ hàm vào delegate
                    sb.AppendLine($"            {delegateFieldName}(value);");
                }
                else
                {
                    // Truyền tham số cứng (fixed)
                    string valueString = "null";
                    var paramType = parameters[0].ParameterType;

                    if (paramType == typeof(bool)) valueString = command.BoolParam.ToString().ToLower();
                    else if (paramType == typeof(int)) valueString = command.IntParam.ToString();
                    else if (paramType == typeof(float)) valueString = command.FloatParam.ToString() + "f";
                    else if (paramType == typeof(string)) valueString = $"\"{command.StringParam.Replace("\"", "\\\"")}\"";

                    sb.AppendLine($"            {delegateFieldName}({valueString});");
                }

                sb.AppendLine("        }");
                sb.AppendLine($"        catch (Exception ex) {{ Debug.LogError($\"Error executing command '{command.CommandID}': {{ex.Message}}\"); }}");
                sb.AppendLine("    }");
                sb.AppendLine();
            }

            sb.AppendLine("}");
            sb.AppendLine("}");

            // --- So sánh và ghi file nếu khác ---
            string newContent = sb.ToString();

            if (File.Exists(GENERATED_FILE_PATH))
            {
                string existingContent = File.ReadAllText(GENERATED_FILE_PATH);
                if (NormalizeContentForComparison(newContent) == NormalizeContentForComparison(existingContent))
                    return; // Không thay đổi, không cần ghi file
            }

            Directory.CreateDirectory(Path.GetDirectoryName(GENERATED_FILE_PATH));
            File.WriteAllText(GENERATED_FILE_PATH, newContent);
            AssetDatabase.Refresh();

            Debug.Log($"[CommandGenerator] ✅ Successfully generated optimized GeneratedCommands.cs with {data.Commands.Count} commands.");
        }
    }
}
#endif