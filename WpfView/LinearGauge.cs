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
    public class LinearGauge : UserControl
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="LinearGauge"/> class.
        /// </summary>
        public LinearGauge()
        {
            RectangleGeometry myRectangleGeometry = new RectangleGeometry();
            myRectangleGeometry.Rect = new Rect(500, 500, 25, 25);

            Canvas = new Canvas();
            Content = Canvas;

            StickRotateTransform = new RotateTransform(180);
            Stick = new Path
            {

                //Data = myRectangleGeometry,//("m0,90 a5,5 0 0 0 20,0 l-8,-88 a2,2 0 0 0 -4 0 z"),
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

            SetCurrentValue(SectionsProperty, new List<LinearSection>());
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

            Slices = new Dictionary<LinearSection, Rectangle>();
        }

        #region Properties

        private Canvas Canvas { get; set; }
        
        private Path Stick { get; set; }
        private RotateTransform StickRotateTransform { get; set; }
        private bool IsControlLaoded { get; set; }
        private Dictionary<LinearSection, Rectangle> Slices { get; set; }


        /// <summary>
        /// The ticks step property
        /// </summary>
        public static readonly DependencyProperty TicksStepProperty = DependencyProperty.Register(
            "TicksStep", typeof(double), typeof(LinearGauge),
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
            "LabelsStep", typeof(double), typeof(LinearGauge),
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
            "FromValue", typeof(double), typeof(LinearGauge),
            new PropertyMetadata(0d, Redraw));
        /// <summary>
        /// Gets or sets the minimum value of the gauge
        /// </summary>
        public double FromValue
        {
            get { return (double)GetValue(FromValueProperty); }
            set { SetValue(FromValueProperty, value); }
        }

        /// <summary>
        /// To value property
        /// </summary>
        public static readonly DependencyProperty ToValueProperty = DependencyProperty.Register(
            "ToValue", typeof(double), typeof(LinearGauge),
            new PropertyMetadata(100d, Redraw));
        /// <summary>
        /// Gets or sets the maximum value of the gauge
        /// </summary>
        public double ToValue
        {
            get { return (double)GetValue(ToValueProperty); }
            set { SetValue(ToValueProperty, value); }
        }

        /// <summary>
        /// The sections property
        /// </summary>
        public static readonly DependencyProperty SectionsProperty = DependencyProperty.Register(
            "Sections", typeof(List<LinearSection>), typeof(LinearGauge),
            new PropertyMetadata(default(SectionsCollection), Redraw));
        /// <summary>
        /// Gets or sets a collection of sections
        /// </summary>
        public List<LinearSection> Sections
        {
            get { return (List<LinearSection>)GetValue(SectionsProperty); }
            set { SetValue(SectionsProperty, value); }
        }

        /// <summary>
        /// The value property
        /// </summary>
        public static readonly DependencyProperty ValueProperty = DependencyProperty.Register(
            "Value", typeof(double), typeof(LinearGauge),
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
            "LabelFormatter", typeof(Func<double, string>), typeof(LinearGauge), new PropertyMetadata(default(Func<double, string>)));
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
            "DisableaAnimations", typeof(bool), typeof(LinearGauge), new PropertyMetadata(default(bool)));
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
            "AnimationsSpeed", typeof(TimeSpan), typeof(LinearGauge), new PropertyMetadata(default(TimeSpan)));
        /// <summary>
        /// Gets or sets the animations speed
        /// </summary>
        public TimeSpan AnimationsSpeed
        {
            get { return (TimeSpan)GetValue(AnimationsSpeedProperty); }
            set { SetValue(AnimationsSpeedProperty, value); }
        }



        public string Type
        {
            get { return (string)GetValue(TypeProperty); }
            set { SetValue(TypeProperty, value); }
        }

        // Using a DependencyProperty as the backing store for Type.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty TypeProperty =
            DependencyProperty.Register("Type", typeof(string), typeof(LinearGauge), new PropertyMetadata(""));



        /// <summary>
        /// The ticks foreground property
        /// </summary>
        public static readonly DependencyProperty TicksForegroundProperty = DependencyProperty.Register(
            "TicksForeground", typeof(Brush), typeof(LinearGauge), new PropertyMetadata(default(Brush)));
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
            "SectionsInnerRadius", typeof(double), typeof(LinearGauge),
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
            "NeedleFill", typeof(Brush), typeof(LinearGauge), new PropertyMetadata(default(Brush)));
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
            "LabelsEffect", typeof(Effect), typeof(LinearGauge), new PropertyMetadata(default(Effect)));

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
            "TicksStrokeThickness", typeof(double), typeof(LinearGauge), new PropertyMetadata(2d));

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

        /// <summary>
        /// 背景宽度
        /// </summary>
        public double BackWidth = 30;
        #endregion

        private static void ValueChangedCallback(DependencyObject o, DependencyPropertyChangedEventArgs e)
        {
            var ag = (LinearGauge)o;
            ag.MoveStick();
        }

        private static void Redraw(DependencyObject o, DependencyPropertyChangedEventArgs e)
        {
            var ag = (LinearGauge)o;
            ag.Draw();
        }

        /// <summary>
        /// 画实时值
        /// </summary>
        private void MoveStick()
        {
            var ValueHight = (GetActualHeight() / (ToValue - FromValue)) * (Value-FromValue);
            Rectangle ValueRec = new Rectangle();
            SolidColorBrush s = new SolidColorBrush(Color.FromRgb(192, 192, 192));
            ValueRec.Fill = s;
            ValueRec.Height = ValueHight;
            ValueRec.Width = BackWidth/2;
            Canvas.Children.Add(ValueRec);
            Canvas.SetLeft(ValueRec, ActualWidth * .5+ValueRec.Width*.5);
            Canvas.SetBottom(ValueRec, BottomHight);
        }

        /// <summary>
        /// 为顶部预留
        /// </summary>
        double topHight = 5;
        internal void Draw()
        {
            
            if (!IsControlLaoded) return;

            //No cache for you gauge :( kill and redraw please
            foreach (var child in Canvas.Children.Cast<UIElement>()
                .Where(x => !Equals(x, Stick) && !(x is LinearSection) && !(x is PieSlice)).ToArray())
                Canvas.Children.Remove(child);
            ////画背景
            DrawBackGround();
            ////画底部
            //DrawBottom();

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
            if (ts / (FromValue - ToValue) > 300)
                throw new LiveChartsException("TicksStep property is too small compared with the range in " +
                                              "the gauge, to avoid performance issues, please increase it.");

            var unit = (GetActualHeight() / (ToValue - FromValue));
            //画短刻度
            for (var i = FromValue; i <= ToValue; i += ts)
            {
                var bottom = (i - FromValue) * unit;

                var tick = new Line
                {
                    X1 = ActualWidth * .5,
                    X2 = ActualWidth * .5+5 ,
                    Y1 = bottom+5,
                    Y2 = bottom+5
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
                    X2 = ActualWidth * .5+10,
                    Y1 = bottom+5,
                    Y2 = bottom+5,
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
              
                Canvas.SetLeft(label,ActualWidth*.5-label.ActualWidth*.5-10);
                Canvas.SetBottom(label,bottom-3+ BottomHight);
                tick.SetBinding(Shape.StrokeProperty,
                    new Binding { Path = new PropertyPath(TicksForegroundProperty), Source = this });
                tick.SetBinding(Shape.StrokeThicknessProperty,
                    new Binding { Path = new PropertyPath(TicksStrokeThicknessProperty), Source = this });
            }
          
            MoveStick();
      
          
        }

        /// <summary>
        /// 画背景
        /// </summary>
        private void DrawBackGround()
        {
            Rectangle rec = new Rectangle();
            rec.Height =  ActualHeight-BottomHight+12;
            rec.Width = BackWidth;
            if (string.IsNullOrEmpty(Type) || Type == "WDJ")
                rec.Fill = new SolidColorBrush(Colors.Green);
            if(Type == "YLT")
                rec.Fill = new SolidColorBrush(Colors.Gray);
            rec.RadiusX = 5;
            rec.RadiusY = 5;
            Canvas.Children.Add(rec);
            Canvas.SetTop(rec,0);
            Canvas.SetLeft(rec, ActualWidth * .5);

        }

        internal void UpdateSections()
        {
            if (!IsControlLaoded) return;
            
            var d = ActualWidth < ActualHeight ? ActualWidth : ActualHeight;

            if (Sections.Any())
            {
                foreach (var section in Sections)
                {
                    var slice = Slices[section];

                    var h = GetActualHeight() / (ToValue - FromValue) * (section.ToValue - section.FromValue);
                    var bottom = (section.FromValue - FromValue) * GetActualHeight() / (ToValue - FromValue);
                    Canvas.SetBottom(slice,bottom+ BottomHight);
                    Canvas.SetLeft(slice, ActualWidth * .5);
                    if(Sections.IndexOf(section) == Sections.Count - 1)
                    {
                        Canvas.SetTop(slice, topHight);
                      
                    }
                    slice.Height = h;
                    slice.Width = BackWidth;
                    slice.Fill = section.Fill;
                }
            }
        }

        /// <summary>
        /// 底部圆球的高度/直径
        /// </summary>
        public double BottomHight
        {
            get {
                if (string.IsNullOrEmpty(Type)||Type == "WDJ")
                    return BottomHight = BackWidth + 30;
                else
                    return 12;
            }
            set {; }
        }


        /// <summary>
        /// 画底
        /// </summary>
        void DrawBottom()
        {
            if (string.IsNullOrEmpty(Type))
            {
                Type = "WDJ";
            }
            if (Type == "WDJ")
            {
                Ellipse ell = new Ellipse();
                ell.Height = BottomHight;
                ell.Width = BottomHight;
                ell.Fill = new SolidColorBrush(Colors.Green);
                Canvas.Children.Add(ell);
                var left = (BottomHight - BackWidth) / 2;
                Canvas.SetLeft(ell, ActualWidth * .5-left);
                Canvas.SetBottom(ell, 0);
                Ellipse ellInner = new Ellipse();
                ellInner.Height = BottomHight*.6;
                ellInner.Width = BottomHight*.6;
                double radiusDifference = (ell.Height - ellInner.Height)/2;
                ellInner.Fill = new SolidColorBrush(Color.FromRgb(192,192,192));
                Canvas.Children.Add(ellInner);
                Canvas.SetLeft(ellInner, (ActualWidth * .5 - left) + radiusDifference);
                Canvas.SetBottom(ellInner, radiusDifference);
                Rectangle rec = new Rectangle();
                rec.Width = BackWidth/2;
                rec.Height = radiusDifference+10;
                rec.Fill = new SolidColorBrush(Color.FromRgb(192, 192, 192));
                Canvas.Children.Add(rec);
                Canvas.SetLeft(rec, ActualWidth * .5+BackWidth/4);
                Canvas.SetBottom(rec, radiusDifference+ellInner.Height-5);
            }
            if (Type == "YLT")
            {
                Border bor = new Border();
                bor.CornerRadius = new CornerRadius(500,500,0,0);
                bor.Background = new SolidColorBrush(Colors.Gray);
                bor.Height = 12;
                bor.Width = BackWidth + 20;
                Canvas.Children.Add(bor);
                Canvas.SetLeft(bor, ActualWidth * .5 -10);
                Canvas.SetBottom(bor, 0);
                Rectangle rec = new Rectangle();
                rec.Height = 10;
                rec.Width = BackWidth;
                rec.Fill = new SolidColorBrush(Colors.Gray);
                Canvas.Children.Add(rec);
                Canvas.SetLeft(rec, ActualWidth * .5);
                Canvas.SetBottom(rec, 2);
            }

        }
      
        double GetActualHeight()
        {
            if (ActualHeight == 0) return 0;
            return ActualHeight - BottomHight- topHight; 
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