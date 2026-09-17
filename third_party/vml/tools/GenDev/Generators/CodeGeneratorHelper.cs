using System.Text;

namespace GenDev.Generators
{
    public static class CodeGeneratorHelper
    {
        /// <summary>Sanitize an identifier name for use in generated code (UPPER_SNAKE_CASE).</summary>
        public static string SanitizeUpper(string name)
        {
            if (string.IsNullOrEmpty(name)) return "_";
            var sb = new StringBuilder();
            foreach (char c in name)
            {
                if (char.IsLetterOrDigit(c) || c == '_')
                    sb.Append(char.ToUpperInvariant(c));
                else
                    sb.Append('_');
            }
            // Collapse duplicate underscores
            var result = sb.ToString();
            while (result.Contains("__"))
                result = result.Replace("__", "_");
            result = result.Trim('_');
            // Must not start with a digit
            if (result.Length > 0 && char.IsDigit(result[0]))
                result = "_" + result;
            return result.Length > 0 ? result : "_";
        }

        /// <summary>Sanitize an identifier name for use in generated code (lower_snake_case).</summary>
        public static string SanitizeLower(string name)
        {
            if (string.IsNullOrEmpty(name)) return "_";
            var sb = new StringBuilder();
            foreach (char c in name)
            {
                if (char.IsLetterOrDigit(c) || c == '_')
                    sb.Append(char.ToLowerInvariant(c));
                else
                    sb.Append('_');
            }
            var result = sb.ToString();
            while (result.Contains("__"))
                result = result.Replace("__", "_");
            result = result.Trim('_');
            if (result.Length > 0 && char.IsDigit(result[0]))
                result = "_" + result;
            return result.Length > 0 ? result : "_";
        }
    }
}
