using ScottPlot.Plottable;
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
using ScottPlot.Drawing;
using ScottPlot.Statistics;


namespace TreefallPatternAnalysis
{
    /// <summary>
    /// Interaction logic for ModelAnalysis.xaml
    /// </summary>
    public partial class ModelAnalysis : ArcGIS.Desktop.Framework.Controls.ProWindow
    {
        public ModelAnalysis()
        {
            InitializeComponent();
            RenderGraph();
        }

        public void RenderGraph()
        {
            if (graphPlot == null) return;

            Plot plt = graphPlot.Plot;
            plt.Clear();

            PixelPadding padding = new PixelPadding(150f, 150f, 29f, 30f);
            plt.ManualDataArea(padding);

            plt.XAxis.SetZoomOutLimit(2000.0);
            plt.YAxis.SetZoomOutLimit(2000.0);
            plt.YAxis.SetBoundary(-1000.0, 1000.0);
            plt.XAxis.SetBoundary(-1000.0, 1000.0);

            double dx = dxSlider.Value;
            int magDx = 25;

            double[,] magnitudes = new double[2000 / magDx, 2000 / magDx];
            Vector2[,] unitVecs = new Vector2[(int)Math.Ceiling(2000.0 / dx), (int)Math.Ceiling(2000.0 / dx)];
            double[] xPositions = DataGen.Range(-1000.0 + dx / 2.0, 1000.0 + dx / 2.0, dx);
            double[] yPositions = DataGen.Range(-1000.0 + dx / 2.0, 1000.0 + dx / 2.0, dx);

            for (int x2 = 0; x2 < 2000 / magDx; x2++)
            {
                for (int y = 0; y < 2000 / magDx; y++)
                {
                    double[] vec = compute_rankine_unit(x2 * magDx - 1000.0, y * magDx - 1000.0, vrSlider.Value, vtSlider.Value, vsSlider.Value, rmaxSlider.Value, phiSlider.Value);
                    magnitudes[2000 / magDx - y - 1, x2] = vec[2];
                }
            }

            for (int x = 0; x < (int)Math.Ceiling(2000.0 / dx); x++)
            {
                for (int y2 = 0; y2 < (int)Math.Ceiling(2000.0 / dx); y2++)
                {
                    double[] vec2 = compute_rankine_unit(x * dx - 1000.0 + dx / 2.0, y2 * dx - 1000.0 + dx / 2.0, vrSlider.Value, vtSlider.Value, vsSlider.Value, rmaxSlider.Value, phiSlider.Value);
                    unitVecs[x, y2] = new Vector2(vec2[0], vec2[1]);
                }
            }

            Heatmap hm = plt.AddHeatmap(magnitudes, Colormap.Jet);
            hm.Update(magnitudes, Colormap.Jet, 0.0, 120.0);
            hm.OffsetX = -1000.0;
            hm.OffsetY = -1000.0;
            hm.CellHeight = magDx;
            hm.CellWidth = magDx;
            hm.Smooth = true;

            Colorbar colorbar = plt.AddColorbar(hm);
            colorbar.MinValue = 0.0;
            colorbar.MaxValue = 120.0;
            colorbar.Label = "Max Wind Velocity (m/s)";

            double scaleFactor = dx;

            plt.AddVectorField(unitVecs, xPositions, yPositions, null, System.Drawing.Color.Black, null, scaleFactor).ScaledArrowheads = true;

            plt.SetAxisLimits(-1000.0, 1000.0, -1000.0, 1000.0);
            graphPlot.Refresh();
        }

        public double hypot(double x, double y)
        {
            return Math.Sqrt(x * x + y * y);
        }

        public double[] compute_rankine(double x, double y, double Vr, double Vt, double Vs, double Rmax, double Phi)
        {
            if (-0.01 < x && x < 0.01 && -0.01 < y && y < 0.01) return new double[2];

            double r = hypot(x, y); 

            double[] radial_unit_vec = new double[2] { -x / r, -y / r };
            double[] tangential_unit_vec = new double[2] { radial_unit_vec[1], -radial_unit_vec[0] };

            double scale_factor = ((r <= Rmax) ? Math.Pow(r / Rmax, Phi) : Math.Pow(Rmax / r, Phi));

            double Vtan = Vt * scale_factor;
            double Vrad = Vr * scale_factor;

            double[] tangential_vec = new double[2] { tangential_unit_vec[0] * Vtan, tangential_unit_vec[1] * Vtan };
            double[] radial_vec = new double[2] { radial_unit_vec[0] * Vrad, radial_unit_vec[1] * Vrad };

            return new double[2] { tangential_vec[0] + radial_vec[0], tangential_vec[1] + radial_vec[1] + Vs };
        }

        public double[] compute_rankine_unit(double x, double y, double Vr, double Vt, double Vs, double Rmax, double Phi)
        {
            double[] vec = compute_rankine(x, y, Vr, Vt, Vs, Rmax, Phi);

            if (vec[0] == 0.0 && vec[1] == 0.0) return new double[3];

            double mag = hypot(vec[0], vec[1]);

            return new double[3] { vec[0] / mag, vec[1] / mag, mag };
        }

        private void paramSliderChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            RenderGraph();
        }
    }
}
