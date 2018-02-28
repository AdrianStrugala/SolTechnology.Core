using System.Collections.Generic;
using System.Linq;
using TSPTimeCost.Models;
using TSPTimeCost.Singletons;

namespace TSPTimeCost.TSP
{
    class AttentionWhore : TSP
    {
        private static List<FanAndStar> _fansAndStars;
        private static List<int> _whoreList;

        public override void SolveTSP()
        {
            _fansAndStars = new List<FanAndStar>();
            _whoreList = new List<int>();

            InitateFansAndStars();

            foreach (var fanAndStar in _fansAndStars)
            {
                FindNearestNeighbour(fanAndStar);
            }

            ConnectLastCityWithFirst();

            int whore = FindWhore();
            while (whore != -1)
            {
                _whoreList.Add(whore);
                CopulateFans(whore);
                whore = FindWhore();
            }

            UpdateBestPath(_fansAndStars, EvaluatedMatrix);
        }



        private static void ConnectLastCityWithFirst()
        {
            _fansAndStars[NoOfCities - 1].Star = 0;
        }

        private static void FindNearestNeighbour(FanAndStar fanAndStar)
        {
            for (int star = 0; star < NoOfCities; star++)
            {

                if (EvaluatedMatrix.Distances[fanAndStar.Fan + NoOfCities * fanAndStar.Star] >
                    EvaluatedMatrix.Distances[fanAndStar.Fan + NoOfCities * star])
                {
                    if (FirstNotConnectedWithLast(fanAndStar.Fan, star) && star != 0 &&
                        _fansAndStars[star].Star != fanAndStar.Fan)
                    {
                        fanAndStar.Star = star;
                    }
                }
            }
        }

        private static void InitateFansAndStars()
        {
            for (int fan = 0; fan < NoOfCities; fan++)
            {
                FanAndStar toadd = new FanAndStar(fan, fan);
                _fansAndStars.Add(toadd);
            }
        }

        private static void CopulateFans(int whore)
        {
            IEnumerable<FanAndStar> fans = _fansAndStars.Where(element => element.Star == whore);

            double minRoad = double.MaxValue;
            FanAndStar newPair = new FanAndStar(-1, -1);

            var fanAndStars = fans as FanAndStar[] ?? fans.ToArray();
            foreach (var origin in fanAndStars)
            {
                foreach (var destination in fanAndStars)
                {
                    if (!_whoreList.Contains(destination.Fan) && destination.Fan != 0)
                    {
                        if (minRoad >
                            EvaluatedMatrix.Distances[origin.Fan + NoOfCities * destination.Fan])
                        {
                            minRoad =
                                EvaluatedMatrix.Distances[origin.Fan + NoOfCities * destination.Fan];
                            newPair.Fan = origin.Fan;
                            newPair.Star = destination.Fan;
                        }
                    }
                }
            }

            foreach (var fanAndStar in _fansAndStars.Where(element => element.Fan == newPair.Fan))
                fanAndStar.Star = newPair.Star;
        }

        private static int FindWhore()
        {
            int whore = -1;
            int actualnoOfFans = 1;

            for (int i = 1; i <= NoOfCities; i++)
            {
                int noOfFans = _fansAndStars.Count(num => num.Star == i);
                if (noOfFans > actualnoOfFans)
                {
                    actualnoOfFans = noOfFans;
                    whore = i;
                }
            }
            return whore;
        }

        private static bool FirstNotConnectedWithLast(int fan, int star)
        {
            return !(fan == 0 && star == NoOfCities - 1) && !(fan == NoOfCities - 1 && star == 0);
        }

    }
}


//using System.Collections.Generic;
//using System.Linq;
//using TSPTimeCost.Models;
//using TSPTimeCost.Singletons;
//
//namespace TSPTimeCost.TSP
//{
//    class AttentionWhore : TSP
//    {
//
//        private static int NoOfCities;
//        private static List<FanAndStar> _fansAndStars;
//        private static List<int> _whoreList;
//
//        public override void SolveTSP()
//        {
//            _fansAndStars = new List<FanAndStar>();
//            _whoreList = new List<int>();
//            NoOfCities = BestPath.Order.Length;
//
//
//            for (int fan = 0; fan < NoOfCities; fan++)
//            {
//                FanAndStar toadd = new FanAndStar(fan, fan);
//                _fansAndStars.Add(toadd);
//            }
//
//            foreach (var fanAndStar in _fansAndStars)
//            {
//                for (int star = 0; star < NoOfCities; star++)
//                {
//
//                    if (DistanceMatrixForFreeRoads.Instance.Distances[fanAndStar.Fan + NoOfCities * fanAndStar.Star] >
//                        DistanceMatrixForFreeRoads.Instance.Distances[fanAndStar.Fan + NoOfCities * star])
//                    {
//
//                        if (FirstNotConnectedWithLast(fanAndStar.Fan, star) && star != 0 )
//                        {
//                            {
//                                fanAndStar.Star = star;
//                            }
//                        }
//                    }
//                }
//            }
//            _fansAndStars[NoOfCities - 1].Star = 0;
//
//            var xd = _fansAndStars;
//
//            int whore = -1;
//
//            do
//            {
//                whore = FindWhore();
//                _whoreList.Add(whore);
//
//                CopulateFans(whore);
//
//                var xd2 = _fansAndStars;
//
//            } while (whore != -1);
//
//            var xd3 = _fansAndStars;
//
//            for (int i = 1; i <= NoOfCities - 1; i++)
//            {
//                BestPath.Order[i] = _fansAndStars.First(element => element.Fan == BestPath.Order[i - 1]).Star;
//            }
//
//            var xd4 = BestPath.Order;
//
//            BestPath.Distance = 0;
//            for (int i = 0; i < NoOfCities - 1; i++)
//            {
//                BestPath.Distance += DistanceMatrixForFreeRoads.Instance.Distances[BestPath.Order[i] * NoOfCities + BestPath.Order[i + 1]];
//            }
//
//            CalculateGoal();
//        }
//
//        private static void CopulateFans(int whore)
//        {
//            IEnumerable<FanAndStar> fans = _fansAndStars.Where(element => element.Star == whore);
//
//            List<int> loosers = new List<int>();
//            for (int i = 1; i < NoOfCities; i++)
//            {
//                loosers.Add(i);
//            }
//
//            foreach (var fanAndStar in _fansAndStars)
//            {
//                if (loosers.Contains(fanAndStar.Star))
//                {
//                    loosers.Remove(fanAndStar.Star);
//                }
//            }
//
//            foreach (var fan in fans)
//            {
//                loosers.Add(fan.Fan);
//            }    
//
//            double minRoad = double.MaxValue;
//            FanAndStar newPair = new FanAndStar(-1, -1);
//
//
//            foreach (var origin in loosers)
//            {
//                foreach (var destination in loosers)
//                {
//                    if (!_whoreList.Contains(destination) && destination != 0)
//                    {
//                        if (minRoad >
//                            DistanceMatrixForFreeRoads.Instance.Distances[origin + NoOfCities * destination])
//                        {
//                            minRoad =
//                                DistanceMatrixForFreeRoads.Instance.Distances[origin + NoOfCities * destination];
//                            newPair.Fan = origin;
//                            newPair.Star = destination;
//                        }
//                    }
//                }
//            }
//
//            var xd6 = newPair;
//
//            _fansAndStars.First(n => n.Fan == newPair.Fan).Star = newPair.Star;
//
////            foreach (var fanAndStar in _fansAndStars.Where(element => element.Fan == newPair.Fan))
////                fanAndStar.Star = newPair.Star;
//            var xd7 = _fansAndStars;
//        }
//
//        private static int FindWhore()
//        {
//            int whore = -1;
//            int actualnoOfFans = 1;
//
//            for (int i = 1; i <= NoOfCities; i++)
//            {
//                int noOfFans = _fansAndStars.Count(num => num.Star == i);
//                if (noOfFans > actualnoOfFans)
//                {
//                    actualnoOfFans = noOfFans;
//                    whore = i;
//                }
//            }
//            return whore;
//        }
//
//        private static bool FirstNotConnectedWithLast(int fan, int star)
//        {
//            return !(fan == 0 && star == NoOfCities - 1) && !(fan == NoOfCities - 1 && star == 0);
//        }
//
//    }
//}
