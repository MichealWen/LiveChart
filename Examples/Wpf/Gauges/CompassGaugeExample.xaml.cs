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
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace Wpf.Gauges
{
    /// <summary>
    /// CompassGaugeExample.xaml 的交互逻辑
    /// </summary>
    public partial class CompassGaugeExample : UserControl
    {
        public CompassGaugeExample()
        {
            InitializeComponent();
        }
        Random r = new Random();
        private void Button_Click(object sender, RoutedEventArgs e)
        {
            var x = r.Next(0,359);
            Button.Content = x;
            Gauge.Value = x;
            string s = Math.Round(x * 16.667, MidpointRounding.AwayFromZero).ToString();
            if (Gauge.IsMil)
                Button.Content = s;
            else
                Button.Content = x;
        }
    }
}
