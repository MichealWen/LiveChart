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
using LiveCharts.Wpf.Points;
using LiveCharts.Helpers;

namespace LiveCharts.Wpf
{
    /// <summary>
    /// The gauge chart is useful to display progress or completion.
    /// </summary>
    public class CompassGauge : AngularGauge
    {
        /// <summary>
        /// 构造方法
        /// </summary>
        public CompassGauge() : base()
        {
            LabelsStep = 30;
            TicksStep = 6;
            FromValue = 0;
            ToValue = 359;
            Wedge = 360;
        }


        /// <summary>
        /// 是否使用密位
        /// </summary>
        public bool IsMil
        {
            get { return (bool)GetValue(IsMilProperty); }
            set { SetValue(IsMilProperty, value); }
        }

        // Using a DependencyProperty as the backing store for IsMil.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty IsMilProperty =
            DependencyProperty.Register("IsMil", typeof(bool), typeof(CompassGauge), new PropertyMetadata(false));



        internal override void MoveStick()
        {
            var value = Value;
            if (value < FromValue)
            {
                value = FromValue;
            }
            if (value > ToValue)
            {
                value = ToValue;
            }
            Wedge = Wedge > 360 ? 360 : (Wedge < 0 ? 0 : Wedge);

            var fromAlpha = (360 - Wedge) * .5;
            var toAlpha = 360 - fromAlpha;

            var alpha = LinearInterpolation(fromAlpha, toAlpha, FromValue, ToValue, value);

            if (DisableaAnimations)
            {
                StickRotateTransform.Angle = alpha;
            }
            else
            {
                StickRotateTransform.BeginAnimation(RotateTransform.AngleProperty,
                    new DoubleAnimation(alpha, AnimationsSpeed));
            }
        }

        internal override void Draw()
        {
            if (!IsControlLaoded) return;
          
            
            //No cache for you gauge :( kill and redraw please
            foreach (var child in Canvas.Children.Cast<UIElement>()
                .Where(x => !Equals(x, Stick) && !(x is CompassSection) && !(x is PieSlice)).ToArray())
                Canvas.Children.Remove(child);


            var fromAlpha = (360 - Wedge) * .5;
            var toAlpha = 360 - fromAlpha;

            var d = ActualWidth < ActualHeight ? ActualWidth : ActualHeight;

            Stick.Height = d * .5 * .8;
            Stick.Width = Stick.Height * .2;

            Canvas.SetLeft(Stick, ActualWidth * .5 - Stick.Width * .5);
            Canvas.SetTop(Stick, ActualHeight * .5 - Stick.Height * .9);

            var ticksHi = d * .5;
            var ticksHj = d * .47;
            var labelsHj = d * .44;

            foreach (var section in Sections)
            {
                PieSlice slice;

                section.Owner = this;

                if (!Slices.TryGetValue(section, out slice))
                {
                    slice = new PieSlice();
                    Slices[section] = slice;
                }

                var p = (Canvas)section.Parent;
                if (p != null) p.Children.Remove(section);
                Canvas.Children.Add(section);
                var ps = (Canvas)slice.Parent;
                if (ps != null) ps.Children.Remove(slice);
                Canvas.Children.Add(slice);
            }

            UpdateSections();

            var ts = double.IsNaN(TicksStep) ? DecideInterval((ToValue - FromValue) / 5) : TicksStep;
            if (ts / (FromValue - ToValue) > 300)
                throw new LiveChartsException("TicksStep property is too small compared with the range in " +
                                              "the gauge, to avoid performance issues, please increase it.");

            for (var i = FromValue; i <= ToValue; i += ts)
            {
                var alpha = LinearInterpolation(fromAlpha, toAlpha, FromValue, ToValue, i) - 90;

                var tick = new Line
                {
                    X1 = ActualWidth * .5 + ticksHi * Math.Cos(alpha * Math.PI / 180),
                    X2 = ActualWidth * .5 + ticksHj * Math.Cos(alpha * Math.PI / 180),
                    Y1 = ActualHeight * .5 + ticksHi * Math.Sin(alpha * Math.PI / 180),
                    Y2 = ActualHeight * .5 + ticksHj * Math.Sin(alpha * Math.PI / 180)
                };
                Canvas.Children.Add(tick);
                tick.SetBinding(Shape.StrokeProperty,
                   new Binding { Path = new PropertyPath(TicksForegroundProperty), Source = this });
                tick.SetBinding(Shape.StrokeThicknessProperty,
                    new Binding { Path = new PropertyPath(TicksStrokeThicknessProperty), Source = this });
            }

            var ls = double.IsNaN(LabelsStep) ? 45 : LabelsStep;
            if (ls / (FromValue - ToValue) > 300)
                throw new LiveChartsException("LabelsStep property is too small compared with the range in " +
                                              "the gauge, to avoid performance issues, please increase it.");
            
            for (var i = FromValue; i <= ToValue; i += ls)
            {
                if (i % 90 == 0 && !IsMil)
                    continue;
                DrawTick(fromAlpha, toAlpha, ticksHi, labelsHj, i);

            }
            if (!IsMil)
            {
                //绘制东西南北标签
                for (var i = FromValue; i <= ToValue; i += (ToValue + 1) / 4)
                {
                    DrawTick(fromAlpha, toAlpha, ticksHi, labelsHj, i);

                }
            }
            MoveStick();
        }

        /// <summary>
        /// 绘制长标签与lable
        /// </summary>
        /// <param name="fromAlpha"></param>
        /// <param name="toAlpha"></param>
        /// <param name="ticksHi"></param>
        /// <param name="labelsHj"></param>
        /// <param name="i"></param>
        void DrawTick(double fromAlpha, double toAlpha, double ticksHi, double labelsHj, double i)
        {
            var alpha = LinearInterpolation(fromAlpha, toAlpha, FromValue, ToValue, i) - 90;

            var tick = new Line
            {
                X1 = ActualWidth * .5 + ticksHi * Math.Cos(alpha * Math.PI / 180),
                X2 = ActualWidth * .5 + labelsHj * Math.Cos(alpha * Math.PI / 180),
                Y1 = ActualHeight * .5 + ticksHi * Math.Sin(alpha * Math.PI / 180),
                Y2 = ActualHeight * .5 + labelsHj * Math.Sin(alpha * Math.PI / 180)
            };

            Canvas.Children.Add(tick);

            tick.SetBinding(Shape.StrokeProperty,
                new Binding { Path = new PropertyPath(TicksForegroundProperty), Source = this });
            tick.SetBinding(Shape.StrokeThicknessProperty,
                new Binding { Path = new PropertyPath(TicksStrokeThicknessProperty), Source = this });
            string text = i.ToString();
            if (i % 90 == 0)
            {
                switch (i.ToString())
                {
                    case "0":
                        text = "北";
                        break;
                    case "90":
                        text = "东";
                        break;
                    case "180":
                        text = "南";
                        break;
                    case "270":
                        text = "西";
                        break;
                    default:
                        break;
                }
            }
            if (IsMil)
            {
                if (i == 0)
                    text = "0-00";
                else
                {
                    text =Math.Round((i * 16.667), MidpointRounding.AwayFromZero).ToString();
                    if (text.Length == 2) text = "0-" + text;
                    else
                    {
                        char[] strArr;
                        strArr = text.Length > 4 ? text.Take(4).ToArray() : text.ToArray();
                        var num = strArr.Length;
                        string s = new string(strArr,0,num-2);
                        string e = new string(strArr, num - 2, 2);
                        text = s + "-" + e;
                    }
                }
                
            }
            var label = new TextBlock
            {
                Text = text
            };

            label.SetBinding(EffectProperty,
                new Binding { Path = new PropertyPath(LabelsEffectProperty), Source = this });

            Canvas.Children.Add(label);
            label.UpdateLayout();
            if (alpha >= -90 && alpha < 0)
            {
                Canvas.SetLeft(label, tick.X2 - label.ActualWidth * .7);
                Canvas.SetTop(label, tick.Y2);
            }
            else if (alpha > 0 && alpha < 90)
            {
                Canvas.SetLeft(label, tick.X2 - label.ActualWidth);
                Canvas.SetTop(label, tick.Y2 - label.ActualHeight * .7);
            }
            else if (alpha >= 90 && alpha <= 180)
            {
                Canvas.SetLeft(label, tick.X2-label.ActualWidth*.5);
                Canvas.SetTop(label, tick.Y2 - label.ActualHeight * .7);
            }
            else
            {
                Canvas.SetLeft(label, tick.X2);
                Canvas.SetTop(label, tick.Y2);
            }
            if (label.Text == "东")
            {
                Canvas.SetLeft(label, tick.X2 - label.ActualWidth * 1.1);
                Canvas.SetTop(label, tick.Y2 - label.ActualHeight * .5);
            }
            if (label.Text == "南")
            {
                Canvas.SetLeft(label, tick.X2 - label.ActualWidth * .5);
                Canvas.SetTop(label, tick.Y2 - label.ActualHeight *1.05);
            }
            if (label.Text == "西")
            {
                Canvas.SetLeft(label, tick.X2);
                Canvas.SetTop(label, tick.Y2 - label.ActualHeight * 0.5);
            }
            if (label.Text == "北")
            {
                Canvas.SetLeft(label, tick.X2 - label.ActualWidth * .5);
                Canvas.SetTop(label, tick.Y2);
            }

        }

        internal override void UpdateSections()
        {
            if (!IsControlLaoded) return;

            var fromAlpha = (360 - Wedge) * .5;
            var toAlpha = 360 - fromAlpha;
            var d = ActualWidth < ActualHeight ? ActualWidth : ActualHeight;

            if (Sections.Any())
            {
                SectionsInnerRadius = SectionsInnerRadius > 1
                    ? 1
                    : (SectionsInnerRadius < 0
                        ? 0
                        : SectionsInnerRadius);

                foreach (var section in Sections)
                {
                    if (section.Visibility != Visibility.Visible)
                        continue;
                    var slice = Slices[section];

                    Canvas.SetTop(slice, ActualHeight * .5);
                    Canvas.SetLeft(slice, ActualWidth * .5);

                    var start = LinearInterpolation(fromAlpha, toAlpha,
                        FromValue, ToValue, section.FromValue);
                    var end = LinearInterpolation(fromAlpha, toAlpha,
                        FromValue, ToValue, section.ToValue);

                    slice.RotationAngle = start;
                    slice.WedgeAngle = end - start;
                    slice.Radius = d * .5;
                    slice.InnerRadius = d * .5 * SectionsInnerRadius;
                    slice.Fill = section.Fill;
                }
            }
        }

    }
}