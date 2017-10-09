using System;
using System.Collections.Generic;
using System.Linq;
using Parallel_Ants.Models;

namespace TSPTimeCost.TSP {

    class AntColony {
        // Algorithm parameters
        private const int noOfAnts = 50;
        private const double trailEvaporationCoefficient = 0.3;
        private const double basicTrialValue = 1;

        //if attractivenesParameter >> trialPreference, program basicaly choses closest city every time
        private const double trialPreference = 1;
        private const double attractivenessParameter = 10;
        private double pheromonePower;
        private const int noOfIterations = 10;
        private int matrixSize;
        private int noOfPoints;
        private double[] attractivenessMatrix;
        private double[] trialsMatrix;

        public void AntColonySingleThread() {

            InitializeParameters();
            FillAttractivenessMatrix();
            FillTrialsMatrix();

            //each iteration is one trip of the ants
            for (int j = 0; j < noOfIterations; j++) {
                List<int[]> pathList = new List<int[]>();
                double minimumPathInThisIteration = Double.MaxValue;
                int minimumPathNumber = -1;

                pathList = InitializePathList(pathList);

                //proceed for each ant
                for (int i = 0; i < noOfAnts; i++) {
                    pathList[i] = CalculatePathForSingleAnt();
                }
                //must be separate, to not affect ants in the same iteration
                for (int i = 0; i < noOfAnts; i++) {
                    UpdateTrialsMatrix(pathList[i]);
                }

                EvaporateTrialsMatrix();
      
                //if its last iteration
                if (j == noOfIterations - 1) {
                    (minimumPathNumber, minimumPathInThisIteration) = FindMinimumPathInThisIteration(pathList, minimumPathInThisIteration, minimumPathNumber);
                    ReplaceBestPathWithCurrentBest(pathList, minimumPathInThisIteration, minimumPathNumber);
                }
            }
        }//end of Ant Colony


        public double CalculateDistanceInPath(int[] path) {
            double result = 0;
            int noOfPoints = path.Length;

            for (int i = 0; i < noOfPoints - 1; i++) {
                result += DistanceMatrix.Instance.value[path[i] * noOfPoints + path[i + 1]];
            }
            return result;
        }

        private void InitializeParameters() {
            pheromonePower = BestPath.Instance.distance;
            matrixSize = DistanceMatrix.Instance.value.Length;
            noOfPoints = (int)Math.Sqrt(matrixSize);
            trialsMatrix = new double[matrixSize];
            attractivenessMatrix = new double[matrixSize];
        }
        private void FillAttractivenessMatrix() {
            for (int i = 0; i < attractivenessMatrix.Length; i++) {
                attractivenessMatrix[i] = (1 / (DistanceMatrix.Instance.value[i]));
            }
        }

        private void FillTrialsMatrix() {
            for (int i = 0; i < trialsMatrix.Length; i++) {
                trialsMatrix[i] = basicTrialValue;
            }
        }

        private List<int[]> InitializePathList(List<int[]> pathList) {
            for (int i = 0; i < noOfAnts; i++) {
                pathList.Add(InitalizePath(noOfPoints));
            }
            return pathList;
        }

        private static int[] InitalizePath(int noOfPoints) {
            int[] path = new int[noOfPoints];
            for (int i = 0; i < noOfPoints; i++) { path[i] = -1; }
            return path;
        }


        //REPRESENTATION OF ANT.
        //RETURNS PATH CHOSEN BY THIS ANT
        private int[] CalculatePathForSingleAnt() {
            double[] probabilityMatrix = new double[matrixSize];
            var path = InitalizePath(noOfPoints);
            path = SetFirstAndLastPointInPath(path);
            probabilityMatrix = InitializeMatrixWithZeros(probabilityMatrix);
            probabilityMatrix = FillProbabilityMatrix(probabilityMatrix);
            probabilityMatrix = ClearProbabilityRowsForFirstAndLastPoint(path, probabilityMatrix);

            //chosing next point until path is full
            for (int j = 1; j < noOfPoints - 1; j++) {
                var row = CopyRowFromProbabilityMatrix(j, path, probabilityMatrix);
                row = NormalizeProbabilityValues(row);
                row = SortRowByProbability(row);

                path[j] = DrawNewPointByProbability(row, path);
                probabilityMatrix = ClearProbabilityRowsForGivenPoint(path[j], probabilityMatrix);
            }
            return path;
        }

        private static int[] SetFirstAndLastPointInPath(int[] path) {
            path[0] = BestPath.Instance.order[0];
            path[path.Length - 1] = BestPath.Instance.order[path.Length - 1];
            return path;
        }

        private double[] InitializeMatrixWithZeros(double[] matrix) {
            for (int i = 0; i < matrix.Length; i++) {
                matrix[i] = 0;
            }
            return matrix;
        }

        private double[] FillProbabilityMatrix(double[] probabilityMatrix) {
            for (int i = 0; i < noOfPoints * noOfPoints; i++) {

                probabilityMatrix[i] = Math.Pow(trialsMatrix[i], trialPreference) *
                                       Math.Pow(attractivenessMatrix[i], attractivenessParameter);
            }
            return probabilityMatrix;
        }

        private double[] ClearProbabilityRowsForFirstAndLastPoint(int[] path, double[] probabilityMatrix) {
            for (int i = 0; i < noOfPoints; i++) {
                probabilityMatrix[path[path[0]] + i * noOfPoints] = 0;
                probabilityMatrix[path[path[noOfPoints - 1]] + i * noOfPoints] = 0;
            }
            return probabilityMatrix;
        }

        private static double DrawRandomLessThan1() {
            Random ran = new Random();
            double result = ran.Next(100);
            result = result / 100;
            return result;
        }

        private VertexAndProbability[] CopyRowFromProbabilityMatrix(int q, int[] path, double[] probabilityMatrix) {
            VertexAndProbability[] result = new VertexAndProbability[noOfPoints];
            for (int i = 0; i < noOfPoints; i++) {
                result[i] = new VertexAndProbability() {
                    Probability = probabilityMatrix[path[q - 1] * noOfPoints + i],
                    Vertex = i
                };
            }

            return result;
        }

        private static VertexAndProbability[] NormalizeProbabilityValues(VertexAndProbability[] row) {
            double sum = row.Sum(value => value.Probability);

            foreach (var value in row) {
                value.Probability /= sum;
            }
            return row;
        }

        private static VertexAndProbability[] SortRowByProbability(VertexAndProbability[] row) {
            Array.Sort(row, (one, two) => one.Probability.CompareTo(two.Probability));
            return row;
        }

        private int DrawNewPointByProbability(VertexAndProbability[] row, int[] path) {
            double randomLessThan1 = DrawRandomLessThan1();
            double sum = 0;
            for (int i = 0; i < noOfPoints; i++) {
                double temp = row[i].Probability;
                row[i].Probability += sum;
                sum += temp;
            }

            int result = -1;

            for (int i = 0; i < noOfPoints; i++) {
                if (row[i].Probability >= randomLessThan1 && !path.Contains(row[i].Vertex)) {
                    result = row[i].Vertex;
                    break;
                }
            }
            return result;
        }

        private double[] ClearProbabilityRowsForGivenPoint(int nr, double[] probabilityMatrix) {
            for (int i = 0; i < noOfPoints; i++) {
                probabilityMatrix[nr + i * noOfPoints] = 0;
            }
            return probabilityMatrix;
        }

        private (int, double) FindMinimumPathInThisIteration(List<int[]> pathList, double min, int nr) {
            double[] distances = new double[pathList.Count];

            for (int i = 0; i < pathList.Count; i++) {
                distances[i] = CalculateDistanceInPath(pathList[i]);

                if (distances[i] < min) {
                    min = distances[i];
                    nr = i;
                }
            }
            return (nr, min);
        }

        private void UpdateTrialsMatrix(int[] path) {
            for (int i = 0; i < noOfPoints - 1; i++) {
                double distance = CalculateDistanceInPath(path);
                trialsMatrix[path[i] * noOfPoints + path[i + 1]] += (pheromonePower * trailEvaporationCoefficient/ distance /noOfAnts);
                trialsMatrix[path[i + 1] * noOfPoints + path[i]] += (pheromonePower * trailEvaporationCoefficient / distance / noOfAnts);
            }
        }

        private void EvaporateTrialsMatrix() {
            for (int i = 0; i < matrixSize; i++) {
                trialsMatrix[i] -= trailEvaporationCoefficient * trialsMatrix[i];
            }
        }

        private static void ReplaceBestPathWithCurrentBest(List<int[]> pathList, double minimumPathInThisIteration,
            int minimumPathNumber) {
            BestPath.Instance.distance = minimumPathInThisIteration;
            BestPath.Instance.order = pathList[minimumPathNumber];
        }


    }

}
