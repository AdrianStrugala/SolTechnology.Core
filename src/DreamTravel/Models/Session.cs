using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace DreamTravel.Models
{
    public class Session
    {
        public int Id { get; set; }
        public int NoOfCities { get; set; }
        public double[] FreeDistances { get; set; }
        public double[] TollDistances { get; set; }
        public double[] Costs { get; set; }
    }
}
