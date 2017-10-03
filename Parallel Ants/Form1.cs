using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using System.Windows.Forms.DataVisualization.Charting;

namespace TSPTimeCost {
    public partial class ParallelAntsFrm : Form {
        public ParallelAntsFrm() {
            InitializeComponent();
        }

        public List<Road> cities = new List<Road>();

        private void Form1_Load(object sender, EventArgs e) {

            InitializeSeries();

            ProcessInputData processInputData = new ProcessInputData();

            cities = processInputData.ReadInputFile();
            foreach (var city in cities) {
              //  processInputData.ConvertCoordinatesToDecimal(city);
            }

            DrawCities();

            processInputData.InitializeSingletons(cities.Count);
          //  processInputData.CalculateDistanceMatrix(cities);
            BestPath.Instance.distance = new AntColony().CalculateDistanceInPath(BestPath.Instance.order);

        }

        public void InitializeSeries()
        {
            Area.Series[0].IsVisibleInLegend = false;
            Area.Series[0].ChartType = SeriesChartType.Point;
            Area.Series[0].MarkerStyle = MarkerStyle.Circle;
            Area.ChartAreas[0].AxisY.IsStartedFromZero = false;

            Area.Series.Add("Line");
            Area.Series["Line"].IsVisibleInLegend = false;
            Area.Series["Line"].Color = Color.Red;
        }

        public void DrawCities() {
            foreach (var city in cities) {
         //       Area.Series[0].Points.AddXY(city.X, city.Y);
            }
        }

        public void ShowRoute() {

            Area.Series["Line"].Points.Clear();
            WriteOrder();
            DrawRoute();
        }

        public void WriteOrder() {
            string text = "Order: ";
            for (int i = 0; i < cities.Count - 1; i++) {
       //         text += cities[BestPath.Instance.order[i]].Name + "->";
            }
       //     text += cities[BestPath.Instance.order[cities.Count - 1]].Name;
            text += "\nDistance: " + BestPath.Instance.distance;

            CityOrder.Text = text;
        }

        public void DrawRoute() {


            for (int i = 0; i < cities.Count - 1; i++) {
                // Area.Series.Add("Line"+i);

         //       Area.Series["Line"].Points.AddXY(cities[BestPath.Instance.order[i]].X, cities[BestPath.Instance.order[i]].Y);
         //       Area.Series["Line"].Points.AddXY(cities[BestPath.Instance.order[i + 1]].X, cities[BestPath.Instance.order[i + 1]].Y);
                Area.Series["Line"].ChartType = SeriesChartType.Line;
            }

        }

        private void AntColonyBtn_Click(object sender, EventArgs e) {

            AntColony ants = new AntColony();
            ants.AntColonySingleThread();

            ShowRoute();
        }

        private void ResetBtn_Click(object sender, EventArgs e) {
            Area.Series["Line"].Points.Clear();
            CityOrder.Text = "";
        }
    }
}
