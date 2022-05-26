using SolTechnology.Core.Guards.Context;

namespace SolTechnology.Core.Guards
{
    public  partial class Guards
    {
        public  GuardsContext2 GuardsContext { get; set; }

        public Guards()
        {
            GuardsContext = new GuardsContext2();
        }
    }
}
