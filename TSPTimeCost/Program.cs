﻿using System;
using System.Collections.Generic;
using System.Windows.Forms;

namespace TSPTimeCost {
    static class Program {

        [STAThread]
        static void Main() {

            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new TspTimeCostFrm());


        }
    }
}
