using SolTechnology.Core.Guards;

namespace SolTechnology.TaleCode.Domain.Player
{
    public record Player
    {
        public int ApiId { get; set; }
        public string Name { get; set; }
        public string DateOfBirth { get; set; }
        public string Nationality { get; set; }
        public string Position { get; set; }
        public List<Match> Matches { get; set; }

        public Player(int apiId, string name, string dateOfBirth, string nationality, string position, List<Match> matches)
        {
            Guards.StringNotNullNorEmpty(name, nameof(name));
            Guards.IntNotZero(apiId, nameof(apiId));

            ApiId = apiId;
            Name = name;
            DateOfBirth = dateOfBirth;
            Nationality = nationality;
            Position = position;
            Matches = matches;
        }
    }
}
