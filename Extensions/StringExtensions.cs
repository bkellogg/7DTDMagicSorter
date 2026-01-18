namespace MagicSorter.Extensions
{
    /// <summary>
    ///     Extension methods for case-insensitive string matching
    /// </summary>
    public static class StringExtensions
    {
        /// <summary>
        ///     Case-insensitive contains check
        /// </summary>
        public static bool Includes(this string str, string value)
        {
            return str.ToLower().Contains(value.ToLower());
        }

        /// <summary>
        ///     Case-insensitive StartsWith check
        /// </summary>
        public static bool HasPrefix(this string str, string value)
        {
            return str.ToLower().StartsWith(value.ToLower());
        }

        /// <summary>
        ///     Case-insensitive equality check
        /// </summary>
        public static bool IsEqual(this string str, string value)
        {
            return str.Equals(value, System.StringComparison.OrdinalIgnoreCase);
        }
    }
}
