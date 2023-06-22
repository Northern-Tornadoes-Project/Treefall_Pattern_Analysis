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

            //graphPlot.Plot.GetSettings().ZoomRectangle.Clear();
            var x = graphPlot.Plot.GetSettings();

            x.AxisAutoUnsetAxes();
            x.AxisAutoAll();
        }

        private ScottPlot.Plottable.Ellipse rmaxCircle;
        private ScottPlot.Plottable.ScatterPlot innerSolutionPoly;
        private ScottPlot.Plottable.ScatterPlot outerSolutionPoly;

        public void RenderGraph()
        {
            if (graphPlot == null) return;

            Plot plt = graphPlot.Plot;
            var al = plt.GetAxisLimits();
            plt.Clear();

            PixelPadding padding = new PixelPadding(150f, 150f, 29f, 30f);
            plt.ManualDataArea(padding);

            plt.XAxis.SetZoomOutLimit(2000.0);
            plt.YAxis.SetZoomOutLimit(2000.0);
            plt.YAxis.SetBoundary(-1000.0, 1000.0);
            plt.XAxis.SetBoundary(-1000.0, 1000.0);

            double dx = dxSlider.Value;
            int magDx = 20;

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

            if ((bool)displayRmax.IsChecked)
            {
                rmaxCircle = plt.AddCircle(0, 0, rmaxSlider.Value, System.Drawing.Color.Black, 4);
            }
            else
            {
                plt.Remove(rmaxCircle);
            }
            if ((bool)displaySolutionCurve.IsChecked)
            {
                var (oxs, oys) = PolarRankine(vrSlider.Value, vtSlider.Value, vsSlider.Value, vcSlider.Value, rmaxSlider.Value, phiSlider.Value, true);

                outerSolutionPoly = plt.AddScatter(oxs, oys, System.Drawing.Color.White, 4, 1);

                var (ixs, iys) = PolarRankine(vrSlider.Value, vtSlider.Value, vsSlider.Value, vcSlider.Value, rmaxSlider.Value, phiSlider.Value, false);

                innerSolutionPoly = plt.AddScatter(ixs, iys, System.Drawing.Color.White, 4, 1);
            }
            else
            {
                plt.Remove(innerSolutionPoly);
                plt.Remove(outerSolutionPoly);
            }

            double scaleFactor = dx;

            var vf = plt.AddVectorField(unitVecs, xPositions, yPositions, null, System.Drawing.Color.Black, null, scaleFactor);
            vf.ScaledArrowheads = true;
            vf.ScaledArrowheadLength = 0.4;

            //plt.SetAxisLimits(-1000.0, 1000.0, -1000.0, 1000.0);
            plt.SetAxisLimits(al);
            graphPlot.Refresh();
        }

        List<ModelProfile> profilePlots = new();

        public void RenderProfiles()
        {
            var plt = profilePlot.Plot;

            plt.Clear();

            int idx = graphedProfilesListBox.SelectedIndex;

            if (idx > -1)
            {

                ModelProfile selectedProfile = profilePlots[idx];

                double[] args = generateProfileArgs(selectedProfile.profileType);

                selectedProfile.setPlotArgs(args);
            }

            foreach(var p in profilePlots)
            {
                p.addFuncPlot(plt);
            }

            
            plt.SetAxisLimits(-0.25, 10.0, -5.0, 100.0);
            plt.Title("Profiles");
            plt.XLabel("r/Rmax");
            plt.YLabel("Wind Velocity (m/s)");
            plt.Legend(location: Alignment.UpperRight);
            profilePlot.Refresh();

        }

        public double[] generateProfileArgs(string profileType)
        {
            double[] args;


            System.Diagnostics.Debug.WriteLine("************" + profileType + "*****************");

            args = profileType.ToLower() switch
            {
                "vr" => new double[] { vrSlider.Value , phiSlider.Value},
                "vt" => new double[] { vtSlider.Value, phiSlider.Value },
                "vs" => new double[] { vsSlider.Value },
                "vc" => new double[] { vcSlider.Value }
            };

            return args;
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

        public (double[], double[]) PolarRankine(double a, double t, double s, double c, double R, double p, bool outer)
        {
            int n = 500;
            List<double> xs = new List<double>();
            List<double> ys = new List<double>();
            List<double> tempxs = new List<double>();
            List<double> tempys = new List<double>();
            bool outOfBounds = false;

            double sqrt2 = Math.Sqrt(2);
            double a2 = a * a;
            double t2 = t * t;
            double s2 = s * s;
            double c2 = c * c;
            double a2t2 = a2 + t2;
            double d = outer ? 1 / (c2 - s2) : 1 / a2t2;

            double k1 = Math.Pow(2, -1 / p) * R;
            double k2 = outer ? 2 * s * t : -2 * s * t;
            double k3 = outer ? -2 * s * a : 2 * s * a;
            double k4 = (2 * c2 - s2) * a2t2;
            double k5 = -s2 * (a2 - t2);
            double k6 = -2 * a * s2 * t;


            for (int i = 0; i < n; i++)
            {
                double sin = Math.Sin((i * 2 * Math.PI) / n);
                double cos = Math.Cos((i * 2 * Math.PI) / n);
                double sin2 = 2 * sin * cos;
                double cos2 = cos * cos - sin * sin;

                double r = k1 * Math.Pow((k2 * cos + k3 * sin + sqrt2 * Math.Sqrt(k4 + k5 * cos2 + k6 * sin2)) * d, 1 / p);

                if((!outer && r <= R) || (outer && r > R))
                {
                    outOfBounds = false;
                    xs.Add(r * cos);
                    ys.Add(r * sin);
                }
                else if (outOfBounds == false)
                {
                    outOfBounds = true;
                    tempxs.AddRange(xs);
                    tempys.AddRange(ys);
                    xs.Clear();
                    ys.Clear();
                }
                
            }

            xs.AddRange(tempxs); 
            ys.AddRange(tempys);

            //xs.Add(xs[0]);
            //ys.Add(ys[0]);

            return (xs.ToArray(), ys.ToArray());
        }

        private void paramSliderChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (utilityTab == null || utilityTab.SelectedItem == null) return;

            String selectedTabHeader = (string)((TabItem)utilityTab.SelectedItem).Header;

            switch (selectedTabHeader)
            {
                case "Model Grapher":
                    RenderGraph();
                    break;

                case "Pattern Solver":
                    break;

                case "Simulator":
                    break;

                case "Model Profiles":
                    RenderProfiles();
                    break;
            }
            
            
        }

        private void displayChecked(object sender, RoutedEventArgs e)
        {
            if (utilityTab == null || utilityTab.SelectedItem == null) return;

            String selectedTabHeader = (string)((TabItem)utilityTab.SelectedItem).Header;

            switch (selectedTabHeader)
            {
                case "Model Grapher":
                    RenderGraph();
                    break;

                case "Pattern Solver":
                    break;

                case "Simulator":
                    break;

                case "Model Profiles":
                    break;
            }
        }

        private void addNewProfile(object sender, RoutedEventArgs e)
        {
            if (profileModelSelection.SelectedItem == null ||
                profileTypeSelection.SelectedItem  == null ||
                profileStyleSelection.SelectedItem == null)   return;

            string modelType = ((ComboBoxItem)profileModelSelection.SelectedItem).Content.ToString();
            string profileType = ((ComboBoxItem)profileTypeSelection.SelectedItem).Content.ToString();
            string profileStyle = ((ComboBoxItem)profileStyleSelection.SelectedItem).Content.ToString();
            //double[] args = generateProfileArgs(profileType);

            graphedProfilesListBox.Items.Add(modelType + " - " + profileType);

            profilePlots.Add(new ModelProfile(modelType, profileType, profileStyle, null));

            graphedProfilesListBox.SelectedIndex = graphedProfilesListBox.Items.Count - 1;

            RenderProfiles();

        }

        private void deleteSelectedProfile(object sender, RoutedEventArgs e)
        {
            int idx = graphedProfilesListBox.SelectedIndex;

            if (idx == -1) return;

            profilePlot.Plot.Remove(profilePlots[idx].plot);

            profilePlots.RemoveAt(idx);

            graphedProfilesListBox.Items.RemoveAt(idx);

            profilePlot.Refresh();
        }
    }
}
