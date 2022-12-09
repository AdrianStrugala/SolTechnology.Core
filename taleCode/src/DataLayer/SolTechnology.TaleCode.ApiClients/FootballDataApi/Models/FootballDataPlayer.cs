namespace SolTechnology.TaleCode.ApiClients.FootballDataApi.Models
{
    public class FootballDataPlayer
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public DateTime DateOfBirth { get; set; }
        public string Nationality { get; set; }
        public string Position { get; set; }
        public List<FootballDataMatch> Matches { get; set; }
    }
}
