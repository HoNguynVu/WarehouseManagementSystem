using System;

namespace Application.Helpers
{
    public static class IdGenerator
    {
        public static string GenerateUniqueSuffix()
        {
            return DateTime.Now.ToString("yyMMddHHmmss") + new Random().Next(100, 999).ToString();
        }

        public static string GenerateId(string prefix)
        {
            return $"{prefix}-{GenerateUniqueSuffix()}";
        }
    }
}
