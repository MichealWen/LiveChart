using System;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;

namespace Wpf.Gauges
{

    public partial class AngularGaugeExmple : UserControl, INotifyPropertyChanged
    {
        private double _value;

        public AngularGaugeExmple()
        {
            InitializeComponent();


            Button.Content = Value.ToString();
            DataContext = this;
        }

        public double Value
        {
            get { return _value; }
            set
            {
                _value = value;
                OnPropertyChanged("Value");
            }
        }

        private void ChangeValueOnClick(object sender, RoutedEventArgs e)
        {
            Value = new Random().Next(500, 1100);
            Button.Content = Value.ToString();
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged(string propertyName = null)
        {
            if (PropertyChanged != null) PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
