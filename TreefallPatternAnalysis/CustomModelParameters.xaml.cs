using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace TreefallPatternAnalysis
{
    /// <summary>
    /// Interaction logic for CustomModelParameters.xaml
    /// </summary>
    public partial class CustomModelParameters : UserControl
    {
        public struct Params
        {
            public double vr;
            public double vt;
            public double vs;
            public double vc;
            public double rmax;
            public double dx;
            public bool displayVectors;
            public bool displayCurve;
            public bool displayRmax;
        }

        private static readonly double[] vrTable = [0.333, 0.519, 0.621, 0.711, 0.8, 0.889, 0.978, 1.067, 1.156, 1.244, 1.333, 1.431, 1.552, 1.711, 1.947, 2.667];
        private static readonly double[] vtTable = [0.017, 0.11, 0.197, 0.284, 0.372, 0.459, 0.547, 0.634, 0.721, 0.809, 0.896, 0.984, 1.079, 1.205, 1.395, 2.0];
        private static readonly double[] vsTable = [0.083, 0.139, 0.17, 0.2, 0.23, 0.259, 0.289, 0.319, 0.348, 0.378, 0.407, 0.439, 0.477, 0.527, 0.602, 0.833];

        public CustomModelParameters()
        {
            InitializeComponent();
        }

        public static readonly RoutedEvent UpdateDataEvent = EventManager.RegisterRoutedEvent("UpdateData_CustomModelParameters", RoutingStrategy.Bubble, typeof(RoutedEventHandler), typeof(CustomModelParameters));

        public event RoutedEventHandler UpdateData
        {
            add { AddHandler(UpdateDataEvent, value); }
            remove { RemoveHandler(UpdateDataEvent, value); }
        }

        public Params GetParams()
        {
            return new Params
            {
                vr = vrTable[(int)vrSlider.Value],
                vt = vtTable[(int)vtSlider.Value],
                vs = vsTable[(int)vsSlider.Value],
                vc = 1.0,//vcSlider.Value,
                rmax = rmaxSlider.Value,
                dx = dxSlider.Value,
                displayVectors = displayVectors.IsChecked ?? false,
                displayCurve = displaySolutionCurve.IsChecked ?? false,
                displayRmax = displayRmax.IsChecked ?? false
            };
        }

        private void paramSliderChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (vrLabel == null || vtLabel == null || vsLabel == null) return;
            if (vrSlider == null || vtSlider == null || vsSlider == null) return;

            vrLabel.Content = vrTable[(int)vrSlider.Value].ToString("N3");
            vtLabel.Content = vtTable[(int)vtSlider.Value].ToString("N3");
            vsLabel.Content = vsTable[(int)vsSlider.Value].ToString("N3");

            var parentWindow = Window.GetWindow(this);

            if (parentWindow == null) return;

            RaiseEvent(new RoutedEventArgs(UpdateDataEvent));
        }

        private void displayChecked(object sender, RoutedEventArgs e)
        {
            var parentWindow = Window.GetWindow(this);

            if (parentWindow == null) return;

            RaiseEvent(new RoutedEventArgs(UpdateDataEvent));
        }
    }
    
}
