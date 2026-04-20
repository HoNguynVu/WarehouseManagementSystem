namespace Application.Helpers
{
    public static class IdGenerator
    {
        public static string GenerateId(string prefix = "")
        {
            var ramdomId = Guid.NewGuid().ToString("N").Substring(0, 10).ToUpper();
            if(string.IsNullOrEmpty(prefix))
            {
                return ramdomId;
            }
            return $"{prefix.ToUpper()}-{ramdomId}";
        }
    }
}
