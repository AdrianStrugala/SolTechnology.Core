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
            System.Windows.Forms.DataVisualization.Charting.ChartArea chartArea1 = new System.Windows.Forms.DataVisualization.Charting.ChartArea();
            System.Windows.Forms.DataVisualization.Charting.Legend legend1 = new System.Windows.Forms.DataVisualization.Charting.Legend();
            System.Windows.Forms.DataVisualization.Charting.Series series1 = new System.Windows.Forms.DataVisualization.Charting.Series();
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
            this.btnEvaluate = new System.Windows.Forms.Button();
            this.evaluationTSPChck = new System.Windows.Forms.CheckBox();
            ((System.ComponentModel.ISupportInitialize)(this.Area)).BeginInit();
            this.SuspendLayout();
            // 
            // Area
            // 
            chartArea1.Name = "ChartArea1";
            this.Area.ChartAreas.Add(chartArea1);
            legend1.Name = "Legend1";
            this.Area.Legends.Add(legend1);
            this.Area.Location = new System.Drawing.Point(9, 10);
            this.Area.Margin = new System.Windows.Forms.Padding(2);
            this.Area.Name = "Area";
            series1.ChartArea = "ChartArea1";
            series1.Legend = "Legend1";
            series1.Name = "Series1";
            this.Area.Series.Add(series1);
            this.Area.Size = new System.Drawing.Size(602, 541);
            this.Area.TabIndex = 0;
            this.Area.Text = "Area";
            this.Area.Click += new System.EventHandler(this.Area_Click);
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
            this.TollTSPBtn.Location = new System.Drawing.Point(638, 71);
            this.TollTSPBtn.Margin = new System.Windows.Forms.Padding(2);
            this.TollTSPBtn.Name = "TollTSPBtn";
            this.TollTSPBtn.Size = new System.Drawing.Size(105, 39);
            this.TollTSPBtn.TabIndex = 2;
            this.TollTSPBtn.Text = "Toll";
            this.TollTSPBtn.UseVisualStyleBackColor = true;
            this.TollTSPBtn.Click += new System.EventHandler(this.TollTSPBtn_Click);
            // 
            // ResetBtn
            // 
            this.ResetBtn.Location = new System.Drawing.Point(686, 532);
            this.ResetBtn.Margin = new System.Windows.Forms.Padding(2);
            this.ResetBtn.Name = "ResetBtn";
            this.ResetBtn.Size = new System.Drawing.Size(56, 19);
            this.ResetBtn.TabIndex = 3;
            this.ResetBtn.Text = "Reset";
            this.ResetBtn.UseVisualStyleBackColor = true;
            this.ResetBtn.Click += new System.EventHandler(this.ResetBtn_Click);
            // 
            // ClassicTSPBtn
            // 
            this.ClassicTSPBtn.Location = new System.Drawing.Point(638, 10);
            this.ClassicTSPBtn.Margin = new System.Windows.Forms.Padding(2);
            this.ClassicTSPBtn.Name = "ClassicTSPBtn";
            this.ClassicTSPBtn.Size = new System.Drawing.Size(105, 39);
            this.ClassicTSPBtn.TabIndex = 4;
            this.ClassicTSPBtn.Text = "Classic";
            this.ClassicTSPBtn.UseVisualStyleBackColor = true;
            this.ClassicTSPBtn.Click += new System.EventHandler(this.ClassicTSPBtn_Click);
            // 
            // tollTSPChck
            // 
            this.tollTSPChck.AutoSize = true;
            this.tollTSPChck.Checked = true;
            this.tollTSPChck.CheckState = System.Windows.Forms.CheckState.Checked;
            this.tollTSPChck.Location = new System.Drawing.Point(757, 84);
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
            this.classicTSPChck.Location = new System.Drawing.Point(757, 23);
            this.classicTSPChck.Name = "classicTSPChck";
            this.classicTSPChck.Size = new System.Drawing.Size(15, 14);
            this.classicTSPChck.TabIndex = 6;
            this.classicTSPChck.UseVisualStyleBackColor = true;
            this.classicTSPChck.CheckedChanged += new System.EventHandler(this.ClassicTSPChck_CheckedChanged);
            // 
            // limitTSPChck
            // 
            this.limitTSPChck.AutoSize = true;
            this.limitTSPChck.Checked = true;
            this.limitTSPChck.CheckState = System.Windows.Forms.CheckState.Checked;
            this.limitTSPChck.Location = new System.Drawing.Point(757, 144);
            this.limitTSPChck.Name = "limitTSPChck";
            this.limitTSPChck.Size = new System.Drawing.Size(15, 14);
            this.limitTSPChck.TabIndex = 8;
            this.limitTSPChck.UseVisualStyleBackColor = true;
            this.limitTSPChck.CheckedChanged += new System.EventHandler(this.LimitTSPChck_CheckedChanged);
            // 
            // LimitTSPBtn
            // 
            this.LimitTSPBtn.Location = new System.Drawing.Point(638, 130);
            this.LimitTSPBtn.Margin = new System.Windows.Forms.Padding(2);
            this.LimitTSPBtn.Name = "LimitTSPBtn";
            this.LimitTSPBtn.Size = new System.Drawing.Size(105, 39);
            this.LimitTSPBtn.TabIndex = 7;
            this.LimitTSPBtn.Text = "Limit";
            this.LimitTSPBtn.UseVisualStyleBackColor = true;
            this.LimitTSPBtn.Click += new System.EventHandler(this.LimitTSPBtn_Click);
            // 
            // limitLbl
            // 
            this.limitLbl.AutoSize = true;
            this.limitLbl.Location = new System.Drawing.Point(646, 189);
            this.limitLbl.Margin = new System.Windows.Forms.Padding(2, 0, 2, 0);
            this.limitLbl.Name = "limitLbl";
            this.limitLbl.Size = new System.Drawing.Size(31, 13);
            this.limitLbl.TabIndex = 9;
            this.limitLbl.Text = "Limit:";
            // 
            // limitTxt
            // 
            this.limitTxt.Location = new System.Drawing.Point(682, 187);
            this.limitTxt.Margin = new System.Windows.Forms.Padding(2);
            this.limitTxt.Name = "limitTxt";
            this.limitTxt.Size = new System.Drawing.Size(62, 20);
            this.limitTxt.TabIndex = 10;
            this.limitTxt.Text = "100.0";
            this.limitTxt.TextChanged += new System.EventHandler(this.limitTxt_TextChanged);
            // 
            // btnEvaluate
            // 
            this.btnEvaluate.Location = new System.Drawing.Point(638, 230);
            this.btnEvaluate.Margin = new System.Windows.Forms.Padding(2);
            this.btnEvaluate.Name = "btnEvaluate";
            this.btnEvaluate.Size = new System.Drawing.Size(105, 39);
            this.btnEvaluate.TabIndex = 11;
            this.btnEvaluate.Text = "Evaluate";
            this.btnEvaluate.UseVisualStyleBackColor = true;
            this.btnEvaluate.Click += new System.EventHandler(this.btnEvaluate_Click);
            // 
            // evaluationTSPChck
            // 
            this.evaluationTSPChck.AutoSize = true;
            this.evaluationTSPChck.Checked = true;
            this.evaluationTSPChck.CheckState = System.Windows.Forms.CheckState.Checked;
            this.evaluationTSPChck.Location = new System.Drawing.Point(757, 243);
            this.evaluationTSPChck.Name = "evaluationTSPChck";
            this.evaluationTSPChck.Size = new System.Drawing.Size(15, 14);
            this.evaluationTSPChck.TabIndex = 12;
            this.evaluationTSPChck.UseVisualStyleBackColor = true;
            this.evaluationTSPChck.CheckedChanged += new System.EventHandler(this.EvaluationTSPChck_CheckedChanged);
            // 
            // TspTimeCostFrm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(792, 592);
            this.Controls.Add(this.evaluationTSPChck);
            this.Controls.Add(this.btnEvaluate);
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
            this.Margin = new System.Windows.Forms.Padding(2);
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
        private System.Windows.Forms.Button btnEvaluate;
        private System.Windows.Forms.CheckBox evaluationTSPChck;
    }
}

