using ScottPlot;
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
    /// Interaction logic for PatternPlot.xaml
    /// </summary>
    public partial class PatternPlot : UserControl
    {
        public PatternPlot()
        {
            InitializeComponent();
        }

        private double lastRunSpacing = 20;

        public void Update(List<double[]> pattern, double dx)
        {
            lastRunSpacing = dx;
            var plt = patternPlot.Plot;
            plt.Clear();

            var vf = plt.AddVectorFieldList();
            vf.Color = System.Drawing.Color.Black;
            vf.ArrowStyle.LineWidth = 2;
            vf.ArrowStyle.ScaledArrowheads = true;
            vf.ArrowStyle.ScaledArrowheadLength = 0.4;

            foreach (var p in pattern)
            {
                vf.RootedVectors.Add((new Coordinate(0.0, p[1]), new CoordinateVector(p[2] * dx, p[3] * dx)));
            }

            RescalePatternPlot(patternPlot, null);
            patternPlot.Refresh();
        }

        private void RescalePatternPlot(object sender, MouseButtonEventArgs e)
        {
            try
            {
                if (e == null || e.MiddleButton == MouseButtonState.Pressed)
                {
                    var plt = (WpfPlot)sender;

                    if (plt == null) return;

                    double spacing = lastRunSpacing;

                    plt.Plot.AxisAuto();
                    var a = plt.Plot.GetAxisLimits();
                    var w = plt.Plot.Width;
                    var h = plt.Plot.Height;

                    var dx = (a.YMax - a.YMin) * 0.226;

                    plt.Plot.SetAxisLimitsY(a.YMin + spacing, a.YMax + spacing);
                    plt.Plot.SetAxisLimitsX(-dx / 2, dx / 2);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("An unkown error has occurred\n\nFunction: rescaleMatchedPatternPlot\n\nError:\n\n" + ex.Message);
            }
        }
    }
}
