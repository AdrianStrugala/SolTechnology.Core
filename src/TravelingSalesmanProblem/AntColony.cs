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
        private const double TrailEvaporationCoefficient = 0.99;
        private const double BasicTrialValue = 1;

        //if attractivenesParameter >> trialPreference, program basicaly choses closest city every time
        private const double TrialPreference = 1;
        private const double AttractivenessParameter = 4.2;
        private double _pheromonePower;
        private int _matrixSize;
        private List<double> _attractivenessMatrix;
        private List<double> _trialsMatrix;
        private List<int> _bestPath;
        private static int _noOfCities;

        public override List<int> SolveTSP(List<double> distances)
        {
            _noOfCities = (int)Math.Sqrt(distances.Count);
            InitializeParameters(distances);
            FillAttractivenessMatrix(distances);
            FillTrialsMatrix();

            List<List<int>> pathList = new List<List<int>>(NoOfAnts);
            //each iteration is one trip of the ants
            for (int j = 0; j < NoOfIterations; j++)
            {
                for (int i = 0; i < NoOfAnts; i++)
                {
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


        private void InitializeParameters(List<double> distances)
        {
            _bestPath = new List<int>(_noOfCities);
            for (int i = 0; i < _noOfCities; i++) { _bestPath.Add(i); }
            _pheromonePower = CalculateDistanceInPath(_bestPath, distances);
            _matrixSize = distances.Count;
            _trialsMatrix = new List<double>(_matrixSize);
            _attractivenessMatrix = new List<double>(_matrixSize);
        }

        private void FillAttractivenessMatrix(List<double> distances)
        {
            for (int i = 0; i < _matrixSize; i++)
            {
                _attractivenessMatrix.Add(1 / distances[i]);
            }
        }


        private void FillTrialsMatrix()
        {
            for (int i = 0; i < _matrixSize; i++)
            {
                _trialsMatrix.Add(BasicTrialValue);
            }
        }


        //REPRESENTATION OF ANT.
        //RETURNS PATH CHOSEN BY THIS ANT
        private List<int> CalculatePathForSingleAnt()
        {
            List<int> path = new List<int>(_noOfCities);
            path.Add(_bestPath[0]);

            List<double> probabilityMatrix = new List<double>(_matrixSize);
            probabilityMatrix = FillProbabilityMatrix(probabilityMatrix);
            probabilityMatrix = ClearProbabilityRowsForFirstAndLastPoint(probabilityMatrix);

            //chosing next point until path is full
            for (int j = 1; j < _noOfCities - 1; j++)
            {
                var row = CopyRowFromProbabilityMatrix(j, path, probabilityMatrix);
                row = NormalizeProbabilityValues(row);
                row = SortRowByProbability(row);

                path.Add(DrawNewPointByProbability(row, path));
                probabilityMatrix = ClearProbabilityRowsForGivenPoint(path[j], probabilityMatrix);
            }

            path.Add(_bestPath[_noOfCities - 1]);
            return path;
        }


        private List<double> FillProbabilityMatrix(List<double> probabilityMatrix)
        {
            for (int i = 0; i < _matrixSize; i++)
            {
                probabilityMatrix.Add(Math.Pow(_trialsMatrix[i], TrialPreference) *
                                       Math.Pow(_attractivenessMatrix[i], AttractivenessParameter));
            }
            return probabilityMatrix;
        }


        private List<double> ClearProbabilityRowsForFirstAndLastPoint(List<double> probabilityMatrix)
        {
            for (int i = 0; i < _noOfCities; i++)
            {
                probabilityMatrix[_bestPath[0] + i * _noOfCities] = 0;
                probabilityMatrix[_bestPath[_noOfCities - 1] + i * _noOfCities] = 0;
            }
            return probabilityMatrix;
        }


        private VertexAndProbability[] CopyRowFromProbabilityMatrix(int q, List<int> path, List<double> probabilityMatrix)
        {
            VertexAndProbability[] result = new VertexAndProbability[_noOfCities];
            for (int i = 0; i < _noOfCities; i++)
            {
                result[i] = new VertexAndProbability
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


        private int DrawNewPointByProbability(VertexAndProbability[] row, List<int> path)
        {
            double randomLessThan1 = StaticRandom.RandomDouble();
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


        private List<double> ClearProbabilityRowsForGivenPoint(int nr, List<double> probabilityMatrix)
        {
            for (int i = 0; i < _noOfCities; i++)
            {
                probabilityMatrix[nr + i * _noOfCities] = 0;
            }
            return probabilityMatrix;
        }


        private void UpdateTrialsMatrix(List<int> path, List<double> distances)
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
