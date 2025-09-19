namespace DepthCharts.Application.Common
{
    public static class Guard
    {
        public static void NotEmpty(string value, string propName)
        {
            if(string.IsNullOrWhiteSpace(value))
            {
                throw new ValidationException($"{propName} cannot be null.");
            }
        }

        public static void Positive(int value, string propName)
        {
            if (value < 0)
            {
                throw new ValidationException($"{propName} must be non-negative number.");
            }
        }
    }
}
