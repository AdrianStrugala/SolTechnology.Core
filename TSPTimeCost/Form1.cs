using System;
using System.Drawing;
using System.Windows.Forms;
using System.Windows.Forms.DataVisualization.Charting;
using TSPTimeCost.Singletons;

namespace TSPTimeCost
{
    public partial class TspTimeCostFrm : Form
    {
        private static Controller _controller;
        private static ViewModel _viewModel;

        public TspTimeCostFrm()
        {
            _controller = new Controller();
            _viewModel = new ViewModel();
            InitializeComponent();

        }

        private void Form1_Load(object sender, EventArgs e)
        {
            _controller.Initialize();
            InitializeSeries();

            DrawCities();
        }


        private void InitializeSeries()
        {
            Area.Series[0].IsVisibleInLegend = false;
            Area.Series[0].ChartType = SeriesChartType.Point;
            Area.Series[0].MarkerStyle = MarkerStyle.Circle;
            Area.ChartAreas[0].AxisY.IsStartedFromZero = false;
            Area.Series[0].MarkerSize = 10;
            Area.Series[0].Color = Color.Magenta;

            InitializeSerie("TollTSP", Color.Blue);
            InitializeSerie("ClassicTSP", Color.Red);
            InitializeSerie("LimitTSP", Color.Black);
            InitializeSerie("EvaluationTSP", Color.Green);
            InitializeSerie("AttentionWhore", Color.Pink);
        }

        private void InitializeSerie(string name, Color color)
        {
            Area.Series.Add(name);
            Area.Series[name].IsVisibleInLegend = false;
            Area.Series[name].BorderWidth = 6;
            Area.Series[name].ChartType = SeriesChartType.Line;
            Area.Series[name].Color = color;
        }


        private void DrawCities()
        {
            foreach (var city in Cities.Instance.ListOfCities)
            {
                Area.Series[0].Points.AddXY(city.Longitude, city.Latitude);
            }
        }

        private void ShowRoute(string nameOfSeries)
        {
            Area.Series[nameOfSeries].Points.Clear();
            orderLbl.Text = _controller.GetOrder();
            goalLbl.Text = _controller.GetGoals();
            durationLbl.Text = _controller.GetDuration();
            DrawRoute(nameOfSeries);
        }


        private void DrawRoute(string nameOfSeries)
        {

            for (int i = 0; i < Cities.Instance.ListOfCities.Count - 1; i++)
            {

                if (_controller.IsTollFragmentInformation(i) == "(T)")
                {
                    Area.Series[nameOfSeries].Points.AddXY(Cities.Instance.ListOfCities[BestPath.Instance.Order[i]].Longitude,
                        Cities.Instance.ListOfCities[BestPath.Instance.Order[i]].Latitude);
                    Area.Series[nameOfSeries].Points.AddXY(Cities.Instance.ListOfCities[BestPath.Instance.Order[i + 1]].Longitude,
                        Cities.Instance.ListOfCities[BestPath.Instance.Order[i + 1]].Latitude);

                    Area.Series[nameOfSeries].Points[i * 2].BorderDashStyle = ChartDashStyle.Dot;
                    Area.Series[nameOfSeries].Points[i * 2 + 1].BorderDashStyle = ChartDashStyle.Dot;

                }

                else
                {
                    Area.Series[nameOfSeries].Points.AddXY(Cities.Instance.ListOfCities[BestPath.Instance.Order[i]].Longitude,
                        Cities.Instance.ListOfCities[BestPath.Instance.Order[i]].Latitude);
                    Area.Series[nameOfSeries].Points.AddXY(Cities.Instance.ListOfCities[BestPath.Instance.Order[i + 1]].Longitude,
                        Cities.Instance.ListOfCities[BestPath.Instance.Order[i + 1]].Latitude);

                    Area.Series[nameOfSeries].Points[i * 2].BorderDashStyle = ChartDashStyle.Solid;
                    Area.Series[nameOfSeries].Points[i * 2 + 1].BorderDashStyle = ChartDashStyle.Solid;
                }

            }
        }

        private void TollTSPBtn_Click(object sender, EventArgs e)
        {
            _controller.TollTSP();
            ShowRoute("TollTSP");

        }

        private void ResetBtn_Click(object sender, EventArgs e)
        {
            Area.Series["TollTSP"].Points.Clear();
            Area.Series["ClassicTSP"].Points.Clear();
            orderLbl.Text = "";
            goalLbl.Text = "";
            durationLbl.Text = "";
        }

        private void ClassicTSPBtn_Click(object sender, EventArgs e)
        {
            _controller.ClassicTSP();
            ShowRoute("ClassicTSP");
        }

        private void LimitTSPBtn_Click(object sender, EventArgs e)
        {
            _viewModel.Limit = Convert.ToDouble(limitTxt.Text);
            _controller.LimitTSP();
            ShowRoute("LimitTSP");
        }

        private void btnEvaluate_Click(object sender, EventArgs e)
        {
            _controller.EvaluationTSP();
            ShowRoute("EvaluationTSP");
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

        private void EvaluationTSPChck_CheckedChanged(object sender, EventArgs e)
        {
            Area.Series["EvaluationTSP"].Enabled = evaluationTSPChck.Checked;
        }

        private void limitTxt_TextChanged(object sender, EventArgs e)
        {

        }

        private void Area_Click(object sender, EventArgs e)
        {

        }

        private void AttentionWhoreBtn_Click(object sender, EventArgs e)
        {
            _controller.AttentionWhore();
            ShowRoute("AttentionWhore");
        }

        private void AttentionWhoreChck_CheckedChanged(object sender, EventArgs e)
        {
            Area.Series["AttentionWhore"].Enabled = AttentionWhoreChck.Checked;
        }
    }
}
