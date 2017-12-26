using System;
using System.Collections.Generic;
using System.Linq;
using TSPTimeCost.Models;

namespace TSPTimeCost.TSP
{

    abstract class AntColony
    {
        //Goal parameters

        public static double FuelPrice { get; } = 1.26;
        public static double RoadVelocity { get; } = 70;
        public static double HighwayVelocity { get; } = 120;
        public static double RoadCombustion { get; } = 0.06; //per km
        public static double GoalFreeRoad { get; } = RoadVelocity * RoadCombustion * FuelPrice;

        // Algorithm parameters
        protected const int noOfAnts = 50;
        private const double trailEvaporationCoefficient = 0.3;
        private const double basicTrialValue = 1;

        //if attractivenesParameter >> trialPreference, program basicaly choses closest city every time
        private const double trialPreference = 1;
        private const double attractivenessParameter = 10;
        private double pheromonePower;
        protected const int noOfIterations = 10;
        private int matrixSize;
        protected static int noOfPoints;
        private double[] attractivenessMatrix;
        private double[] trialsMatrix;


        public abstract void AntColonySingleThread(List<City> cities);


        public double CalculateDistanceInPath(int[] path, IDistanceMatrix distanceMatrix)
        {
            double result = 0;
            noOfPoints = path.Length;

            for (int i = 0; i < noOfPoints - 1; i++)
            {
                result += distanceMatrix.GetInstance().Value[path[i] * noOfPoints + path[i + 1]];
            }
            return result;
        }

        protected void InitializeParameters(IDistanceMatrix distanceMatrix)
        {
            pheromonePower = BestPath.Instance.Distance;
            matrixSize = distanceMatrix.GetInstance().Value.Length;
            noOfPoints = (int)Math.Sqrt(matrixSize);
            trialsMatrix = new double[matrixSize];
            attractivenessMatrix = new double[matrixSize];
        }


        protected void FillAttractivenessMatrix(IDistanceMatrix distanceMatrix)
        {
            for (int i = 0; i < attractivenessMatrix.Length; i++)
            {
                attractivenessMatrix[i] = 1 / distanceMatrix.GetInstance().Value[i];
            }
        }


        protected void FillTrialsMatrix()
        {
            for (int i = 0; i < trialsMatrix.Length; i++)
            {
                trialsMatrix[i] = basicTrialValue;
            }
        }

        protected List<int[]> InitializePathList(List<int[]> pathList)
        {
            for (int i = 0; i < noOfAnts; i++)
            {
                pathList.Add(InitalizePath());
            }
            return pathList;
        }

        private static int[] InitalizePath()
        {
            int[] path = new int[noOfPoints];
            for (int i = 0; i < noOfPoints; i++) { path[i] = -1; }
            return path;
        }


        //REPRESENTATION OF ANT.
        //RETURNS PATH CHOSEN BY THIS ANT
        protected int[] CalculatePathForSingleAnt()
        {
            double[] probabilityMatrix = new double[matrixSize];
            var path = InitalizePath();
            path = SetFirstAndLastPointInPath(path);
            probabilityMatrix = InitializeMatrixWithZeros(probabilityMatrix);
            probabilityMatrix = FillProbabilityMatrix(probabilityMatrix);
            probabilityMatrix = ClearProbabilityRowsForFirstAndLastPoint(path, probabilityMatrix);

            //chosing next point until path is full
            for (int j = 1; j < noOfPoints - 1; j++)
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
            for (int i = 0; i < noOfPoints * noOfPoints; i++)
            {

                probabilityMatrix[i] = Math.Pow(trialsMatrix[i], trialPreference) *
                                       Math.Pow(attractivenessMatrix[i], attractivenessParameter);
            }
            return probabilityMatrix;
        }

        private double[] ClearProbabilityRowsForFirstAndLastPoint(int[] path, double[] probabilityMatrix)
        {
            for (int i = 0; i < noOfPoints; i++)
            {
                probabilityMatrix[path[path[0]] + i * noOfPoints] = 0;
                probabilityMatrix[path[path[noOfPoints - 1]] + i * noOfPoints] = 0;
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
            VertexAndProbability[] result = new VertexAndProbability[noOfPoints];
            for (int i = 0; i < noOfPoints; i++)
            {
                result[i] = new VertexAndProbability()
                {
                    Probability = probabilityMatrix[path[q - 1] * noOfPoints + i],
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
            for (int i = 0; i < noOfPoints; i++)
            {
                double temp = row[i].Probability;
                row[i].Probability += sum;
                sum += temp;
            }

            int result = -1;

            for (int i = 0; i < noOfPoints; i++)
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
            for (int i = 0; i < noOfPoints; i++)
            {
                probabilityMatrix[nr + i * noOfPoints] = 0;
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
            for (int i = 0; i < noOfPoints - 1; i++)
            {
                double distance = CalculateDistanceInPath(path, distanceMatrix);
                trialsMatrix[path[i] * noOfPoints + path[i + 1]] += (pheromonePower * trailEvaporationCoefficient / distance / noOfAnts);
                trialsMatrix[path[i + 1] * noOfPoints + path[i]] += (pheromonePower * trailEvaporationCoefficient / distance / noOfAnts);
            }
        }

        protected void EvaporateTrialsMatrix()
        {
            for (int i = 0; i < matrixSize; i++)
            {
                trialsMatrix[i] -= trailEvaporationCoefficient * trialsMatrix[i];
            }
        }

        protected static void ReplaceBestPathWithCurrentBest(List<int[]> pathList, double minimumPathInThisIteration, int minimumPathNumber, IDistanceMatrix distanceMatrix)
        {
            BestPath.Instance.Distance = minimumPathInThisIteration;
            BestPath.Instance.Order = pathList[minimumPathNumber];

            for (int i = 0; i < noOfPoints - 1; i++)
            {
                BestPath.Instance.DistancesInOrder[i] =
                    distanceMatrix.GetInstance().Value[BestPath.Instance.Order[i] + noOfPoints * BestPath.Instance.Order[i + 1]];
            }
        }


        protected void NormalizeDistances()
        {
            BestPath.Instance.Distance = 0;
            for (int i = 0; i < noOfPoints - 1; i++)
            {
                BestPath.Instance.Distance += BestPath.Instance.DistancesInOrder[i];
            }
        }

        //G=  ΔC/ΔT
        protected List<TimeDifferenceAndCost> CalculateGoal(List<City> cities)
        {
            List<TimeDifferenceAndCost> goalList = new List<TimeDifferenceAndCost>();


            for (int i = 0; i < cities.Count - 1; i++)
            {
                City origin = cities[BestPath.Instance.Order[i]];
                City destination = cities[BestPath.Instance.Order[i + 1]];
                var indexOrigin = cities.IndexOf(origin);
                var indexDestination = cities.IndexOf(destination);

                TimeDifferenceAndCost goalItem =
                    new TimeDifferenceAndCost
                    {
                        FeeCost = CostMatrix.Instance.Value[indexOrigin + cities.Count * indexDestination],
                        Index = i,
                        TimeDifference =
                            DistanceMatrixForFreeRoads.Instance.Value[indexOrigin + cities.Count * indexDestination] -
                            DistanceMatrixForTollRoads.Instance.Value[indexOrigin + cities.Count * indexDestination]
                    };

                // C_G=s×combustion×fuel price [€]
                goalItem.GasolineCostFree =
                    DistanceMatrixForFreeRoads.Instance.Value[indexOrigin + cities.Count * indexDestination] /
                    3600 * RoadVelocity * RoadCombustion * FuelPrice;

                goalItem.GasolineCostToll =
                    DistanceMatrixForTollRoads.Instance.Value[indexOrigin + cities.Count * indexDestination] /
                    3600 * HighwayVelocity * RoadCombustion * 1.25 * FuelPrice;

                if (BestPath.Instance.DistancesInOrder[i] == DistanceMatrixForFreeRoads.Instance.Value[indexOrigin + cities.Count * indexDestination]) //free road
                {
                   // goalItem.Goal = goalItem.GasolineCostFree / (DistanceMatrixForFreeRoads.Instance.Value[indexOrigin + cities.Count * indexDestination] / 3600);
                    goalItem.Goal = GoalFreeRoad; //const value, equal to above
                }

                else // C_G = s × combustion × fuel price [€] 
                {                  
                    goalItem.Goal = (goalItem.FeeCost + goalItem.GasolineCostToll - goalItem.GasolineCostFree) / (goalItem.TimeDifference / 3600);
                }
               
                BestPath.Instance.Goal[i] = goalItem.Goal;

                goalList.Add(goalItem);
            }

            return goalList;
        }

    }

}
