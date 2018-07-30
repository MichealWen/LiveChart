﻿//The MIT License(MIT)

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
    public class CompassGauge : UserControl
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="CompassGauge"/> class.
        /// </summary>
        public CompassGauge()
        {


            Canvas = new Canvas();
            Content = Canvas;

            StickRotateTransform = new RotateTransform(180);
            Stick = new Path
            {

                Data = Geometry.Parse("m0,90 a5,5 0 0 0 20,0 l-8,-88 a2,2 0 0 0 -4 0 z"),
                Fill = Brushes.CornflowerBlue,
                Stretch = Stretch.Fill,
                RenderTransformOrigin = new Point(0.5, 0.9),
                RenderTransform = StickRotateTransform
            };
            Canvas.Children.Add(Stick);
            Panel.SetZIndex(Stick, 1);

            Canvas.SetBinding(WidthProperty,
                new Binding { Path = new PropertyPath(ActualWidthProperty), Source = this });
            Canvas.SetBinding(HeightProperty,
                new Binding { Path = new PropertyPath(ActualHeightProperty), Source = this });

            SetCurrentValue(SectionsProperty, new List<CompassSection>());
            SetCurrentValue(NeedleFillProperty, new SolidColorBrush(Color.FromRgb(255, 90, 100)));

            Stick.SetBinding(Shape.FillProperty,
                new Binding { Path = new PropertyPath(NeedleFillProperty), Source = this });

            SetCurrentValue(AnimationsSpeedProperty, TimeSpan.FromMilliseconds(500));
            SetCurrentValue(TicksForegroundProperty, new SolidColorBrush(Color.FromRgb(250, 250, 210)));
            Func<double, string> defaultFormatter = x => x.ToString(CultureInfo.InvariantCulture);
            SetCurrentValue(LabelFormatterProperty, defaultFormatter);
            SetCurrentValue(LabelsEffectProperty,
                new DropShadowEffect { ShadowDepth = 2, RenderingBias = RenderingBias.Performance });

            SizeChanged += (sender, args) =>
            {
                IsControlLaoded = true;
                Draw();
            };

            Slices = new Dictionary<CompassSection, PieSlice>();
        }

        #region Properties

        private Canvas Canvas { get; set; }
        private Path Stick { get; set; }
        private RotateTransform StickRotateTransform { get; set; }
        private bool IsControlLaoded { get; set; }
        private Dictionary<CompassSection, PieSlice> Slices { get; set; }

        /// <summary>
        /// The wedge property
        /// </summary>
        public static readonly DependencyProperty WedgeProperty = DependencyProperty.Register(
            "Wedge", typeof(double), typeof(CompassGauge),
            new PropertyMetadata(300d, Redraw));
        /// <summary>
        /// Gets or sets the opening angle in the gauge
        /// </summary>
        private double Wedge
        {
            get { return 360; }
            set { SetValue(WedgeProperty, value); }
        }

        /// <summary>
        /// The ticks step property
        /// </summary>
        public static readonly DependencyProperty TicksStepProperty = DependencyProperty.Register(
            "TicksStep", typeof(double), typeof(CompassGauge),
            new PropertyMetadata(double.NaN, Redraw));
        /// <summary>
        /// Gets or sets the separation between every tick
        /// </summary>
        public double TicksStep
        {
            get { return (double)GetValue(TicksStepProperty); }
            set { SetValue(TicksStepProperty, value); }
        }

        /// <summary>
        /// The labels step property
        /// </summary>
        public static readonly DependencyProperty LabelsStepProperty = DependencyProperty.Register(
            "LabelsStep", typeof(double), typeof(CompassGauge),
            new PropertyMetadata(double.NaN, Redraw));
        /// <summary>
        /// Gets or sets the separation between every label
        /// </summary>
        public double LabelsStep
        {
            get { return (double)GetValue(LabelsStepProperty); }
            set { SetValue(LabelsStepProperty, value); }
        }

        /// <summary>
        /// From value property
        /// </summary>
        public static readonly DependencyProperty FromValueProperty = DependencyProperty.Register(
            "FromValue", typeof(double), typeof(CompassGauge),
            new PropertyMetadata(0d, Redraw));
        /// <summary>
        /// Gets or sets the minimum value of the gauge
        /// </summary>
        private double FromValue
        {
            get { return 0; }
            set { SetValue(FromValueProperty, value); }
        }

        /// <summary>
        /// To value property
        /// </summary>
        public static readonly DependencyProperty ToValueProperty = DependencyProperty.Register(
            "ToValue", typeof(double), typeof(CompassGauge),
            new PropertyMetadata(100d, Redraw));
        /// <summary>
        /// Gets or sets the maximum value of the gauge
        /// </summary>
        private double ToValue
        {
            get { return 359; }
            set { SetValue(ToValueProperty, value); }
        }

        /// <summary>
        /// The sections property
        /// </summary>
        public static readonly DependencyProperty SectionsProperty = DependencyProperty.Register(
            "Sections", typeof(List<CompassSection>), typeof(CompassGauge),
            new PropertyMetadata(default(SectionsCollection), Redraw));
        /// <summary>
        /// Gets or sets a collection of sections
        /// </summary>
        public List<CompassSection> Sections
        {
            get { return (List<CompassSection>)GetValue(SectionsProperty); }
            set { SetValue(SectionsProperty, value); }
        }

        /// <summary>
        /// The value property
        /// </summary>
        public static readonly DependencyProperty ValueProperty = DependencyProperty.Register(
            "Value", typeof(double), typeof(CompassGauge),
            new PropertyMetadata(default(double), ValueChangedCallback));
        /// <summary>
        /// Gets or sets the current gauge value
        /// </summary>
        public double Value
        {
            get { return (double)GetValue(ValueProperty); }
            set { SetValue(ValueProperty, value); }
        }

        /// <summary>
        /// The label formatter property
        /// </summary>
        public static readonly DependencyProperty LabelFormatterProperty = DependencyProperty.Register(
            "LabelFormatter", typeof(Func<double, string>), typeof(CompassGauge), new PropertyMetadata(default(Func<double, string>)));
        /// <summary>
        /// Gets or sets the label formatter
        /// </summary>
        public Func<double, string> LabelFormatter
        {
            get { return (Func<double, string>)GetValue(LabelFormatterProperty); }
            set { SetValue(LabelFormatterProperty, value); }
        }

        /// <summary>
        /// The disablea animations property
        /// </summary>
        public static readonly DependencyProperty DisableaAnimationsProperty = DependencyProperty.Register(
            "DisableaAnimations", typeof(bool), typeof(CompassGauge), new PropertyMetadata(default(bool)));
        /// <summary>
        /// Gets or sets whether the chart is animated
        /// </summary>
        public bool DisableaAnimations
        {
            get { return (bool)GetValue(DisableaAnimationsProperty); }
            set { SetValue(DisableaAnimationsProperty, value); }
        }

        /// <summary>
        /// The animations speed property
        /// </summary>
        public static readonly DependencyProperty AnimationsSpeedProperty = DependencyProperty.Register(
            "AnimationsSpeed", typeof(TimeSpan), typeof(CompassGauge), new PropertyMetadata(default(TimeSpan)));
        /// <summary>
        /// Gets or sets the animations speed
        /// </summary>
        public TimeSpan AnimationsSpeed
        {
            get { return (TimeSpan)GetValue(AnimationsSpeedProperty); }
            set { SetValue(AnimationsSpeedProperty, value); }
        }

        /// <summary>
        /// The ticks foreground property
        /// </summary>
        public static readonly DependencyProperty TicksForegroundProperty = DependencyProperty.Register(
            "TicksForeground", typeof(Brush), typeof(CompassGauge), new PropertyMetadata(default(Brush)));
        /// <summary>
        /// Gets or sets the ticks foreground
        /// </summary>
        public Brush TicksForeground
        {
            get { return (Brush)GetValue(TicksForegroundProperty); }
            set { SetValue(TicksForegroundProperty, value); }
        }

        /// <summary>
        /// The sections inner radius property
        /// </summary>
        public static readonly DependencyProperty SectionsInnerRadiusProperty = DependencyProperty.Register(
            "SectionsInnerRadius", typeof(double), typeof(CompassGauge),
            new PropertyMetadata(0.94d, Redraw));
        /// <summary>
        /// Gets or sets the inner radius of all the sections in the chart, the unit of this property is percentage, goes from 0 to 1
        /// </summary>
        public double SectionsInnerRadius
        {
            get { return (double)GetValue(SectionsInnerRadiusProperty); }
            set { SetValue(SectionsInnerRadiusProperty, value); }
        }

        /// <summary>
        /// The needle fill property
        /// </summary>
        public static readonly DependencyProperty NeedleFillProperty = DependencyProperty.Register(
            "NeedleFill", typeof(Brush), typeof(CompassGauge), new PropertyMetadata(default(Brush)));
        /// <summary>
        /// Gets o sets the needle fill
        /// </summary>
        public Brush NeedleFill
        {
            get { return (Brush)GetValue(NeedleFillProperty); }
            set { SetValue(NeedleFillProperty, value); }
        }

        /// <summary>
        /// The labels effect property
        /// </summary>
        public static readonly DependencyProperty LabelsEffectProperty = DependencyProperty.Register(
            "LabelsEffect", typeof(Effect), typeof(CompassGauge), new PropertyMetadata(default(Effect)));

        /// <summary>
        /// Gets or sets the labels effect.
        /// </summary>
        /// <value>
        /// The labels effect.
        /// </value>
        public Effect LabelsEffect
        {
            get { return (Effect)GetValue(LabelsEffectProperty); }
            set { SetValue(LabelsEffectProperty, value); }
        }

        /// <summary>
        /// The ticks stroke thickness property
        /// </summary>
        public static readonly DependencyProperty TicksStrokeThicknessProperty = DependencyProperty.Register(
            "TicksStrokeThickness", typeof(double), typeof(CompassGauge), new PropertyMetadata(2d));

        /// <summary>
        /// Gets or sets the ticks stroke thickness.
        /// </summary>
        /// <value>
        /// The ticks stroke thickness.
        /// </value>
        public double TicksStrokeThickness
        {
            get { return (double)GetValue(TicksStrokeThicknessProperty); }
            set { SetValue(TicksStrokeThicknessProperty, value); }
        }

        #endregion

        private static void ValueChangedCallback(DependencyObject o, DependencyPropertyChangedEventArgs e)
        {
            var ag = (CompassGauge)o;
            ag.MoveStick();
        }

        private static void Redraw(DependencyObject o, DependencyPropertyChangedEventArgs e)
        {
            var ag = (CompassGauge)o;
            ag.Draw();
        }

        private void MoveStick()
        {
            Wedge = Wedge > 360 ? 360 : (Wedge < 0 ? 0 : Wedge);

            var fromAlpha = (360 - Wedge) * .5;
            var toAlpha = 360 - fromAlpha;

            var alpha = LinearInterpolation(fromAlpha, toAlpha, FromValue, ToValue, Value);

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

        internal void Draw()
        {
            if (!IsControlLaoded) return;

            //No cache for you gauge :( kill and redraw please
            foreach (var child in Canvas.Children.Cast<UIElement>()
                .Where(x => !Equals(x, Stick) && !(x is CompassSection) && !(x is PieSlice)).ToArray())
                Canvas.Children.Remove(child);

            Wedge = Wedge > 360 ? 360 : (Wedge < 0 ? 0 : Wedge);

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
                if (i % 90 == 0)
                    continue;
                DrawTick(fromAlpha, toAlpha, ticksHi, labelsHj, i);

            }
            //绘制东西南北标签
            for (var i = FromValue; i <= ToValue; i += 90)
            {
                DrawTick(fromAlpha,toAlpha,ticksHi,labelsHj,i);

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
        void DrawTick(double fromAlpha, double toAlpha, double ticksHi,double labelsHj,double i)
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
            string text = "";
            if(i%90==0)
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
                        text = "东";
                        break;
                    default:
                        break;
                }
            }
            else
            {
                text = LabelFormatter(i);
            }
            var label = new TextBlock
            {
                Text = text
            };

            label.SetBinding(EffectProperty,
                new Binding { Path = new PropertyPath(LabelsEffectProperty), Source = this });

            Canvas.Children.Add(label);
            label.UpdateLayout();
            Canvas.SetLeft(label, alpha < 270
                ? tick.X2
                : (Math.Abs(alpha - 270) < 4
                    ? tick.X2 - label.ActualWidth * .5
                    : tick.X2 - label.ActualWidth));
            Canvas.SetTop(label, tick.Y2);
        }

        internal void UpdateSections()
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

        private static double LinearInterpolation(double fromA, double toA, double fromB, double toB, double value)
        {
            var p1 = new Point(fromB, fromA);
            var p2 = new Point(toB, toA);

            var deltaX = p2.X - p1.X;
            // ReSharper disable once CompareOfFloatsByEqualityOperator
            var m = (p2.Y - p1.Y) / (deltaX == 0 ? double.MinValue : deltaX);

            return m * (value - p1.X) + p1.Y;
        }

        private static double DecideInterval(double minimum)
        {
            var magnitude = Math.Pow(10, Math.Floor(Math.Log(minimum) / Math.Log(10)));

            var residual = minimum / magnitude;
            double tick;
            if (residual > 5)
                tick = 10 * magnitude;
            else if (residual > 2)
                tick = 5 * magnitude;
            else if (residual > 1)
                tick = 2 * magnitude;
            else
                tick = magnitude;

            return tick;
        }
    }
}