using System.Collections.Generic;
using System.Linq;
using MagicSorter.Extensions;
using MagicSorter.Models;

namespace MagicSorter.Services
{
    /// <summary>
    ///     Matches item names against pattern rules to determine categories
    /// </summary>
    public class PatternMatcher
    {
        private readonly List<PatternRule> _sortedPatterns;

        public PatternMatcher(List<PatternRule> patterns)
        {
            // Filter out invalid patterns and sort by priority (highest first)
            _sortedPatterns = patterns != null
                ? patterns
                    .Where(p => !string.IsNullOrEmpty(p.Match) && p.Categories != null && p.Categories.Count > 0)
                    .OrderByDescending(p => p.Priority)
                    .ToList()
                : new List<PatternRule>();
        }

        /// <summary>
        ///     Gets categories for an item name by matching against pattern rules
        /// </summary>
        /// <param name="itemName">The item name to match</param>
        /// <returns>List of categories, or empty list if no match</returns>
        public List<string> GetCategories(string itemName)
        {
            if (string.IsNullOrEmpty(itemName))
                return new List<string>();

            foreach (var pattern in _sortedPatterns)
            {
                if (Matches(itemName, pattern))
                    return new List<string>(pattern.Categories);
            }

            return new List<string>();
        }

        /// <summary>
        ///     Checks if an item name matches a pattern rule
        /// </summary>
        private static bool Matches(string itemName, PatternRule pattern)
        {
            // Check primary match
            if (!MatchesType(itemName, pattern.Match, pattern.Type))
                return false;

            // Check exclusion if specified
            if (!string.IsNullOrEmpty(pattern.Exclude))
            {
                if (MatchesType(itemName, pattern.Exclude, pattern.ExcludeType))
                    return false;
            }

            // Check secondary match if specified
            if (!string.IsNullOrEmpty(pattern.AlsoMatch))
            {
                if (!MatchesType(itemName, pattern.AlsoMatch, pattern.AlsoMatchType))
                    return false;
            }

            return true;
        }

        /// <summary>
        ///     Performs the actual pattern matching based on type
        /// </summary>
        private static bool MatchesType(string itemName, string match, PatternType type)
        {
            switch (type)
            {
                case PatternType.Prefix:
                    return itemName.HasPrefix(match);
                case PatternType.Contains:
                    return itemName.Includes(match);
                case PatternType.Equals:
                    return itemName.IsEqual(match);
                default:
                    return false;
            }
        }
    }
}
