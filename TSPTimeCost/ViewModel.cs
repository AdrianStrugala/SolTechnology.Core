using System.Collections.Generic;
using System.Windows.Forms.DataVisualization.Charting;
using TSPTimeCost.Models;

namespace TSPTimeCost
{
    public class ViewModel
    {
        private static List<City> _cities;
        private static double _limit;

        public List<City> Cities
        {
            get => _cities;
            set => _cities = value;
        }

        public double Limit
        {
            get => _limit;
            set => _limit = value;
        }

    }
}

