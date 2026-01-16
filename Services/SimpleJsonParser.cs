using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using MagicSorter.Models;

namespace MagicSorter.Services
{
    /// <summary>
    /// Lightweight JSON parser with no external dependencies
    /// </summary>
    public static class SimpleJsonParser
    {
        /// <summary>
        /// Parses mapping data from JSON string
        /// </summary>
        public static MappingData ParseMappingData(string json)
        {
            var data = new MappingData();
            if (string.IsNullOrEmpty(json)) return data;

            try
            {
                var root = ParseValue(json, 0, out _) as Dictionary<string, object>;
                if (root == null) return data;

                // Parse version
                if (root.TryGetValue("version", out var version))
                {
                    data.Version = version?.ToString() ?? "1.0.0";
                }

                // Parse categories
                if (root.TryGetValue("categories", out var categoriesObj) && categoriesObj is Dictionary<string, object> categories)
                {
                    foreach (var kvp in categories)
                    {
                        var catDef = new CategoryDefinition();
                        if (kvp.Value is Dictionary<string, object> catData)
                        {
                            if (catData.TryGetValue("specificity", out var spec))
                            {
                                catDef.Specificity = Convert.ToInt32(spec);
                            }
                            if (catData.TryGetValue("description", out var desc))
                            {
                                catDef.Description = desc?.ToString() ?? "";
                            }
                        }
                        else if (kvp.Value != null)
                        {
                            catDef.Specificity = Convert.ToInt32(kvp.Value);
                        }
                        data.Categories[kvp.Key] = catDef;
                    }
                }

                // Parse items
                if (root.TryGetValue("items", out var itemsObj) && itemsObj is Dictionary<string, object> items)
                {
                    foreach (var kvp in items)
                    {
                        var categoryList = new List<string>();
                        if (kvp.Value is List<object> arrayList)
                        {
                            foreach (var item in arrayList)
                            {
                                categoryList.Add(item?.ToString() ?? "");
                            }
                        }
                        else if (kvp.Value is string singleCat)
                        {
                            categoryList.Add(singleCat);
                        }
                        data.Items[kvp.Key] = categoryList;
                    }
                }

                // Parse aliases
                if (root.TryGetValue("aliases", out var aliasesObj) && aliasesObj is Dictionary<string, object> aliases)
                {
                    foreach (var kvp in aliases)
                    {
                        data.ContainerAliases[kvp.Key] = kvp.Value?.ToString() ?? "";
                    }
                }

                // Parse tags
                if (root.TryGetValue("tags", out var tagsObj) && tagsObj is Dictionary<string, object> tags)
                {
                    foreach (var kvp in tags)
                    {
                        var tagList = new List<string>();
                        if (kvp.Value is List<object> arrayList)
                        {
                            foreach (var item in arrayList)
                            {
                                tagList.Add(item?.ToString() ?? "");
                            }
                        }
                        data.Tags[kvp.Key] = tagList;
                    }
                }

                // Parse category fallbacks
                if (root.TryGetValue("categoryFallbacks", out var fallbacksObj) && fallbacksObj is Dictionary<string, object> fallbacks)
                {
                    foreach (var kvp in fallbacks)
                    {
                        data.CategoryFallbacks[kvp.Key] = kvp.Value?.ToString() ?? "";
                    }
                }
            }
            catch (Exception ex)
            {
                Log.Error($"[MagicSorter] JSON parse error: {ex.Message}");
            }

            return data;
        }

        private static object ParseValue(string json, int index, out int endIndex)
        {
            index = SkipWhitespace(json, index);

            if (index >= json.Length)
            {
                endIndex = index;
                return null;
            }

            char c = json[index];

            if (c == '{')
                return ParseObject(json, index, out endIndex);
            if (c == '[')
                return ParseArray(json, index, out endIndex);
            if (c == '"')
                return ParseString(json, index, out endIndex);
            if (c == 't' || c == 'f')
                return ParseBool(json, index, out endIndex);
            if (c == 'n')
                return ParseNull(json, index, out endIndex);
            if (c == '-' || char.IsDigit(c))
                return ParseNumber(json, index, out endIndex);

            endIndex = index;
            return null;
        }

        private static Dictionary<string, object> ParseObject(string json, int index, out int endIndex)
        {
            var dict = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);
            index++; // Skip '{'
            index = SkipWhitespace(json, index);

            while (index < json.Length && json[index] != '}')
            {
                // Parse key
                if (json[index] != '"')
                {
                    endIndex = index;
                    return dict;
                }

                string key = ParseString(json, index, out index);
                index = SkipWhitespace(json, index);

                // Expect ':'
                if (index >= json.Length || json[index] != ':')
                {
                    endIndex = index;
                    return dict;
                }
                index++; // Skip ':'

                // Parse value
                object value = ParseValue(json, index, out index);
                dict[key] = value;

                index = SkipWhitespace(json, index);

                // Check for comma or end
                if (index < json.Length && json[index] == ',')
                {
                    index++;
                    index = SkipWhitespace(json, index);
                }
            }

            if (index < json.Length && json[index] == '}')
                index++;

            endIndex = index;
            return dict;
        }

        private static List<object> ParseArray(string json, int index, out int endIndex)
        {
            var list = new List<object>();
            index++; // Skip '['
            index = SkipWhitespace(json, index);

            while (index < json.Length && json[index] != ']')
            {
                object value = ParseValue(json, index, out index);
                list.Add(value);

                index = SkipWhitespace(json, index);

                // Check for comma or end
                if (index < json.Length && json[index] == ',')
                {
                    index++;
                    index = SkipWhitespace(json, index);
                }
            }

            if (index < json.Length && json[index] == ']')
                index++;

            endIndex = index;
            return list;
        }

        private static string ParseString(string json, int index, out int endIndex)
        {
            var sb = new StringBuilder();
            index++; // Skip opening '"'

            while (index < json.Length)
            {
                char c = json[index];

                if (c == '"')
                {
                    index++;
                    break;
                }

                if (c == '\\' && index + 1 < json.Length)
                {
                    index++;
                    char escaped = json[index];
                    switch (escaped)
                    {
                        case '"': sb.Append('"'); break;
                        case '\\': sb.Append('\\'); break;
                        case '/': sb.Append('/'); break;
                        case 'b': sb.Append('\b'); break;
                        case 'f': sb.Append('\f'); break;
                        case 'n': sb.Append('\n'); break;
                        case 'r': sb.Append('\r'); break;
                        case 't': sb.Append('\t'); break;
                        case 'u':
                            if (index + 4 < json.Length)
                            {
                                string hex = json.Substring(index + 1, 4);
                                if (int.TryParse(hex, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out int unicode))
                                {
                                    sb.Append((char)unicode);
                                    index += 4;
                                }
                            }
                            break;
                        default: sb.Append(escaped); break;
                    }
                }
                else
                {
                    sb.Append(c);
                }

                index++;
            }

            endIndex = index;
            return sb.ToString();
        }

        private static object ParseNumber(string json, int index, out int endIndex)
        {
            int start = index;

            if (json[index] == '-')
                index++;

            while (index < json.Length && char.IsDigit(json[index]))
                index++;

            bool isFloat = false;
            if (index < json.Length && json[index] == '.')
            {
                isFloat = true;
                index++;
                while (index < json.Length && char.IsDigit(json[index]))
                    index++;
            }

            if (index < json.Length && (json[index] == 'e' || json[index] == 'E'))
            {
                isFloat = true;
                index++;
                if (index < json.Length && (json[index] == '+' || json[index] == '-'))
                    index++;
                while (index < json.Length && char.IsDigit(json[index]))
                    index++;
            }

            string numStr = json.Substring(start, index - start);
            endIndex = index;

            if (isFloat)
            {
                if (double.TryParse(numStr, NumberStyles.Float, CultureInfo.InvariantCulture, out double d))
                    return d;
            }
            else
            {
                if (int.TryParse(numStr, NumberStyles.Integer, CultureInfo.InvariantCulture, out int i))
                    return i;
                if (long.TryParse(numStr, NumberStyles.Integer, CultureInfo.InvariantCulture, out long l))
                    return l;
            }

            return 0;
        }

        private static bool ParseBool(string json, int index, out int endIndex)
        {
            if (json.Substring(index).StartsWith("true", StringComparison.Ordinal))
            {
                endIndex = index + 4;
                return true;
            }
            if (json.Substring(index).StartsWith("false", StringComparison.Ordinal))
            {
                endIndex = index + 5;
                return false;
            }
            endIndex = index;
            return false;
        }

        private static object ParseNull(string json, int index, out int endIndex)
        {
            if (json.Substring(index).StartsWith("null", StringComparison.Ordinal))
            {
                endIndex = index + 4;
                return null;
            }
            endIndex = index;
            return null;
        }

        private static int SkipWhitespace(string json, int index)
        {
            while (index < json.Length && char.IsWhiteSpace(json[index]))
                index++;
            return index;
        }
    }
}
