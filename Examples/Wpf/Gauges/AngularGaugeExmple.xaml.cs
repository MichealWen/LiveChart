using System;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;

namespace Wpf.Gauges
{

    public partial class AngularGaugeExmple : UserControl
    {

        public AngularGaugeExmple()
        {
            InitializeComponent();
            DataContext = this;
        }


        private void ChangeValueOnClick(object sender, RoutedEventArgs e)
        {
            var x= new Random().Next(0, 90);
            Angular.Value = x;
            Button.Content = x.ToString();
        }

     
    }
}
