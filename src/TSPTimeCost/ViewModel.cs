using System;

namespace TSPTimeCost
{
    [Serializable]
    public class ViewModel
    {
        private static double _limit;

        public double Limit
        {
            get => _limit;
            set => _limit = value;
        }

        private static string _cities;
        
        public string Cities
        {
            get => _cities;
            set => _cities = value;
        }
    }
}

