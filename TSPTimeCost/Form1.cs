using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using System.Windows.Forms.DataVisualization.Charting;
using Parallel_Ants;
using TSPTimeCost.TSP;

namespace TSPTimeCost {
    public partial class TspTimeCostFrm : Form {
        public TspTimeCostFrm() {
            InitializeComponent();
        }

        public List<City> Cities = new List<City>();

        private void Form1_Load(object sender, EventArgs e) {

            InitializeSeries();

            ProcessInputData _processInputData = new ProcessInputData();

            Cities = _processInputData.GetCitiesFromGoogleApi();
            DrawCities();

            _processInputData.InitializeSingletons(Cities.Count);
            _processInputData.CalculateDistanceMatrixForTollRoads(Cities);
            _processInputData.CalculateDistanceMatrixForFreeRoads(Cities);

            BestPath.Instance.distance = new AntColonyToll().CalculateDistanceInPath(BestPath.Instance.order);

        }



        public void InitializeSeries() {
            Area.Series[0].IsVisibleInLegend = false;
            Area.Series[0].ChartType = SeriesChartType.Point;
            Area.Series[0].MarkerStyle = MarkerStyle.Circle;
            Area.ChartAreas[0].AxisY.IsStartedFromZero = false;

            Area.Series.Add("TollTSP");
            Area.Series["TollTSP"].IsVisibleInLegend = true;
            Area.Series["TollTSP"].Color = Color.Blue;

            Area.Series.Add("ClassicTSP");
            Area.Series["ClassicTSP"].IsVisibleInLegend = true;
            Area.Series["ClassicTSP"].Color = Color.Red;
        }


        public void DrawCities() {
            foreach (var city in Cities) {
                Area.Series[0].Points.AddXY(city.Longitude, city.Latitude);
            }
        }

        public void ShowRoute(string nameOfSeries) {

            Area.Series[nameOfSeries].Points.Clear();
            WriteOrder();
            DrawRoute(nameOfSeries);
        }

        public void WriteOrder() {
            string _text = "Order: ";
            for (int i = 0; i < Cities.Count - 1; i++) {
                _text += Cities[BestPath.Instance.order[i]].Name + "->";
            }
            _text += Cities[BestPath.Instance.order[Cities.Count - 1]].Name;
            _text += "\nDuration: " + BestPath.Instance.distance;

            CityOrder.Text = _text;
        }

        public void DrawRoute(string nameOfSeries) {


            for (int i = 0; i < Cities.Count - 1; i++) {

                Area.Series[nameOfSeries].Points.AddXY(Cities[BestPath.Instance.order[i]].Longitude, Cities[BestPath.Instance.order[i]].Latitude);
                Area.Series[nameOfSeries].Points.AddXY(Cities[BestPath.Instance.order[i + 1]].Longitude, Cities[BestPath.Instance.order[i + 1]].Latitude);
                Area.Series[nameOfSeries].ChartType = SeriesChartType.Line;
            }

        }

        private void TollTSPBtn_Click(object sender, EventArgs e) {

            AntColonyToll ants = new AntColonyToll();
            ants.AntColonySingleThread();

            ShowRoute("TollTSP");
        }

        private void ResetBtn_Click(object sender, EventArgs e) {
            Area.Series["TollTSP"].Points.Clear();
            Area.Series["ClassicTSP"].Points.Clear();
            CityOrder.Text = "";
        }

        private void ClassicTSPBtn_Click(object sender, EventArgs e) {

            AntColonyClassic ants = new AntColonyClassic();
            ants.AntColonySingleThread();

            ShowRoute("ClassicTSP");
        }
    }
}
