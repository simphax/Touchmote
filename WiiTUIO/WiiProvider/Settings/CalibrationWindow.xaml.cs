using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace WiiTUIO.Provider
{
    /// <summary>
    /// Interaction logic for CalibrationWindow.xaml
    /// </summary>
    public partial class CalibrationWindow : Window
    {
        public CalibrationWindow()
        {
            InitializeComponent();

            CalibrationCanvas.OnCalibrationFinished += CalibrationCanvas_OnCalibrationFinished;
        }

        void CalibrationCanvas_OnCalibrationFinished(WiiProvider.CalibrationRectangle arg1, WiiProvider.CalibrationRectangle arg2, Vector arg3)
        {
            this.Hide();
        }

        private void Window_KeyUp_1(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Escape)
            {
                this.Hide();
            }
        }
    }
}
