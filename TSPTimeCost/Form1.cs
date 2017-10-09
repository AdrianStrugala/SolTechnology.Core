using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using System.Windows.Forms.DataVisualization.Charting;
using Parallel_Ants;
using TSPTimeCost.TSP;

namespace TSPTimeCost {
    public partial class ParallelAntsFrm : Form {
        public ParallelAntsFrm() {
            InitializeComponent();
        }

        public List<City> Cities = new List<City>();

        private void Form1_Load(object sender, EventArgs e) {

            InitializeSeries();

            ProcessInputData _processInputData = new ProcessInputData();

            Cities = _processInputData.GetCitiesFromGoogleApi();
            DrawCities();

            _processInputData.InitializeSingletons(Cities.Count);
            _processInputData.CalculateDistanceMatrix(Cities);
            BestPath.Instance.distance = new AntColony().CalculateDistanceInPath(BestPath.Instance.order);

        }



        public void InitializeSeries() {
            Area.Series[0].IsVisibleInLegend = false;
            Area.Series[0].ChartType = SeriesChartType.Point;
            Area.Series[0].MarkerStyle = MarkerStyle.Circle;
            Area.ChartAreas[0].AxisY.IsStartedFromZero = false;

            Area.Series.Add("ClassicTSP");
            Area.Series["ClassicTSP"].IsVisibleInLegend = true;
            Area.Series["ClassicTSP"].Color = Color.Red;
        }

        public void DrawCities() {
            foreach (var city in Cities) {
                Area.Series[0].Points.AddXY(city.Longitude, city.Latitude);
            }
        }

        public void ShowRoute() {

            Area.Series["ClassicTSP"].Points.Clear();
            WriteOrder();
            DrawRoute();
        }

        public void WriteOrder() {
            string _text = "Order: ";
            for (int i = 0; i < Cities.Count - 1; i++) {
                _text += Cities[BestPath.Instance.order[i]].Name + "->";
            }
            _text += Cities[BestPath.Instance.order[Cities.Count - 1]].Name;
            _text += "\nDistance: " + BestPath.Instance.distance;

            CityOrder.Text = _text;
        }

        public void DrawRoute() {


            for (int i = 0; i < Cities.Count - 1; i++) {

                Area.Series["ClassicTSP"].Points.AddXY(Cities[BestPath.Instance.order[i]].Longitude, Cities[BestPath.Instance.order[i]].Latitude);
                Area.Series["ClassicTSP"].Points.AddXY(Cities[BestPath.Instance.order[i + 1]].Longitude, Cities[BestPath.Instance.order[i + 1]].Latitude);
                Area.Series["ClassicTSP"].ChartType = SeriesChartType.Line;
            }

        }

        private void AntColonyBtn_Click(object sender, EventArgs e) {

            AntColony ants = new AntColony();
            ants.AntColonySingleThread();

            ShowRoute();
        }

        private void ResetBtn_Click(object sender, EventArgs e) {
            Area.Series["ClassicTSP"].Points.Clear();
            CityOrder.Text = "";
        }
    }
}
