using System.Diagnostics;
using System.Threading.Tasks;
using TSPTimeCost.Models;
using TSPTimeCost.Singletons;
using TSPTimeCost.TSP;
using TSPTimeCost.TSP.AntColony;
using AttentionWhore = TSPTimeCost.TSP.AttentionWhore;

namespace TSPTimeCost
{
    class Controller
    {
        private double _goalSum;
        private readonly ViewModel _viewModel;


        public Controller()
        {
            _viewModel = new ViewModel();
        }


        public async Task InitializeAsync()
        {
            Task initializeSingletons = Task.Factory.StartNew(ProcessInputData.InitializeSingletons) ;
            initializeSingletons.Wait();
            await Task.Factory.StartNew(ProcessInputData.CalculateDistanceMatrixForFreeRoads);
            await Task.Factory.StartNew(ProcessInputData.CalculateDistanceMatrixForTollRoads);
            await Task.Factory.StartNew(ProcessInputData.CalculateCostMatrix);
            await Task.Factory.StartNew(ProcessInputData.CalculateDistanceMatrixEvaluated);

           
            BestPath.Instance.Distance = new AntColonyToll().CalculateDistanceInPath(BestPath.Instance.Order, DistanceMatrixForTollRoads.Instance);
        }

        public string GetOrder()
        {
            string text = "Order: ";
            for (int i = 0; i < Cities.Instance.ListOfCities.Count - 1; i++)
            {
                text += Cities.Instance.ListOfCities[BestPath.Instance.Order[i]].Name + "-" + IsTollFragmentInformation(i) + ">";
            }
            text += Cities.Instance.ListOfCities[BestPath.Instance.Order[Cities.Instance.ListOfCities.Count - 1]].Name;

            return text;
        }

        public string GetGoals()
        {
            _goalSum = 0;
            string text = "Goal values: ";
            for (int i = 0; i < Cities.Instance.ListOfCities.Count - 1; i++)
            {
                text += BestPath.Instance.Goal[i].ToString("#.000") + "  ";
            }

            return text;
        }

        public string GetDuration()
        {
            int hours = (int)(BestPath.Instance.Distance / 3600);
            int minutes = (int)((BestPath.Instance.Distance - hours * 3600) / 60);
            int seconds = (int)(BestPath.Instance.Distance % 60);

            _goalSum = 0;
            for (int i = 0; i < Cities.Instance.ListOfCities.Count - 1; i++)
            {
                _goalSum += BestPath.Instance.Goal[i];
            }

            return $"Duration: {hours}:{minutes:00}:{seconds:00}   Cost: {BestPath.Instance.Cost}   Goal: {_goalSum:000.000}";
        }

        public string IsTollFragmentInformation(int indexInBestPath)
        {
            City origin = Cities.Instance.ListOfCities[BestPath.Instance.Order[indexInBestPath]];
            City destination = Cities.Instance.ListOfCities[BestPath.Instance.Order[indexInBestPath + 1]];
            var indexOrigin = Cities.Instance.ListOfCities.IndexOf(origin);
            var indexDestination = Cities.Instance.ListOfCities.IndexOf(destination);


            return Equals(
                BestPath.Instance.DistancesInOrder[indexInBestPath],
                DistanceMatrixForFreeRoads.Instance.Distances[indexOrigin + Cities.Instance.ListOfCities.Count * indexDestination]) ? "(F)" : "(T)";
        }

        public void TollTSP()
        {
            AntColonyToll ants = new AntColonyToll();
            ants.SolveTSP();
        }

        public void ClassicTSP()
        {
            AntColonyClassic ants = new AntColonyClassic();
            ants.SolveTSP();
        }

        public void LimitTSP()
        {
            AntColonyWithLimit ants = new AntColonyWithLimit(_viewModel.Limit);
            ants.SolveTSP();
        }

        public void EvaluationTSP()
        {
            AntColonyEvaluation ants = new AntColonyEvaluation();
            ants.SolveTSP();
        }


        public void AttentionWhore()
        {
            AttentionWhore attentionWhore = new AttentionWhore();
            attentionWhore.SolveTSP();
        }

        public void God()
        {
            God god = new God();
            god.SolveTSP();
        }
    }
}

