using System.Collections.Generic;
using TSPTimeCost.Models;
using TSPTimeCost.TSP;

namespace TSPTimeCost
{
    class Controller
    {
        private readonly ViewModel _viewModel;


        public Controller()
        {
            _viewModel = new ViewModel();
        }


        public void Initialize()
        {
            ProcessInputData processInputData = new ProcessInputData();

            _viewModel.Cities = new List<City>();
            _viewModel.Cities = processInputData.GetCitiesFromGoogleApi();

            ProcessInputData.InitializeSingletons(_viewModel.Cities.Count);
            ProcessInputData.CalculateDistanceMatrixForTollRoads(_viewModel.Cities);
            processInputData.CalculateDistanceMatrixForFreeRoads(_viewModel.Cities);
            processInputData.CalculateCostMatrix(_viewModel.Cities);

            BestPath.Instance.Distance = new AntColonyToll().CalculateDistanceInPath(BestPath.Instance.Order, DistanceMatrixForTollRoads.Instance);
        }

        public string GetOrder()
        {
            string text = "Order: ";
            for (int i = 0; i < _viewModel.Cities.Count - 1; i++)
            {
                text += _viewModel.Cities[BestPath.Instance.Order[i]].Name + "-" + IsTollFragmentInformation(i) + ">";
            }
            text += _viewModel.Cities[BestPath.Instance.Order[_viewModel.Cities.Count - 1]].Name;

            return text;
        }

        public string GetGoals()
        {
            string text = "Goal values: ";
            double goalSum = 0;
            for (int i = 0; i < _viewModel.Cities.Count - 1; i++)
            {
                text += BestPath.Instance.Goal[i].ToString("#.000") + "  ";
                goalSum += BestPath.Instance.Goal[i];
            }

            return text;
        }

        public string GetDuration()
        {
            int hours = (int)(BestPath.Instance.Distance / 3600);
            int minutes = (int)((BestPath.Instance.Distance - hours * 3600) / 60);
            int seconds = (int)(BestPath.Instance.Distance % 60);

            return $"Duration: {hours}:{minutes:00}:{seconds:00}   Cost: {BestPath.Instance.Cost}";
        }

        public string IsTollFragmentInformation(int indexInBestPath)
        {
            City origin = _viewModel.Cities[BestPath.Instance.Order[indexInBestPath]];
            City destination = _viewModel.Cities[BestPath.Instance.Order[indexInBestPath + 1]];
            var indexOrigin = _viewModel.Cities.IndexOf(origin);
            var indexDestination = _viewModel.Cities.IndexOf(destination);


            if (Equals(
                    BestPath.Instance.DistancesInOrder[indexInBestPath],
                    DistanceMatrixForTollRoads.Instance.Value[indexOrigin + _viewModel.Cities.Count * indexDestination])
                && !Equals(
                    DistanceMatrixForTollRoads.Instance.Value[indexOrigin + _viewModel.Cities.Count * indexDestination],
                    DistanceMatrixForFreeRoads.Instance.Value[indexOrigin + _viewModel.Cities.Count * indexDestination])
            )
            {
                return "(T)";
            }
            return "(F)";
        }

        public void TollTSP()
        {
            AntColonyToll ants = new AntColonyToll();
            ants.AntColonySingleThread(_viewModel.Cities);
        }

        public void ClassicTSP()
        {
            AntColonyClassic ants = new AntColonyClassic();
            ants.AntColonySingleThread(_viewModel.Cities);
        }

        public void LimitTSP()
        {
            AntColonyWithLimit ants = new AntColonyWithLimit(_viewModel.Limit);
            ants.AntColonySingleThread(_viewModel.Cities);
        }

        public void EvaluationTSP()
        {
            AntColonyEvaluation ants = new AntColonyEvaluation();
            ants.AntColonySingleThread(_viewModel.Cities);
        }


    }
}

