namespace OceanOfCode.Helpers
{
    public static class StringHelpers
    {
        public static string ToPascalCase(this string text)
        {
            if (text == null || text.Length < 2)
                return text;

            return text.Substring(0, 1).ToUpper() + text.Substring(1).ToLower();
        }
    }
}
