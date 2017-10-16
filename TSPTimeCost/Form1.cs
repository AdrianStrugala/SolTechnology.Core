using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using System.Windows.Forms.DataVisualization.Charting;
using TSPTimeCost.Models;
using TSPTimeCost.TSP;

namespace TSPTimeCost
{
    public partial class TspTimeCostFrm : Form
    {
        public TspTimeCostFrm()
        {
            InitializeComponent();
        }

        public List<City> Cities = new List<City>();

        private void Form1_Load(object sender, EventArgs e)
        {

            InitializeSeries();

            ProcessInputData _processInputData = new ProcessInputData();

            Cities = _processInputData.GetCitiesFromGoogleApi();
            DrawCities();

            _processInputData.InitializeSingletons(Cities.Count);
            _processInputData.CalculateDistanceMatrixForTollRoads(Cities);
            _processInputData.CalculateDistanceMatrixForFreeRoads(Cities);

            BestPath.Instance.distance = new AntColonyToll().CalculateDistanceInPath(BestPath.Instance.order);

        }



        public void InitializeSeries()
        {
            Area.Series[0].IsVisibleInLegend = false;
            Area.Series[0].ChartType = SeriesChartType.Point;
            Area.Series[0].MarkerStyle = MarkerStyle.Circle;
            Area.ChartAreas[0].AxisY.IsStartedFromZero = false;
            Area.Series[0].MarkerSize = 10;
            Area.Series[0].Color = Color.Magenta;

            Area.Series.Add("TollTSP");
            Area.Series["TollTSP"].IsVisibleInLegend = false;
            Area.Series["TollTSP"].BorderWidth = 6;
            // Area.Series["TollTSP"].BorderDashStyle = ChartDashStyle.Solid;
            Area.Series["TollTSP"].ChartType = SeriesChartType.Line;
            Area.Series["TollTSP"].Color = Color.Blue;


            Area.Series.Add("ClassicTSP");
            Area.Series["ClassicTSP"].IsVisibleInLegend = false;
            Area.Series["ClassicTSP"].BorderWidth = 6;
            //  Area.Series["ClassicTSP"].BorderDashStyle = ChartDashStyle.Solid;
            Area.Series["ClassicTSP"].ChartType = SeriesChartType.Line;
            Area.Series["ClassicTSP"].Color = Color.Red;
        }


        public void DrawCities()
        {
            foreach (var _city in Cities)
            {
                Area.Series[0].Points.AddXY(_city.Longitude, _city.Latitude);
            }
        }

        public void ShowRoute(string nameOfSeries)
        {

            Area.Series[nameOfSeries].Points.Clear();
            WriteOrder();
            DrawRoute(nameOfSeries);
        }

        public void WriteOrder()
        {
            string _text = "Order: ";
            for (int i = 0; i < Cities.Count - 1; i++)
            {
                _text += Cities[BestPath.Instance.order[i]].Name + "-" + IsTollFragment(i) + ">";
            }
            _text += Cities[BestPath.Instance.order[Cities.Count - 1]].Name;
            _text += "\nDuration: " + BestPath.Instance.distance;

            CityOrder.Text = _text;
        }

        public void DrawRoute(string nameOfSeries)
        {

            for (int i = 0; i < Cities.Count - 1; i++)
            {

                if (IsTollFragment(i) == "(T)")
                {
                    Area.Series[nameOfSeries].Points.AddXY(Cities[BestPath.Instance.order[i]].Longitude,
                        Cities[BestPath.Instance.order[i]].Latitude);
                    Area.Series[nameOfSeries].Points.AddXY(Cities[BestPath.Instance.order[i + 1]].Longitude,
                        Cities[BestPath.Instance.order[i + 1]].Latitude);

                    Area.Series[nameOfSeries].Points[i * 2].BorderDashStyle = ChartDashStyle.Dot;
                    Area.Series[nameOfSeries].Points[i * 2 + 1].BorderDashStyle = ChartDashStyle.Dot;

                }

                else
                {
                    Area.Series[nameOfSeries].Points.AddXY(Cities[BestPath.Instance.order[i]].Longitude,
                        Cities[BestPath.Instance.order[i]].Latitude);
                    Area.Series[nameOfSeries].Points.AddXY(Cities[BestPath.Instance.order[i + 1]].Longitude,
                        Cities[BestPath.Instance.order[i + 1]].Latitude);

                    Area.Series[nameOfSeries].Points[i * 2].BorderDashStyle = ChartDashStyle.Solid;
                    Area.Series[nameOfSeries].Points[i * 2 + 1].BorderDashStyle = ChartDashStyle.Solid;
                }

            }
        }

        private string IsTollFragment(int indexInBestPath)
        {
            City _origin = Cities[BestPath.Instance.order[indexInBestPath]];
            City _destination = Cities[BestPath.Instance.order[indexInBestPath + 1]];
            var _indexOrigin = Cities.IndexOf(_origin);
            var _indexDestination = Cities.IndexOf(_destination);


            if (Equals(
                BestPath.Instance.disnancesInOrder[indexInBestPath],
                DistanceMatrixForTollRoads.Instance.Value[_indexOrigin + Cities.Count * _indexDestination])
                && !Equals(
                    DistanceMatrixForTollRoads.Instance.Value[_indexOrigin + Cities.Count * _indexDestination],
                    DistanceMatrixForFreeRoads.Instance.Value[_indexOrigin + Cities.Count * _indexDestination])
            )
            {
                return "(T)";
            }
            return "(F)";
        }

        private void TollTSPBtn_Click(object sender, EventArgs e)
        {

            AntColonyToll ants = new AntColonyToll();
            ants.AntColonySingleThread();

            ShowRoute("TollTSP");
        }

        private void ResetBtn_Click(object sender, EventArgs e)
        {
            Area.Series["TollTSP"].Points.Clear();
            Area.Series["ClassicTSP"].Points.Clear();
            CityOrder.Text = "";
        }

        private void ClassicTSPBtn_Click(object sender, EventArgs e)
        {

            AntColonyClassic ants = new AntColonyClassic();
            ants.AntColonySingleThread();

            ShowRoute("ClassicTSP");
        }

        private void tollTSPChck_CheckedChanged(object sender, EventArgs e)
        {
            Area.Series["TollTSP"].Enabled = tollTSPChck.Checked;
        }

        private void classicTSPChck_CheckedChanged(object sender, EventArgs e)
        {
            Area.Series["ClassicTSP"].Enabled = classicTSPChck.Checked;
        }
    }
}
