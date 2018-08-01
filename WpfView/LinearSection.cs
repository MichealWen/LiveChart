using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Media;
namespace LiveCharts.Wpf
{
    /// <summary>
    /// 
    /// </summary>
    /// <seealso cref="System.Windows.FrameworkElement" />
    public class LinearSection : AngularSection
    {
        internal LinearGauge Owner { get; set; }

       
        private static void Redraw(DependencyObject dependencyObject, DependencyPropertyChangedEventArgs dependencyPropertyChangedEventArgs)
        {
            var LinearSection = (LinearSection)dependencyObject;

            if (LinearSection.Owner == null) return;

            LinearSection.Owner.UpdateSections();
        }
    }
}
