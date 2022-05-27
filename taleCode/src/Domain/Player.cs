using SolTechnology.Core.Guards;

namespace SolTechnology.TaleCode.Domain
{
    public record Player
    {
        public int ApiId { get; set; }
        public string Name { get; set; }
        public DateTime DateOfBirth { get; set; }
        public string Nationality { get; set; }
        public string Position { get; set; }
        public List<Match> Matches { get; set; }
        public List<Team> Teams { get; set; }

        private Player()
        {
            //required by ORM
        }

        public Player(int apiId, string name, DateTime dateOfBirth, string nationality, string position, List<Match> matches, List<Team> teams)
        {
            var guards = new Guards();
            guards.String(name, nameof(name), x=> x.NotNull().NotEmpty())
                  .Int(apiId, nameof(apiId), x => x.NotZero())
                  .ThrowOnError();

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
