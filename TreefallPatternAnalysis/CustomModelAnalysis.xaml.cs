using ArcGIS.Core.Internal.CIM;
using ScottPlot;
using ScottPlot.Drawing;
using ScottPlot.Plottable;
using System;
using System.Collections.Generic;
using System.Diagnostics;
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
    /// Interaction logic for CustomModelAnalysis.xaml
    /// </summary>
    public partial class CustomModelAnalysis : ArcGIS.Desktop.Framework.Controls.ProWindow
    {
        public CustomModelAnalysis()
        {
            InitializeComponent();

            customModelParameters.UpdateData += UpdateGraph;
            vrLpGraph.UpdateData += UpdateGraph;
            vtLpGraph.UpdateData += UpdateGraph;
            vtLpGraph.color = System.Drawing.Color.Green;
            vtLpGraph.UpdateSpline();

            UpdateGraph(null, null);
        }

        private bool debounce = false;

        private void KeyPressed(object sender, KeyEventArgs e)
        {
            if (debounce) return;

            vrLpGraph.lastKey = e.Key.ToString().ToLower()[0];
            vtLpGraph.lastKey = e.Key.ToString().ToLower()[0];
            debounce = true;
        }

        private void KeyReleased(object sender, KeyEventArgs e)
        {
            vrLpGraph.lastKey = '\0';
            vtLpGraph.lastKey = '\0';
            debounce = false;
        }

        private void UpdateGraph(object sender, RoutedEventArgs e)
        {
            if (graphPlot == null) return;

            var al = resetFieldGraph();

            renderFieldGraph();

            graphPlot.Plot.SetAxisLimits(al);
            graphPlot.Refresh();
        }

        private void renderFieldGraph()
        {
            var modelParams = customModelParameters.GetParams();
            var vrLines = vrLpGraph.GetLines();
            var vtLines = vtLpGraph.GetLines();
            //lines[2] = 1e-4;

            double dx = modelParams.dx;

            PatternSolver.Field field = PatternSolver.getFieldLP([-1000.0, -1000.0, 1000.0, 1000.0, dx],
                                                               [modelParams.vr, modelParams.vt, modelParams.vs, modelParams.rmax, 1.0], vrLines, vtLines);

            Plot plt = graphPlot.Plot;

            Heatmap hm = plt.AddHeatmap(field.magnitudes, Colormap.Jet);
            hm.Update(field.magnitudes, Colormap.Jet, 0.0, 2.75); //120.0
            hm.OffsetX = -1000.0;
            hm.OffsetY = -1000.0;
            hm.CellHeight = dx;
            hm.CellWidth = dx;
            hm.Smooth = true;

            Colorbar colorbar = plt.AddColorbar(hm);
            colorbar.MinValue = 0.0;
            colorbar.MaxValue = 2.75; //120.0
            colorbar.Label = "Wind Velocity (ratio to Vc)";

            if (modelParams.displayRmax)
            {
                plt.AddCircle(0, 0, modelParams.rmax, System.Drawing.Color.Black, 4);
            }

            if (modelParams.displayCurve)
            {
                var (xs, ys) = PatternSolver.getCurveLP(500, [modelParams.vr, modelParams.vt, modelParams.vs, modelParams.vc, modelParams.rmax, 1.0], vrLines, vtLines);

                plt.AddScatter(xs, ys, System.Drawing.Color.White, 4, 1);
            }

            if (modelParams.displayVectors)
            {
                var vf = plt.AddVectorField(field.unitVecs, field.xPositions, field.yPositions, null, System.Drawing.Color.Black, null, dx);
                vf.ScaledArrowheads = true;
                vf.ScaledArrowheadLength = 0.4;
            }

            var (pattern, w) = PatternSolver.getPatternLP([modelParams.vr, modelParams.vt, modelParams.vs, modelParams.vc, modelParams.rmax, 1.0], vrLines, vtLines, -16, true);

            patternPlot.Update(pattern, w / 16.0);
        }

        private AxisLimits resetFieldGraph()
        {
            Plot plt = graphPlot.Plot;
            AxisLimits al = plt.GetAxisLimits();
            plt.Clear();

            PixelPadding padding = new PixelPadding(120f, 170f, 20f, 10f);
            plt.ManualDataArea(padding);

            plt.XAxis.SetZoomOutLimit(2000.0);
            plt.YAxis.SetZoomOutLimit(2000.0);
            plt.YAxis.SetBoundary(-1000.0, 1000.0);
            plt.XAxis.SetBoundary(-1000.0, 1000.0);

            return al;
        }
    }

}
