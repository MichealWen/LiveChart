//The MIT License(MIT)

//Copyright(c) 2016 Alberto Rodriguez & LiveCharts Contributors

//Permission is hereby granted, free of charge, to any person obtaining a copy
//of this software and associated documentation files (the "Software"), to deal
//in the Software without restriction, including without limitation the rights
//to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
//copies of the Software, and to permit persons to whom the Software is
//furnished to do so, subject to the following conditions:

//The above copyright notice and this permission notice shall be included in all
//copies or substantial portions of the Software.

//THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
//IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
//FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
//AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
//LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
//OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
//SOFTWARE.

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Media;
using System.Windows.Shapes;
using System.Linq;
using System.Windows.Media.Animation;
using System.Windows.Media.Effects;
using LiveCharts.Helpers;
using LiveCharts.Wpf.Points;
using LiveCharts.Wpf;

namespace LiveCharts.Wpf
{
    /// <summary>
    /// The LinearGauge chart.
    /// </summary>
    public class LinearGauge : AngularGauge
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="LinearGauge"/> class.
        /// </summary>
        public LinearGauge()
        {
            Canvas = new Canvas();
            Content = Canvas;
            Canvas.SetBinding(WidthProperty,
                new Binding { Path = new PropertyPath(ActualWidthProperty), Source = this });
            Canvas.SetBinding(HeightProperty,
                new Binding { Path = new PropertyPath(ActualHeightProperty), Source = this });

            SetCurrentValue(SectionsProperty, new List<AngularSection>());

            SetCurrentValue(AnimationsSpeedProperty, TimeSpan.FromMilliseconds(500));
            SetCurrentValue(TicksForegroundProperty, new SolidColorBrush(Color.FromRgb(250, 250, 210)));
            Func<double, string> defaultFormatter = x => x.ToString(CultureInfo.InvariantCulture);
            SetCurrentValue(LabelFormatterProperty, defaultFormatter);
            SetCurrentValue(LabelsEffectProperty,
                new DropShadowEffect { ShadowDepth = 2, RenderingBias = RenderingBias.Performance });
            SetCurrentValue(NeedleFillProperty, new SolidColorBrush(Colors.Red));
            DropShadowEffect dse=new DropShadowEffect()
            {
                Direction = 360,
                Opacity = 0.5
            };
            ValueRec = new Rectangle()
            {
                RadiusX = 3,
                RadiusY = 3,
                Effect = dse
            };
            ValueRec.SetBinding(Shape.FillProperty,
                new Binding { Path = new PropertyPath(NeedleFillProperty), Source = this });

            SizeChanged += (sender, args) =>
            {
                IsControlLaoded = true;
                Draw();
            };

            Slices = new Dictionary<AngularSection, Rectangle>();
       
        }

        #region Properties
        
        internal  Dictionary<AngularSection, Rectangle> Slices { get; set; }

        /// <summary>
        /// 背景宽度(水银柱宽度为背景宽度的四分之一)
        /// </summary>
        public double BackWidth
        {
            get { return (double)GetValue(BackWidthProperty); }
            set { SetValue(BackWidthProperty, value); }
        }

        // Using a DependencyProperty as the backing store for BackWidth.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty BackWidthProperty =
            DependencyProperty.Register("BackWidth", typeof(double), typeof(LinearGauge), new PropertyMetadata(30.0));

        #endregion

        private Rectangle ValueRec;
        /// <summary>
        /// 画实时值
        /// </summary>
        internal override void MoveStick()
        {
            if (ValueRec == null)
                return;
            if (Value < FromValue)
            {
                Value = FromValue;
            }
            if (Value > ToValue)
            {
                Value = ToValue;
            }
            var ValueHight = (GetActualHeight() / (ToValue - FromValue)) * (Value - FromValue);
            

            if (DisableaAnimations)
            {
                ValueRec.Height = ValueHight;
            }
            else
            {
                ValueRec.Height = ValueHight;

                ValueRec.BeginAnimation(Rectangle.HeightProperty,
                    new DoubleAnimation(ValueHight, AnimationsSpeed));
            }
        }

        internal override void Draw()
        {

            if (!IsControlLaoded) return;
            foreach (var child in Canvas.Children.Cast<UIElement>()
                .Where(x => !Equals(x, Stick) && !(x is LinearSection) && !(x is PieSlice)).ToArray())
                Canvas.Children.Remove(child);

            foreach (var section in Sections)
            {
                Rectangle slice;

                section.Owner = this;

                if (!Slices.TryGetValue(section, out slice))
                {
                    slice = new Rectangle();
                    Slices[section] = slice;
                }

                var p = (Canvas)section.Parent;
                if (p != null) p.Children.Remove(section);
                Canvas.Children.Add(section);
                var ps = (Canvas)slice.Parent;
                if (ps != null) ps.Children.Remove(slice);
                Canvas.Children.Add(slice);
            }

            //画区域
            UpdateSections();

            var ts = double.IsNaN(TicksStep) ? DecideInterval((ToValue - FromValue) / 5) : TicksStep;

            var unit = (GetActualHeight() / (ToValue - FromValue));
            //画短刻度
            for (var i = FromValue; i <= ToValue; i += ts)
            {
                var bottom = (i - FromValue) * unit;

                var tick = new Line
                {
                    X1 = ActualWidth * .5,
                    X2 = ActualWidth * .5 + 5,
                    Y1 = ActualHeight - bottom,
                    Y2 = ActualHeight - bottom
                };
                Canvas.Children.Add(tick);
                tick.SetBinding(Shape.StrokeProperty,
                    new Binding { Path = new PropertyPath(TicksForegroundProperty), Source = this });
                tick.SetBinding(Shape.StrokeThicknessProperty,
                    new Binding { Path = new PropertyPath(TicksStrokeThicknessProperty), Source = this });
            }

            var ls = double.IsNaN(LabelsStep) ? DecideInterval((ToValue - FromValue) / 5) : LabelsStep;
            if (ls / (FromValue - ToValue) > 300)
                throw new LiveChartsException("LabelsStep property is too small compared with the range in " +
                                              "the gauge, to avoid performance issues, please increase it.");


            //画长刻度与数字
            for (var i = FromValue; i <= ToValue; i += ls)
            {

                var bottom = (i - FromValue) * unit;


                var tick = new Line
                {
                    X1 = ActualWidth * .5,
                    X2 = ActualWidth * .5 + 10,
                    Y1 = ActualHeight - bottom,
                    Y2 = ActualHeight - bottom,
                };
                Canvas.Children.Add(tick);
                var label = new TextBlock
                {
                    Text = LabelFormatter(i)
                };

                label.SetBinding(EffectProperty,
                    new Binding { Path = new PropertyPath(LabelsEffectProperty), Source = this });

                Canvas.Children.Add(label);
                label.UpdateLayout();

                Canvas.SetLeft(label, ActualWidth * .5 - label.ActualWidth * .5 - 10);
                Canvas.SetBottom(label, bottom - (label.ActualHeight / 2));
                tick.SetBinding(Shape.StrokeProperty,
                    new Binding { Path = new PropertyPath(TicksForegroundProperty), Source = this });
                tick.SetBinding(Shape.StrokeThicknessProperty,
                    new Binding { Path = new PropertyPath(TicksStrokeThicknessProperty), Source = this });
            }
           
            ValueRec.Width = BackWidth / 4;
            Canvas.Children.Add(ValueRec);
            
            Canvas.SetLeft(ValueRec, ActualWidth * 0.5 + ValueRec.Width * 1.5);
            Canvas.SetBottom(ValueRec, 0);
           
            MoveStick();
        

        }



        internal override void UpdateSections()
        {

            if (!IsControlLaoded) return;

            var d = ActualWidth < ActualHeight ? ActualWidth : ActualHeight;

            if (Sections.Any())
            {
                foreach (var section in Sections)
                {
                    if (section.Visibility == Visibility.Visible)
                    {
                        var slice = Slices[section];

                        var h = GetActualHeight() / (ToValue - FromValue) * (section.ToValue - section.FromValue);
                        var bottom = (section.FromValue - FromValue) * GetActualHeight() / (ToValue - FromValue);
                        Canvas.SetBottom(slice, bottom);
                        Canvas.SetLeft(slice, ActualWidth * .5);
                        slice.Height = h;
                        slice.Width = BackWidth;
                        slice.Fill = section.Fill;
                    }

                }
            }
        }



        double GetActualHeight()
        {
            return ActualHeight;
        }
        
    }
}