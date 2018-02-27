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
            this.orderLbl = new System.Windows.Forms.Label();
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
            this.label1 = new System.Windows.Forms.Label();
            this.goalLbl = new System.Windows.Forms.Label();
            this.durationLbl = new System.Windows.Forms.Label();
            this.AttentionWhoreBtn = new System.Windows.Forms.Button();
            this.AttentionWhoreChck = new System.Windows.Forms.CheckBox();
            this.GodBtn = new System.Windows.Forms.Button();
            this.GodChck = new System.Windows.Forms.CheckBox();
            ((System.ComponentModel.ISupportInitialize)(this.Area)).BeginInit();
            this.SuspendLayout();
            // 
            // Area
            // 
            chartArea2.Name = "ChartArea1";
            this.Area.ChartAreas.Add(chartArea2);
            legend2.Name = "Legend1";
            this.Area.Legends.Add(legend2);
            this.Area.Location = new System.Drawing.Point(12, 12);
            this.Area.Margin = new System.Windows.Forms.Padding(3, 2, 3, 2);
            this.Area.Name = "Area";
            series2.ChartArea = "ChartArea1";
            series2.Legend = "Legend1";
            series2.Name = "Series1";
            this.Area.Series.Add(series2);
            this.Area.Size = new System.Drawing.Size(803, 666);
            this.Area.TabIndex = 0;
            this.Area.Text = "Area";
            this.Area.Click += new System.EventHandler(this.Area_Click);
            // 
            // orderLbl
            // 
            this.orderLbl.AutoSize = true;
            this.orderLbl.Location = new System.Drawing.Point(12, 681);
            this.orderLbl.Name = "orderLbl";
            this.orderLbl.Size = new System.Drawing.Size(0, 17);
            this.orderLbl.TabIndex = 1;
            // 
            // TollTSPBtn
            // 
            this.TollTSPBtn.Location = new System.Drawing.Point(851, 87);
            this.TollTSPBtn.Margin = new System.Windows.Forms.Padding(3, 2, 3, 2);
            this.TollTSPBtn.Name = "TollTSPBtn";
            this.TollTSPBtn.Size = new System.Drawing.Size(140, 48);
            this.TollTSPBtn.TabIndex = 2;
            this.TollTSPBtn.Text = "Toll";
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
            this.ClassicTSPBtn.Location = new System.Drawing.Point(851, 12);
            this.ClassicTSPBtn.Margin = new System.Windows.Forms.Padding(3, 2, 3, 2);
            this.ClassicTSPBtn.Name = "ClassicTSPBtn";
            this.ClassicTSPBtn.Size = new System.Drawing.Size(140, 48);
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
            this.tollTSPChck.Location = new System.Drawing.Point(1009, 103);
            this.tollTSPChck.Margin = new System.Windows.Forms.Padding(4, 4, 4, 4);
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
            this.classicTSPChck.Margin = new System.Windows.Forms.Padding(4, 4, 4, 4);
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
            this.limitTSPChck.Margin = new System.Windows.Forms.Padding(4, 4, 4, 4);
            this.limitTSPChck.Name = "limitTSPChck";
            this.limitTSPChck.Size = new System.Drawing.Size(18, 17);
            this.limitTSPChck.TabIndex = 8;
            this.limitTSPChck.UseVisualStyleBackColor = true;
            this.limitTSPChck.CheckedChanged += new System.EventHandler(this.LimitTSPChck_CheckedChanged);
            // 
            // LimitTSPBtn
            // 
            this.LimitTSPBtn.Location = new System.Drawing.Point(851, 160);
            this.LimitTSPBtn.Margin = new System.Windows.Forms.Padding(3, 2, 3, 2);
            this.LimitTSPBtn.Name = "LimitTSPBtn";
            this.LimitTSPBtn.Size = new System.Drawing.Size(140, 48);
            this.LimitTSPBtn.TabIndex = 7;
            this.LimitTSPBtn.Text = "Limit";
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
            this.limitTxt.Margin = new System.Windows.Forms.Padding(3, 2, 3, 2);
            this.limitTxt.Name = "limitTxt";
            this.limitTxt.Size = new System.Drawing.Size(81, 22);
            this.limitTxt.TabIndex = 10;
            this.limitTxt.Text = "100.0";
            this.limitTxt.TextChanged += new System.EventHandler(this.limitTxt_TextChanged);
            // 
            // btnEvaluate
            // 
            this.btnEvaluate.Location = new System.Drawing.Point(851, 283);
            this.btnEvaluate.Margin = new System.Windows.Forms.Padding(3, 2, 3, 2);
            this.btnEvaluate.Name = "btnEvaluate";
            this.btnEvaluate.Size = new System.Drawing.Size(140, 48);
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
            this.evaluationTSPChck.Location = new System.Drawing.Point(1009, 299);
            this.evaluationTSPChck.Margin = new System.Windows.Forms.Padding(4, 4, 4, 4);
            this.evaluationTSPChck.Name = "evaluationTSPChck";
            this.evaluationTSPChck.Size = new System.Drawing.Size(18, 17);
            this.evaluationTSPChck.TabIndex = 12;
            this.evaluationTSPChck.UseVisualStyleBackColor = true;
            this.evaluationTSPChck.CheckedChanged += new System.EventHandler(this.EvaluationTSPChck_CheckedChanged);
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(528, 363);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(0, 17);
            this.label1.TabIndex = 13;
            // 
            // goalLbl
            // 
            this.goalLbl.AutoSize = true;
            this.goalLbl.Location = new System.Drawing.Point(12, 697);
            this.goalLbl.Name = "goalLbl";
            this.goalLbl.Size = new System.Drawing.Size(0, 17);
            this.goalLbl.TabIndex = 14;
            // 
            // durationLbl
            // 
            this.durationLbl.AutoSize = true;
            this.durationLbl.Location = new System.Drawing.Point(12, 714);
            this.durationLbl.Name = "durationLbl";
            this.durationLbl.Size = new System.Drawing.Size(0, 17);
            this.durationLbl.TabIndex = 15;
            // 
            // AttentionWhoreBtn
            // 
            this.AttentionWhoreBtn.Location = new System.Drawing.Point(852, 408);
            this.AttentionWhoreBtn.Margin = new System.Windows.Forms.Padding(3, 2, 3, 2);
            this.AttentionWhoreBtn.Name = "AttentionWhoreBtn";
            this.AttentionWhoreBtn.Size = new System.Drawing.Size(140, 48);
            this.AttentionWhoreBtn.TabIndex = 16;
            this.AttentionWhoreBtn.Text = "Attention Whore";
            this.AttentionWhoreBtn.UseVisualStyleBackColor = true;
            this.AttentionWhoreBtn.Click += new System.EventHandler(this.AttentionWhoreBtn_Click);
            // 
            // AttentionWhoreChck
            // 
            this.AttentionWhoreChck.AutoSize = true;
            this.AttentionWhoreChck.Checked = true;
            this.AttentionWhoreChck.CheckState = System.Windows.Forms.CheckState.Checked;
            this.AttentionWhoreChck.Location = new System.Drawing.Point(1009, 425);
            this.AttentionWhoreChck.Margin = new System.Windows.Forms.Padding(4, 4, 4, 4);
            this.AttentionWhoreChck.Name = "AttentionWhoreChck";
            this.AttentionWhoreChck.Size = new System.Drawing.Size(18, 17);
            this.AttentionWhoreChck.TabIndex = 17;
            this.AttentionWhoreChck.UseVisualStyleBackColor = true;
            this.AttentionWhoreChck.CheckedChanged += new System.EventHandler(this.AttentionWhoreChck_CheckedChanged);
            // 
            // GodBtn
            // 
            this.GodBtn.Location = new System.Drawing.Point(852, 476);
            this.GodBtn.Margin = new System.Windows.Forms.Padding(3, 2, 3, 2);
            this.GodBtn.Name = "GodBtn";
            this.GodBtn.Size = new System.Drawing.Size(140, 48);
            this.GodBtn.TabIndex = 18;
            this.GodBtn.Text = "God";
            this.GodBtn.UseVisualStyleBackColor = true;
            this.GodBtn.Click += new System.EventHandler(this.godBtn_Click);
            // 
            // GodChck
            // 
            this.GodChck.AutoSize = true;
            this.GodChck.Checked = true;
            this.GodChck.CheckState = System.Windows.Forms.CheckState.Checked;
            this.GodChck.Location = new System.Drawing.Point(1009, 493);
            this.GodChck.Margin = new System.Windows.Forms.Padding(4);
            this.GodChck.Name = "GodChck";
            this.GodChck.Size = new System.Drawing.Size(18, 17);
            this.GodChck.TabIndex = 19;
            this.GodChck.UseVisualStyleBackColor = true;
            this.GodChck.CheckedChanged += new System.EventHandler(this.GodChck_CheckedChanged);
            // 
            // TspTimeCostFrm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(8F, 16F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(1056, 741);
            this.Controls.Add(this.GodChck);
            this.Controls.Add(this.GodBtn);
            this.Controls.Add(this.AttentionWhoreChck);
            this.Controls.Add(this.AttentionWhoreBtn);
            this.Controls.Add(this.durationLbl);
            this.Controls.Add(this.goalLbl);
            this.Controls.Add(this.label1);
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
            this.Controls.Add(this.orderLbl);
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
        private System.Windows.Forms.Label orderLbl;
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
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.Label goalLbl;
        private System.Windows.Forms.Label durationLbl;
        private System.Windows.Forms.Button AttentionWhoreBtn;
        private System.Windows.Forms.CheckBox AttentionWhoreChck;
        private System.Windows.Forms.Button GodBtn;
        private System.Windows.Forms.CheckBox GodChck;
    }
}

