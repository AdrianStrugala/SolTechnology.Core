namespace SolTechnology.TaleCode.StaticData
{
    public static class DateProvider
    {
        public static DateTime DateMin() => DateTime.Parse("1920-05-18");
        public static DateTime DateMax() => DateTime.UtcNow;
    }
}
