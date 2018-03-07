using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TSPTimeCostConsole
{
    public sealed class ListOfCities
    {

        public List<string> value { get; set; }
        private static volatile ListOfCities instance;
        private static object syncRoot = new Object();

        private ListOfCities()
        {
            value = new List<string>();
        }

        public static ListOfCities Instance
        {
            get
            {
                if (instance == null)
                {
                    lock (syncRoot)
                    {
                        if (instance == null)
                            instance = new ListOfCities();
                    }
                }

                return instance;
            }
        }
    }
}
