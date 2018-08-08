using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using DreamTravel.ExternalConnection.Interfaces;
using DreamTravel.Models;

namespace DreamTravel.ExternalConnection
{
    public class ProcessInputData : IProcessInputData
    {
        private readonly ICallAPI _apiCaller;

        public ProcessInputData(ICallAPI apiCaller)
        {
            _apiCaller = apiCaller;
        }

        public EvaluationMatrix Execute(List<City> listOfCities,
            EvaluationMatrix evaluationMatrix)
        {
            SetTablesValueAsMax(evaluationMatrix, 0);

            Parallel.Invoke
            (
                () => evaluationMatrix.TollDistances = _apiCaller.DowloadDurationMatrixByTollRoad(listOfCities),
                () => evaluationMatrix.FreeDistances = _apiCaller.DowloadDurationMatrixByFreeRoad(listOfCities)
            );


            Parallel.For(0, listOfCities.Count, i =>
            {
                for (int j = 0; j < listOfCities.Count; j++)
                {
                    int iterator = j + i * listOfCities.Count;

                    if (i == j)
                    {
                        SetTablesValueAsMax(evaluationMatrix, iterator);
                    }

                    else
                    {
                        evaluationMatrix.Costs[iterator] =
                            _apiCaller.DowloadCostBetweenTwoCities(listOfCities[i], listOfCities[j]) / 100;
                    }
                }
            });

            return evaluationMatrix;
        }

        private static void SetTablesValueAsMax(EvaluationMatrix evaluationMatrix, int iterator)
        {
            evaluationMatrix.FreeDistances[iterator] = Double.MaxValue;
            evaluationMatrix.TollDistances[iterator] = Double.MaxValue;
            evaluationMatrix.OptimalDistances[iterator] = Double.MaxValue;
            evaluationMatrix.Goals[iterator] = Double.MaxValue;
            evaluationMatrix.Costs[iterator] = Double.MaxValue;
            evaluationMatrix.OptimalCosts[iterator] = Double.MaxValue;
        }
    }
}