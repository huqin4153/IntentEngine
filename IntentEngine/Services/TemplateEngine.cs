using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace IntentEngine.Services
{
    public static class TemplateEngine
    {
        public static string ReplaceSql(string template, Dictionary<string, string> context,
            out Dictionary<string, string> paramValues, string paramPrefix = "@")
        {
            paramValues = new Dictionary<string, string>();
            if (string.IsNullOrEmpty(template))
                return "";

            var ctx = context ?? new Dictionary<string, string>();
            var localParams = new Dictionary<string, string>();
            int paramIndex = 0;

            string result = Regex.Replace(template, @"\$(\w+)(?:\.(\w+))?", match =>
            {
                string fullKey = "$" + match.Groups[1].Value;
                string val;
                if (ctx.TryGetValue(fullKey, out val))
                    return EscapeSqlValue(val);
                if (ctx.TryGetValue(match.Groups[0].Value, out val))
                    return EscapeSqlValue(val);
                return match.Value;
            });

            result = Regex.Replace(result, @"@(\w+)", match =>
            {
                string key = "@" + match.Groups[1].Value;
                string name = match.Groups[1].Value;
                string val;
                if (ctx.TryGetValue(key, out val))
                {
                    if (!localParams.ContainsKey(name))
                        localParams[name] = val;
                    return paramPrefix + name;
                }
                return "NULL";
            });

            paramValues = localParams;
            return result;
        }

        public static string ReplaceText(string template, Dictionary<string, string> context)
        {
            if (string.IsNullOrEmpty(template)) return template;
            return Regex.Replace(template, @"\{(\$\w+(?:\.\w+)?|@\w+)\}", match =>
            {
                string val;
                if (context.TryGetValue(match.Groups[1].Value, out val))
                    return val;
                return match.Value;
            });
        }

        private static string EscapeSqlValue(string value)
        {
            if (value == null) return "NULL";
            return "'" + value.Replace("'", "''") + "'";
        }
    }
}
