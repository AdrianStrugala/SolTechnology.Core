namespace TSPTimeCost {
    partial class TspTimeCostFrm {
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
            System.Windows.Forms.DataVisualization.Charting.ChartArea chartArea2 = new System.Windows.Forms.DataVisualization.Charting.ChartArea();
            System.Windows.Forms.DataVisualization.Charting.Legend legend2 = new System.Windows.Forms.DataVisualization.Charting.Legend();
            System.Windows.Forms.DataVisualization.Charting.Series series2 = new System.Windows.Forms.DataVisualization.Charting.Series();
            this.Area = new System.Windows.Forms.DataVisualization.Charting.Chart();
            this.CityOrder = new System.Windows.Forms.Label();
            this.TollTSPBtn = new System.Windows.Forms.Button();
            this.ResetBtn = new System.Windows.Forms.Button();
            this.ClassicTSPBtn = new System.Windows.Forms.Button();
            this.tollTSPChck = new System.Windows.Forms.CheckBox();
            this.classicTSPChck = new System.Windows.Forms.CheckBox();
            ((System.ComponentModel.ISupportInitialize)(this.Area)).BeginInit();
            this.SuspendLayout();
            // 
            // Area
            // 
            chartArea2.Name = "ChartArea1";
            this.Area.ChartAreas.Add(chartArea2);
            legend2.Name = "Legend1";
            this.Area.Legends.Add(legend2);
            this.Area.Location = new System.Drawing.Point(9, 10);
            this.Area.Margin = new System.Windows.Forms.Padding(2, 2, 2, 2);
            this.Area.Name = "Area";
            series2.ChartArea = "ChartArea1";
            series2.Legend = "Legend1";
            series2.Name = "Series1";
            this.Area.Series.Add(series2);
            this.Area.Size = new System.Drawing.Size(602, 541);
            this.Area.TabIndex = 0;
            this.Area.Text = "Area";
            // 
            // CityOrder
            // 
            this.CityOrder.AutoSize = true;
            this.CityOrder.Location = new System.Drawing.Point(9, 553);
            this.CityOrder.Margin = new System.Windows.Forms.Padding(2, 0, 2, 0);
            this.CityOrder.Name = "CityOrder";
            this.CityOrder.Size = new System.Drawing.Size(0, 13);
            this.CityOrder.TabIndex = 1;
            // 
            // TollTSPBtn
            // 
            this.TollTSPBtn.Location = new System.Drawing.Point(637, 27);
            this.TollTSPBtn.Margin = new System.Windows.Forms.Padding(2, 2, 2, 2);
            this.TollTSPBtn.Name = "TollTSPBtn";
            this.TollTSPBtn.Size = new System.Drawing.Size(105, 39);
            this.TollTSPBtn.TabIndex = 2;
            this.TollTSPBtn.Text = "Toll TSP";
            this.TollTSPBtn.UseVisualStyleBackColor = true;
            this.TollTSPBtn.Click += new System.EventHandler(this.TollTSPBtn_Click);
            // 
            // ResetBtn
            // 
            this.ResetBtn.Location = new System.Drawing.Point(686, 532);
            this.ResetBtn.Margin = new System.Windows.Forms.Padding(2, 2, 2, 2);
            this.ResetBtn.Name = "ResetBtn";
            this.ResetBtn.Size = new System.Drawing.Size(56, 19);
            this.ResetBtn.TabIndex = 3;
            this.ResetBtn.Text = "Reset";
            this.ResetBtn.UseVisualStyleBackColor = true;
            this.ResetBtn.Click += new System.EventHandler(this.ResetBtn_Click);
            // 
            // ClassicTSPBtn
            // 
            this.ClassicTSPBtn.Location = new System.Drawing.Point(637, 80);
            this.ClassicTSPBtn.Margin = new System.Windows.Forms.Padding(2, 2, 2, 2);
            this.ClassicTSPBtn.Name = "ClassicTSPBtn";
            this.ClassicTSPBtn.Size = new System.Drawing.Size(105, 39);
            this.ClassicTSPBtn.TabIndex = 4;
            this.ClassicTSPBtn.Text = "Classic TSP";
            this.ClassicTSPBtn.UseVisualStyleBackColor = true;
            this.ClassicTSPBtn.Click += new System.EventHandler(this.ClassicTSPBtn_Click);
            // 
            // tollTSPChck
            // 
            this.tollTSPChck.AutoSize = true;
            this.tollTSPChck.Checked = true;
            this.tollTSPChck.CheckState = System.Windows.Forms.CheckState.Checked;
            this.tollTSPChck.Location = new System.Drawing.Point(756, 40);
            this.tollTSPChck.Name = "tollTSPChck";
            this.tollTSPChck.Size = new System.Drawing.Size(15, 14);
            this.tollTSPChck.TabIndex = 5;
            this.tollTSPChck.UseVisualStyleBackColor = true;
            this.tollTSPChck.CheckedChanged += new System.EventHandler(this.TollTSPChck_CheckedChanged);
            // 
            // classicTSPChck
            // 
            this.classicTSPChck.AutoSize = true;
            this.classicTSPChck.Checked = true;
            this.classicTSPChck.CheckState = System.Windows.Forms.CheckState.Checked;
            this.classicTSPChck.Location = new System.Drawing.Point(756, 93);
            this.classicTSPChck.Name = "classicTSPChck";
            this.classicTSPChck.Size = new System.Drawing.Size(15, 14);
            this.classicTSPChck.TabIndex = 6;
            this.classicTSPChck.UseVisualStyleBackColor = true;
            this.classicTSPChck.CheckedChanged += new System.EventHandler(this.ClassicTSPChck_CheckedChanged);
            // 
            // TspTimeCostFrm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(792, 592);
            this.Controls.Add(this.classicTSPChck);
            this.Controls.Add(this.tollTSPChck);
            this.Controls.Add(this.ClassicTSPBtn);
            this.Controls.Add(this.ResetBtn);
            this.Controls.Add(this.TollTSPBtn);
            this.Controls.Add(this.CityOrder);
            this.Controls.Add(this.Area);
            this.Margin = new System.Windows.Forms.Padding(2, 2, 2, 2);
            this.Name = "TspTimeCostFrm";
            this.Text = "Traveling Eco-Salesman";
            this.Load += new System.EventHandler(this.Form1_Load);
            ((System.ComponentModel.ISupportInitialize)(this.Area)).EndInit();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.DataVisualization.Charting.Chart Area;
        private System.Windows.Forms.Label CityOrder;
        private System.Windows.Forms.Button TollTSPBtn;
        private System.Windows.Forms.Button ResetBtn;
        private System.Windows.Forms.Button ClassicTSPBtn;
        private System.Windows.Forms.CheckBox tollTSPChck;
        private System.Windows.Forms.CheckBox classicTSPChck;
    }
}

