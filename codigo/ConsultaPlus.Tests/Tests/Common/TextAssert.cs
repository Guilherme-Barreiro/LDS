using System.Globalization;
using System.Text;
using Xunit;

namespace ConsultaPlus.Tests
{
    public static class TextAssert
    {
        public static void ContainsIgnoringDiacritics(string expected, string actual)
        {
            var normExpected = Normalize(expected);
            var normActual = Normalize(actual);

            Assert.Contains(normExpected, normActual);
        }

        private static string Normalize(string s)
        {
            if (s is null) return string.Empty;

            var formD = s.Normalize(NormalizationForm.FormD);
            var sb = new StringBuilder(formD.Length);

            foreach (var ch in formD)
            {
                var uc = CharUnicodeInfo.GetUnicodeCategory(ch);
                if (uc != UnicodeCategory.NonSpacingMark)
                    sb.Append(ch);
            }

            return sb.ToString()
                     .Normalize(NormalizationForm.FormC)
                     .ToLowerInvariant();
        }
    }
}
