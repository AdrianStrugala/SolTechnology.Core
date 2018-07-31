using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using TravelingSalesmanProblem.Models;

namespace TravelingSalesmanProblem
{
    public class AntColony : TSPAbstract
    {
        // Algorithm parameters
        protected const int NoOfIterations = 10;
        protected static readonly int NoOfAnts = 10000 / NoOfIterations;
        private const double TrailEvaporationCoefficient = 0.3;
        private const double BasicTrialValue = 1;

        //if attractivenesParameter >> trialPreference, program basicaly choses closest city every time
        private const double TrialPreference = 1;
        private const double AttractivenessParameter = 10;
        private double _pheromonePower;
        private int _matrixSize;
        private double[] _attractivenessMatrix;
        private double[] _trialsMatrix;
        private int[] _bestPath;
        private static int _noOfCities;

        public override int[] SolveTSP(double[] distances)
        {
            _noOfCities = (int)Math.Sqrt(distances.Length);
            InitializeParameters(distances);
            FillAttractivenessMatrix(distances);
            FillTrialsMatrix();

            List<int[]> pathList = new List<int[]>(NoOfAnts);
            //each iteration is one trip of the ants
            for (int j = 0; j < NoOfIterations; j++)
            {
                for(int i = 0; i <NoOfAnts; i++) {
                    pathList.Add(CalculatePathForSingleAnt());
                }

                //must be separate, to not affect ants in the same iteration
                Parallel.For(0, NoOfAnts, i =>
                    UpdateTrialsMatrix(pathList[i], distances)
                );

                EvaporateTrialsMatrix();
            }
            return FindMinimumPathInListOfPaths(pathList, distances, _noOfCities);
        }//end of Ant Colony


        private void InitializeParameters(double[] distances)
        {
            _bestPath = new int[_noOfCities];
            for (int i = 0; i < _noOfCities; i++) { _bestPath[i] = i; }
            _pheromonePower = CalculateDistanceInPath(_bestPath, distances);
            _matrixSize = distances.Length;
            _trialsMatrix = new double[_matrixSize];
            _attractivenessMatrix = new double[_matrixSize];
        }

        private void FillAttractivenessMatrix(double[] distances)
        {
            for (int i = 0; i < _attractivenessMatrix.Length; i++)
            {
                _attractivenessMatrix[i] = 1 / distances[i];
            }
        }


        private void FillTrialsMatrix()
        {
            for (int i = 0; i < _trialsMatrix.Length; i++)
            {
                _trialsMatrix[i] = BasicTrialValue;
            }
        }        

        private static int[] InitalizePath(int noOfCities)
        {
            int[] path = new int[noOfCities];
            for (int i = 0; i < noOfCities; i++) { path[i] = -1; }
            return path;
        }


        //REPRESENTATION OF ANT.
        //RETURNS PATH CHOSEN BY THIS ANT
        private int[] CalculatePathForSingleAnt()
        {
            double[] probabilityMatrix = new double[_matrixSize];
            int[] path = InitalizePath(_noOfCities);
            path = SetFirstAndLastPointInPath(path);
            probabilityMatrix = InitializeMatrixWithZeros(probabilityMatrix);
            probabilityMatrix = FillProbabilityMatrix(probabilityMatrix);
            probabilityMatrix = ClearProbabilityRowsForFirstAndLastPoint(path, probabilityMatrix);

            //chosing next point until path is full
            for (int j = 1; j < _noOfCities - 1; j++)
            {
                var row = CopyRowFromProbabilityMatrix(j, path, probabilityMatrix);
                row = NormalizeProbabilityValues(row);
                row = SortRowByProbability(row);

                path[j] = DrawNewPointByProbability(row, path);
                probabilityMatrix = ClearProbabilityRowsForGivenPoint(path[j], probabilityMatrix);
            }
            return path;
        }


        private int[] SetFirstAndLastPointInPath(int[] path)
        {
            path[0] = _bestPath[0];
            path[path.Length - 1] = _bestPath[path.Length - 1];
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
            for (int i = 0; i < _noOfCities * _noOfCities; i++)
            {

                probabilityMatrix[i] = Math.Pow(_trialsMatrix[i], TrialPreference) *
                                       Math.Pow(_attractivenessMatrix[i], AttractivenessParameter);
            }
            return probabilityMatrix;
        }


        private double[] ClearProbabilityRowsForFirstAndLastPoint(int[] path, double[] probabilityMatrix)
        {
            for (int i = 0; i < _noOfCities; i++)
            {
                probabilityMatrix[path[path[0]] + i * _noOfCities] = 0;
                probabilityMatrix[path[path[_noOfCities - 1]] + i * _noOfCities] = 0;
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
            VertexAndProbability[] result = new VertexAndProbability[_noOfCities];
            for (int i = 0; i < _noOfCities; i++)
            {
                result[i] = new VertexAndProbability()
                {
                    Probability = probabilityMatrix[path[q - 1] * _noOfCities + i],
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
            for (int i = 0; i < _noOfCities; i++)
            {
                double temp = row[i].Probability;
                row[i].Probability += sum;
                sum += temp;
            }

            int result = -1;

            for (int i = 0; i < _noOfCities; i++)
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
            for (int i = 0; i < _noOfCities; i++)
            {
                probabilityMatrix[nr + i * _noOfCities] = 0;
            }
            return probabilityMatrix;
        }


        private void UpdateTrialsMatrix(int[] path, double[] distances)
        {
            for (int i = 0; i < _noOfCities - 1; i++)
            {
                double distance = CalculateDistanceInPath(path, distances);
                _trialsMatrix[path[i] * _noOfCities + path[i + 1]] += (_pheromonePower * TrailEvaporationCoefficient / distance / NoOfAnts);
                _trialsMatrix[path[i + 1] * _noOfCities + path[i]] += (_pheromonePower * TrailEvaporationCoefficient / distance / NoOfAnts);
            }
        }


        private void EvaporateTrialsMatrix()
        {
            for (int i = 0; i < _matrixSize; i++)
            {
                _trialsMatrix[i] -= TrailEvaporationCoefficient * _trialsMatrix[i];
            }
        }
    }

}
