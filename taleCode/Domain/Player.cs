namespace SolTechnology.TaleCode.Domain
{
    public record Player
    {
        public int PlayerId { get; set; }
        public string Name { get; set; }
        public string DateOfBirth { get; set; }
        public string Nationality { get; set; }
        public string Position { get; set; }

        public Player(int playerId, string name, string dateOfBirth, string nationality, string position)
        {
            PlayerId = playerId;
            Name = name;
            DateOfBirth = dateOfBirth;
            Nationality = nationality;
            Position = position;
        }
    }
}
