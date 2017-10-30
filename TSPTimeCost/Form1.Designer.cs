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
            System.Windows.Forms.DataVisualization.Charting.ChartArea chartArea5 = new System.Windows.Forms.DataVisualization.Charting.ChartArea();
            System.Windows.Forms.DataVisualization.Charting.Legend legend5 = new System.Windows.Forms.DataVisualization.Charting.Legend();
            System.Windows.Forms.DataVisualization.Charting.Series series5 = new System.Windows.Forms.DataVisualization.Charting.Series();
            this.Area = new System.Windows.Forms.DataVisualization.Charting.Chart();
            this.CityOrder = new System.Windows.Forms.Label();
            this.TollTSPBtn = new System.Windows.Forms.Button();
            this.ResetBtn = new System.Windows.Forms.Button();
            this.ClassicTSPBtn = new System.Windows.Forms.Button();
            this.tollTSPChck = new System.Windows.Forms.CheckBox();
            this.classicTSPChck = new System.Windows.Forms.CheckBox();
            this.limitTSPChck = new System.Windows.Forms.CheckBox();
            this.LimitTSPBtn = new System.Windows.Forms.Button();
            this.limitLbl = new System.Windows.Forms.Label();
            this.limitTxt = new System.Windows.Forms.TextBox();
            ((System.ComponentModel.ISupportInitialize)(this.Area)).BeginInit();
            this.SuspendLayout();
            // 
            // Area
            // 
            chartArea5.Name = "ChartArea1";
            this.Area.ChartAreas.Add(chartArea5);
            legend5.Name = "Legend1";
            this.Area.Legends.Add(legend5);
            this.Area.Location = new System.Drawing.Point(12, 12);
            this.Area.Margin = new System.Windows.Forms.Padding(3, 2, 3, 2);
            this.Area.Name = "Area";
            series5.ChartArea = "ChartArea1";
            series5.Legend = "Legend1";
            series5.Name = "Series1";
            this.Area.Series.Add(series5);
            this.Area.Size = new System.Drawing.Size(803, 666);
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
            // TollTSPBtn
            // 
            this.TollTSPBtn.Location = new System.Drawing.Point(850, 87);
            this.TollTSPBtn.Margin = new System.Windows.Forms.Padding(3, 2, 3, 2);
            this.TollTSPBtn.Name = "TollTSPBtn";
            this.TollTSPBtn.Size = new System.Drawing.Size(140, 48);
            this.TollTSPBtn.TabIndex = 2;
            this.TollTSPBtn.Text = "Toll TSP";
            this.TollTSPBtn.UseVisualStyleBackColor = true;
            this.TollTSPBtn.Click += new System.EventHandler(this.TollTSPBtn_Click);
            // 
            // ResetBtn
            // 
            this.ResetBtn.Location = new System.Drawing.Point(915, 655);
            this.ResetBtn.Margin = new System.Windows.Forms.Padding(3, 2, 3, 2);
            this.ResetBtn.Name = "ResetBtn";
            this.ResetBtn.Size = new System.Drawing.Size(75, 23);
            this.ResetBtn.TabIndex = 3;
            this.ResetBtn.Text = "Reset";
            this.ResetBtn.UseVisualStyleBackColor = true;
            this.ResetBtn.Click += new System.EventHandler(this.ResetBtn_Click);
            // 
            // ClassicTSPBtn
            // 
            this.ClassicTSPBtn.Location = new System.Drawing.Point(850, 12);
            this.ClassicTSPBtn.Margin = new System.Windows.Forms.Padding(3, 2, 3, 2);
            this.ClassicTSPBtn.Name = "ClassicTSPBtn";
            this.ClassicTSPBtn.Size = new System.Drawing.Size(140, 48);
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
            this.tollTSPChck.Location = new System.Drawing.Point(1009, 103);
            this.tollTSPChck.Margin = new System.Windows.Forms.Padding(4);
            this.tollTSPChck.Name = "tollTSPChck";
            this.tollTSPChck.Size = new System.Drawing.Size(18, 17);
            this.tollTSPChck.TabIndex = 5;
            this.tollTSPChck.UseVisualStyleBackColor = true;
            this.tollTSPChck.CheckedChanged += new System.EventHandler(this.TollTSPChck_CheckedChanged);
            // 
            // classicTSPChck
            // 
            this.classicTSPChck.AutoSize = true;
            this.classicTSPChck.Checked = true;
            this.classicTSPChck.CheckState = System.Windows.Forms.CheckState.Checked;
            this.classicTSPChck.Location = new System.Drawing.Point(1009, 28);
            this.classicTSPChck.Margin = new System.Windows.Forms.Padding(4);
            this.classicTSPChck.Name = "classicTSPChck";
            this.classicTSPChck.Size = new System.Drawing.Size(18, 17);
            this.classicTSPChck.TabIndex = 6;
            this.classicTSPChck.UseVisualStyleBackColor = true;
            this.classicTSPChck.CheckedChanged += new System.EventHandler(this.ClassicTSPChck_CheckedChanged);
            // 
            // limitTSPChck
            // 
            this.limitTSPChck.AutoSize = true;
            this.limitTSPChck.Checked = true;
            this.limitTSPChck.CheckState = System.Windows.Forms.CheckState.Checked;
            this.limitTSPChck.Location = new System.Drawing.Point(1009, 177);
            this.limitTSPChck.Margin = new System.Windows.Forms.Padding(4);
            this.limitTSPChck.Name = "limitTSPChck";
            this.limitTSPChck.Size = new System.Drawing.Size(18, 17);
            this.limitTSPChck.TabIndex = 8;
            this.limitTSPChck.UseVisualStyleBackColor = true;
            this.limitTSPChck.CheckedChanged += new System.EventHandler(this.LimitTSPChck_CheckedChanged);
            // 
            // LimitTSPBtn
            // 
            this.LimitTSPBtn.Location = new System.Drawing.Point(850, 160);
            this.LimitTSPBtn.Margin = new System.Windows.Forms.Padding(3, 2, 3, 2);
            this.LimitTSPBtn.Name = "LimitTSPBtn";
            this.LimitTSPBtn.Size = new System.Drawing.Size(140, 48);
            this.LimitTSPBtn.TabIndex = 7;
            this.LimitTSPBtn.Text = "Limit TSP";
            this.LimitTSPBtn.UseVisualStyleBackColor = true;
            this.LimitTSPBtn.Click += new System.EventHandler(this.LimitTSPBtn_Click);
            // 
            // limitLbl
            // 
            this.limitLbl.AutoSize = true;
            this.limitLbl.Location = new System.Drawing.Point(861, 233);
            this.limitLbl.Name = "limitLbl";
            this.limitLbl.Size = new System.Drawing.Size(41, 17);
            this.limitLbl.TabIndex = 9;
            this.limitLbl.Text = "Limit:";
            // 
            // limitTxt
            // 
            this.limitTxt.Location = new System.Drawing.Point(909, 230);
            this.limitTxt.Name = "limitTxt";
            this.limitTxt.Size = new System.Drawing.Size(81, 22);
            this.limitTxt.TabIndex = 10;
            this.limitTxt.Text = "100.0";
            this.limitTxt.TextChanged += new System.EventHandler(this.limitTxt_TextChanged);
            // 
            // TspTimeCostFrm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(8F, 16F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(1056, 729);
            this.Controls.Add(this.limitTxt);
            this.Controls.Add(this.limitLbl);
            this.Controls.Add(this.limitTSPChck);
            this.Controls.Add(this.LimitTSPBtn);
            this.Controls.Add(this.classicTSPChck);
            this.Controls.Add(this.tollTSPChck);
            this.Controls.Add(this.ClassicTSPBtn);
            this.Controls.Add(this.ResetBtn);
            this.Controls.Add(this.TollTSPBtn);
            this.Controls.Add(this.CityOrder);
            this.Controls.Add(this.Area);
            this.Margin = new System.Windows.Forms.Padding(3, 2, 3, 2);
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
        private System.Windows.Forms.CheckBox limitTSPChck;
        private System.Windows.Forms.Button LimitTSPBtn;
        private System.Windows.Forms.Label limitLbl;
        private System.Windows.Forms.TextBox limitTxt;
    }
}

