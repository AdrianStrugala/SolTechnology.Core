using System;
using System.Collections.Generic;
using System.Linq;
using TSPTimeCost.Models;
using TSPTimeCost.Singletons;

namespace TSPTimeCost.TSP
{

    abstract class AntColonyAbstract : TSP
    {

        // Algorithm parameters
        protected const int NoOfAnts = 50;
        private const double TrailEvaporationCoefficient = 0.3;
        private const double BasicTrialValue = 1;

        //if attractivenesParameter >> trialPreference, program basicaly choses closest city every time
        private const double TrialPreference = 1;
        private const double AttractivenessParameter = 10;
        private double _pheromonePower;
        protected const int NoOfIterations = 10;
        private int _matrixSize;
        protected static int NoOfCities;
        private double[] _attractivenessMatrix;
        private double[] _trialsMatrix;


        protected void InitializeParameters(IDistanceMatrix distanceMatrix)
        {
            _pheromonePower = BestPath.Instance.Distance;
            _matrixSize = distanceMatrix.GetInstance().Distances.Length;
            NoOfCities = BestPath.Instance.Order.Length;
            _trialsMatrix = new double[_matrixSize];
            _attractivenessMatrix = new double[_matrixSize];
        }


        protected void FillAttractivenessMatrix(IDistanceMatrix distanceMatrix)
        {
            for (int i = 0; i < _attractivenessMatrix.Length; i++)
            {
                _attractivenessMatrix[i] = 1 / distanceMatrix.GetInstance().Distances[i];
            }
        }


        protected void FillTrialsMatrix()
        {
            for (int i = 0; i < _trialsMatrix.Length; i++)
            {
                _trialsMatrix[i] = BasicTrialValue;
            }
        }

        protected List<int[]> InitializePathList(List<int[]> pathList)
        {
            for (int i = 0; i < NoOfAnts; i++)
            {
                pathList.Add(InitalizePath());
            }
            return pathList;
        }

        private static int[] InitalizePath()
        {
            int[] path = new int[NoOfCities];
            for (int i = 0; i < NoOfCities; i++) { path[i] = -1; }
            return path;
        }


        //REPRESENTATION OF ANT.
        //RETURNS PATH CHOSEN BY THIS ANT
        protected int[] CalculatePathForSingleAnt()
        {
            double[] probabilityMatrix = new double[_matrixSize];
            var path = InitalizePath();
            path = SetFirstAndLastPointInPath(path);
            probabilityMatrix = InitializeMatrixWithZeros(probabilityMatrix);
            probabilityMatrix = FillProbabilityMatrix(probabilityMatrix);
            probabilityMatrix = ClearProbabilityRowsForFirstAndLastPoint(path, probabilityMatrix);

            //chosing next point until path is full
            for (int j = 1; j < NoOfCities - 1; j++)
            {
                var row = CopyRowFromProbabilityMatrix(j, path, probabilityMatrix);
                row = NormalizeProbabilityValues(row);
                row = SortRowByProbability(row);

                path[j] = DrawNewPointByProbability(row, path);
                probabilityMatrix = ClearProbabilityRowsForGivenPoint(path[j], probabilityMatrix);
            }
            return path;
        }

        private static int[] SetFirstAndLastPointInPath(int[] path)
        {
            path[0] = BestPath.Instance.Order[0];
            path[path.Length - 1] = BestPath.Instance.Order[path.Length - 1];
            return path;
        }

        private double[] InitializeMatrixWithZeros(double[] matrix)
        {
            for (int i = 0; i < matrix.Length; i++)
            {
                matrix[i] = 0;
            }
            return matrix;
        }

        private double[] FillProbabilityMatrix(double[] probabilityMatrix)
        {
            for (int i = 0; i < NoOfCities * NoOfCities; i++)
            {

                probabilityMatrix[i] = Math.Pow(_trialsMatrix[i], TrialPreference) *
                                       Math.Pow(_attractivenessMatrix[i], AttractivenessParameter);
            }
            return probabilityMatrix;
        }

        private double[] ClearProbabilityRowsForFirstAndLastPoint(int[] path, double[] probabilityMatrix)
        {
            for (int i = 0; i < NoOfCities; i++)
            {
                probabilityMatrix[path[path[0]] + i * NoOfCities] = 0;
                probabilityMatrix[path[path[NoOfCities - 1]] + i * NoOfCities] = 0;
            }
            return probabilityMatrix;
        }

        private static double DrawRandomLessThan1()
        {
            Random ran = new Random();
            double result = ran.Next(100);
            result = result / 100;
            return result;
        }

        private VertexAndProbability[] CopyRowFromProbabilityMatrix(int q, int[] path, double[] probabilityMatrix)
        {
            VertexAndProbability[] result = new VertexAndProbability[NoOfCities];
            for (int i = 0; i < NoOfCities; i++)
            {
                result[i] = new VertexAndProbability()
                {
                    Probability = probabilityMatrix[path[q - 1] * NoOfCities + i],
                    Vertex = i
                };
            }

            return result;
        }

        private static VertexAndProbability[] NormalizeProbabilityValues(VertexAndProbability[] row)
        {
            double sum = row.Sum(value => value.Probability);

            foreach (var value in row)
            {
                value.Probability /= sum;
            }
            return row;
        }

        private static VertexAndProbability[] SortRowByProbability(VertexAndProbability[] row)
        {
            Array.Sort(row, (one, two) => one.Probability.CompareTo(two.Probability));
            return row;
        }

        private int DrawNewPointByProbability(VertexAndProbability[] row, int[] path)
        {
            double randomLessThan1 = DrawRandomLessThan1();
            double sum = 0;
            for (int i = 0; i < NoOfCities; i++)
            {
                double temp = row[i].Probability;
                row[i].Probability += sum;
                sum += temp;
            }

            int result = -1;

            for (int i = 0; i < NoOfCities; i++)
            {
                if (row[i].Probability >= randomLessThan1 && !path.Contains(row[i].Vertex))
                {
                    result = row[i].Vertex;
                    break;
                }
            }
            return result;
        }

        private double[] ClearProbabilityRowsForGivenPoint(int nr, double[] probabilityMatrix)
        {
            for (int i = 0; i < NoOfCities; i++)
            {
                probabilityMatrix[nr + i * NoOfCities] = 0;
            }
            return probabilityMatrix;
        }

        protected (int, double) FindMinimumPathInThisIteration(List<int[]> pathList, double min, int nr, IDistanceMatrix distanceMatrix)
        {
            double[] distances = new double[pathList.Count];

            for (int i = 0; i < pathList.Count; i++)
            {
                distances[i] = CalculateDistanceInPath(pathList[i], distanceMatrix);

                if (distances[i] < min)
                {
                    min = distances[i];
                    nr = i;
                }
            }
            return (nr, min);
        }

        protected void UpdateTrialsMatrix(int[] path, IDistanceMatrix distanceMatrix)
        {
            for (int i = 0; i < NoOfCities - 1; i++)
            {
                double distance = CalculateDistanceInPath(path, distanceMatrix);
                _trialsMatrix[path[i] * NoOfCities + path[i + 1]] += (_pheromonePower * TrailEvaporationCoefficient / distance / NoOfAnts);
                _trialsMatrix[path[i + 1] * NoOfCities + path[i]] += (_pheromonePower * TrailEvaporationCoefficient / distance / NoOfAnts);
            }
        }

        protected void EvaporateTrialsMatrix()
        {
            for (int i = 0; i < _matrixSize; i++)
            {
                _trialsMatrix[i] -= TrailEvaporationCoefficient * _trialsMatrix[i];
            }
        }

        protected static void ReplaceBestPathWithCurrentBest(List<int[]> pathList, double minimumPathInThisIteration, int minimumPathNumber, IDistanceMatrix distanceMatrix)
        {
            BestPath.Instance.Distance = minimumPathInThisIteration;
            BestPath.Instance.Order = pathList[minimumPathNumber];

            for (int i = 0; i < NoOfCities - 1; i++)
            {
                BestPath.Instance.DistancesInOrder[i] =
                    distanceMatrix.GetInstance().Distances[BestPath.Instance.Order[i] + NoOfCities * BestPath.Instance.Order[i + 1]];
            }
        }


        public override void SolveTSP()
        {
            
        }
    }

}
