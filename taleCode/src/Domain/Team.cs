using SolTechnology.Core.Guards;

namespace SolTechnology.TaleCode.Domain
{
    public record Team
    {
        public long Id { get; set; }
        public int PlayerApiId { get; set; }
        public DateTime DateFrom { get; set; }
        public DateTime DateTo { get; set; }
        public string Name { get; set; }

        private Team()
        {
            //required by ORM
        }

        public Team(int playerApiId, DateTime dateFrom, DateTime dateTo, string name)
        {
            Guards.Int(playerApiId, nameof(playerApiId)).NotZero();
            Guards.String(name, nameof(name)).NotNull().NotEmpty();

            PlayerApiId = playerApiId;
            DateFrom = dateFrom;
            DateTo = dateTo;
            Name = name;
        }
    }
}
