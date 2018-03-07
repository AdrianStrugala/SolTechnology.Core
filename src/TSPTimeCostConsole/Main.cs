using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TSPTimeCost;

namespace TSPTimeCostConsole
{
    class Program
    {
        static void Main(string[] args)
        {
            //            List<Road> roads = new List<Road>();
            //
            //            ProcessInputData PID = new ProcessInputData();
            //
            //            roads = PID.ReadInputFile();
            //
            //            foreach (var road in roads)
            //            {
            //                PID.ConvertTimeToDecimal(road);
            //            }
            //
            //            PID.CreateListOfCities(roads);
            //            PID.InitializeSingletons(ListOfCities.Instance.value.Count);
            //            PID.CreateMatrixes(roads);
            //
            //            int i = NoCostMatrix.Instance.value.Length;
            //            int j = CostMatrix.Instance.value.Length;



            Task taskA = Task.Factory.StartNew(() => DoSomeWork(10000, "A"));

            Task taskB = Task.Factory.StartNew(() => DoSomeWork(10000, "B"));

            Console.ReadKey();
        }

        private static void DoSomeWork(int p0, string letter)
        {
            for (int i = 0;  i < p0; i++) {
                Console.WriteLine(letter + i);
            }
        }
    }
}
