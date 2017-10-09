namespace TSPTimeCost {
    partial class ParallelAntsFrm {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing) {
            if (disposing && (components != null)) {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent() {
            System.Windows.Forms.DataVisualization.Charting.ChartArea chartArea1 = new System.Windows.Forms.DataVisualization.Charting.ChartArea();
            System.Windows.Forms.DataVisualization.Charting.Legend legend1 = new System.Windows.Forms.DataVisualization.Charting.Legend();
            System.Windows.Forms.DataVisualization.Charting.Series series1 = new System.Windows.Forms.DataVisualization.Charting.Series();
            this.Area = new System.Windows.Forms.DataVisualization.Charting.Chart();
            this.CityOrder = new System.Windows.Forms.Label();
            this.ClassicTSPBtn = new System.Windows.Forms.Button();
            this.ResetBtn = new System.Windows.Forms.Button();
            ((System.ComponentModel.ISupportInitialize)(this.Area)).BeginInit();
            this.SuspendLayout();
            // 
            // Area
            // 
            chartArea1.Name = "ChartArea1";
            this.Area.ChartAreas.Add(chartArea1);
            legend1.Name = "Legend1";
            this.Area.Legends.Add(legend1);
            this.Area.Location = new System.Drawing.Point(12, 12);
            this.Area.Name = "Area";
            series1.ChartArea = "ChartArea1";
            series1.Legend = "Legend1";
            series1.Name = "Series1";
            this.Area.Series.Add(series1);
            this.Area.Size = new System.Drawing.Size(802, 666);
            this.Area.TabIndex = 0;
            this.Area.Text = "Area";
            // 
            // CityOrder
            // 
            this.CityOrder.AutoSize = true;
            this.CityOrder.Location = new System.Drawing.Point(12, 681);
            this.CityOrder.Name = "CityOrder";
            this.CityOrder.Size = new System.Drawing.Size(0, 17);
            this.CityOrder.TabIndex = 1;
            // 
            // ClassicTSPBtn
            // 
            this.ClassicTSPBtn.Location = new System.Drawing.Point(849, 33);
            this.ClassicTSPBtn.Name = "ClassicTSPBtn";
            this.ClassicTSPBtn.Size = new System.Drawing.Size(140, 48);
            this.ClassicTSPBtn.TabIndex = 2;
            this.ClassicTSPBtn.Text = "Classic TSP";
            this.ClassicTSPBtn.UseVisualStyleBackColor = true;
            this.ClassicTSPBtn.Click += new System.EventHandler(this.AntColonyBtn_Click);
            // 
            // ResetBtn
            // 
            this.ResetBtn.Location = new System.Drawing.Point(914, 655);
            this.ResetBtn.Name = "ResetBtn";
            this.ResetBtn.Size = new System.Drawing.Size(75, 23);
            this.ResetBtn.TabIndex = 3;
            this.ResetBtn.Text = "Reset";
            this.ResetBtn.UseVisualStyleBackColor = true;
            this.ResetBtn.Click += new System.EventHandler(this.ResetBtn_Click);
            // 
            // ParallelAntsFrm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(8F, 16F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(1010, 729);
            this.Controls.Add(this.ResetBtn);
            this.Controls.Add(this.ClassicTSPBtn);
            this.Controls.Add(this.CityOrder);
            this.Controls.Add(this.Area);
            this.Name = "ParallelAntsFrm";
            this.Text = "Parallel Ants";
            this.Load += new System.EventHandler(this.Form1_Load);
            ((System.ComponentModel.ISupportInitialize)(this.Area)).EndInit();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.DataVisualization.Charting.Chart Area;
        private System.Windows.Forms.Label CityOrder;
        private System.Windows.Forms.Button ClassicTSPBtn;
        private System.Windows.Forms.Button ResetBtn;
    }
}

