using SolTechnology.Core.Guards;

namespace SolTechnology.TaleCode.Domain
{
    public record Player
    {
        public int ApiId { get; set; }
        public string Name { get; set; }
        public string DateOfBirth { get; set; }
        public string Nationality { get; set; }
        public string Position { get; set; }
        public List<Match> Matches { get; set; }
        public List<Team> Teams { get; set; }

        private Player()
        {
            //required by ORM
        }

        public Player(int apiId, string name, string dateOfBirth, string nationality, string position, List<Match> matches, List<Team> teams)
        {
            Guards.String(name, nameof(name)).NotNull().NotEmpty(); ;
            Guards.Int(apiId, nameof(apiId)).NotZero();

            ApiId = apiId;
            Name = name;
            DateOfBirth = dateOfBirth;
            Nationality = nationality;
            Position = position;
            Matches = matches;
            Teams = teams;
        }
    }
}
