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

        private List<City> _cities = new List<City>();

        private void Form1_Load(object sender, EventArgs e)
        {

            InitializeSeries();

            ProcessInputData processInputData = new ProcessInputData();

            _cities = processInputData.GetCitiesFromGoogleApi();
            DrawCities();

            ProcessInputData.InitializeSingletons(_cities.Count);
            ProcessInputData.CalculateDistanceMatrixForTollRoads(_cities);
            processInputData.CalculateDistanceMatrixForFreeRoads(_cities);
            processInputData.CalculateCostMatrix(_cities);


            BestPath.Instance.Distance = new AntColonyToll().CalculateDistanceInPath(BestPath.Instance.Order);

        }


        private void InitializeSeries()
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

            Area.Series.Add("LimitTSP");
            Area.Series["LimitTSP"].IsVisibleInLegend = false;
            Area.Series["LimitTSP"].BorderWidth = 6;
            Area.Series["LimitTSP"].ChartType = SeriesChartType.Line;
            Area.Series["LimitTSP"].Color = Color.Black;
        }


        private void DrawCities()
        {
            foreach (var city in _cities)
            {
                Area.Series[0].Points.AddXY(city.Longitude, city.Latitude);
            }
        }

        private void ShowRoute(string nameOfSeries)
        {

            Area.Series[nameOfSeries].Points.Clear();
            WriteOrder();
            DrawRoute(nameOfSeries);
        }

        private void WriteOrder()
        {
            string text = "Order: ";
            for (int i = 0; i < _cities.Count - 1; i++)
            {
                text += _cities[BestPath.Instance.Order[i]].Name + "-" + IsTollFragment(i) + ">";
            }
            text += _cities[BestPath.Instance.Order[_cities.Count - 1]].Name;
            text += "\nDuration: " + BestPath.Instance.Distance + "    Cost: " + BestPath.Instance.Cost;

            CityOrder.Text = text;
        }

        private void DrawRoute(string nameOfSeries)
        {

            for (int i = 0; i < _cities.Count - 1; i++)
            {

                if (IsTollFragment(i) == "(T)")
                {
                    Area.Series[nameOfSeries].Points.AddXY(_cities[BestPath.Instance.Order[i]].Longitude,
                        _cities[BestPath.Instance.Order[i]].Latitude);
                    Area.Series[nameOfSeries].Points.AddXY(_cities[BestPath.Instance.Order[i + 1]].Longitude,
                        _cities[BestPath.Instance.Order[i + 1]].Latitude);

                    Area.Series[nameOfSeries].Points[i * 2].BorderDashStyle = ChartDashStyle.Dot;
                    Area.Series[nameOfSeries].Points[i * 2 + 1].BorderDashStyle = ChartDashStyle.Dot;

                }

                else
                {
                    Area.Series[nameOfSeries].Points.AddXY(_cities[BestPath.Instance.Order[i]].Longitude,
                        _cities[BestPath.Instance.Order[i]].Latitude);
                    Area.Series[nameOfSeries].Points.AddXY(_cities[BestPath.Instance.Order[i + 1]].Longitude,
                        _cities[BestPath.Instance.Order[i + 1]].Latitude);

                    Area.Series[nameOfSeries].Points[i * 2].BorderDashStyle = ChartDashStyle.Solid;
                    Area.Series[nameOfSeries].Points[i * 2 + 1].BorderDashStyle = ChartDashStyle.Solid;
                }

            }
        }

        private string IsTollFragment(int indexInBestPath)
        {
            City origin = _cities[BestPath.Instance.Order[indexInBestPath]];
            City destination = _cities[BestPath.Instance.Order[indexInBestPath + 1]];
            var indexOrigin = _cities.IndexOf(origin);
            var indexDestination = _cities.IndexOf(destination);


            if (Equals(
                BestPath.Instance.DistancesInOrder[indexInBestPath],
                DistanceMatrixForTollRoads.Instance.Value[indexOrigin + _cities.Count * indexDestination])
                && !Equals(
                    DistanceMatrixForTollRoads.Instance.Value[indexOrigin + _cities.Count * indexDestination],
                    DistanceMatrixForFreeRoads.Instance.Value[indexOrigin + _cities.Count * indexDestination])
            )
            {
                return "(T)";
            }
            return "(F)";
        }

        private void TollTSPBtn_Click(object sender, EventArgs e)
        {

            AntColonyToll ants = new AntColonyToll();
            ants.AntColonySingleThread(_cities);

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

        private void LimitTSPBtn_Click(object sender, EventArgs e)
        {

            double limit = Convert.ToDouble(limitTxt.Text);

            AntColonyWithLimit ants = new AntColonyWithLimit(limit);
            ants.AntColonySingleThread(_cities);

            ShowRoute("LimitTSP");
        }

        private void TollTSPChck_CheckedChanged(object sender, EventArgs e)
        {
            Area.Series["TollTSP"].Enabled = tollTSPChck.Checked;
        }

        private void ClassicTSPChck_CheckedChanged(object sender, EventArgs e)
        {
            Area.Series["ClassicTSP"].Enabled = classicTSPChck.Checked;
        }

        private void LimitTSPChck_CheckedChanged(object sender, EventArgs e)
        {
            Area.Series["LimitTSP"].Enabled = limitTSPChck.Checked;
        }

        private void limitTxt_TextChanged(object sender, EventArgs e)
        {

        }
    }
}
