/***************************************************/
/* Singleton containing list of cities             */
/***************************************************/

using System.Collections.Generic;
using TSPTimeCost.Models;

namespace TSPTimeCost.Singletons
{
    public sealed class Cities
    {
        public List<City> ListOfCities { get; set; }

        private static volatile Cities _instance;
        private static readonly object SyncRoot = new object();

        private Cities() { }

        public static Cities Instance
        {
            get
            {
                if (_instance == null)
                {
                    lock (SyncRoot)
                    {
                        if (_instance == null)
                            _instance = new Cities();
                    }
                }

                return _instance;
            }
        }
    }
}