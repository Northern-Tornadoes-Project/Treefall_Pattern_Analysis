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
using ArcGIS.Desktop.Mapping;
using ArcGIS.Core.Data.UtilityNetwork.Trace;
using System.Numerics;

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
        private ScottPlot.Plottable.FunctionPlot solutionFunc;

        public void RenderGraph()
        {
            try
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
                double[] fieldParams = new double[] { -1000.0, -1000.0, 1000.0, 1000.0, dx };
                double[] modelParams = new double[] { vrSlider.Value, vtSlider.Value, vsSlider.Value, rmaxSlider.Value, phiSlider.Value };
                int modelType = modelTypeListView.SelectedIndex;

                if (modelType < 0) return;

                PatternSolver.Field field = PatternSolver.getField(fieldParams, modelParams, modelType);
                /*int magDx = 20;

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
                }*/

                Heatmap hm = plt.AddHeatmap(field.magnitudes, Colormap.Jet);
                hm.Update(field.magnitudes, Colormap.Jet, 0.0, 120.0);
                hm.OffsetX = -1000.0;
                hm.OffsetY = -1000.0;
                hm.CellHeight = dx;
                hm.CellWidth = dx;
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
                    switch (modelType)
                    {
                        case 0:
                            var (oxs, oys) = PolarRankine(vrSlider.Value, vtSlider.Value, vsSlider.Value, vcSlider.Value, rmaxSlider.Value, phiSlider.Value, true);

                            outerSolutionPoly = plt.AddScatter(oxs, oys, System.Drawing.Color.White, 4, 1);

                            var (ixs, iys) = PolarRankine(vrSlider.Value, vtSlider.Value, vsSlider.Value, vcSlider.Value, rmaxSlider.Value, phiSlider.Value, false);

                            innerSolutionPoly = plt.AddScatter(ixs, iys, System.Drawing.Color.White, 4, 1);
                            break;

                        case 1:
                            solutionFunc = plt.AddFunction(new Func<double, double?>((x) => solveBakerSterling(x, new double[] { vrSlider.Value, vtSlider.Value, vsSlider.Value, vcSlider.Value, rmaxSlider.Value })),
                                                           System.Drawing.Color.White, 4, LineStyle.Solid);
                            break;
                    }
                }
                else
                {
                    plt.Remove(solutionFunc);
                    plt.Remove(innerSolutionPoly);
                    plt.Remove(outerSolutionPoly);
                }

                double scaleFactor = dx;

                var vf = plt.AddVectorField(field.unitVecs, field.xPositions, field.yPositions, null, System.Drawing.Color.Black, null, scaleFactor);
                vf.ScaledArrowheads = true;
                vf.ScaledArrowheadLength = 0.4;

                //plt.SetAxisLimits(-1000.0, 1000.0, -1000.0, 1000.0);
                plt.SetAxisLimits(al);
                graphPlot.Refresh();
            }
            catch (Exception ex)
            {
                MessageBox.Show("An unkown error has occurred\n\nFunction: RenderGraph\n\nError:\n\n" + ex.Message);
            }

        }

        public void RenderSolvedPattern()
        {
            try
            {
                if (patternGraphPlot == null) return;

                Plot plt = patternGraphPlot.Plot;
                var al = plt.GetAxisLimits();
                plt.Clear();

                PixelPadding padding = new PixelPadding(150f, 150f, 29f, 30f);
                plt.ManualDataArea(padding);

                plt.XAxis.SetZoomOutLimit(2000.0);
                plt.YAxis.SetZoomOutLimit(2000.0);
                plt.YAxis.SetBoundary(-1000.0, 1000.0);
                plt.XAxis.SetBoundary(-1000.0, 1000.0);

                double dx = dxSlider.Value;
                double[] fieldParams = new double[] { -1000.0, -1000.0, 1000.0, 1000.0, dx };
                double[] modelParams = new double[] { vrSlider.Value, vtSlider.Value, vsSlider.Value, rmaxSlider.Value, phiSlider.Value };
                int modelType = modelTypeListView.SelectedIndex;

                if (modelType < 0) return;

                PatternSolver.Field field = PatternSolver.getField(fieldParams, modelParams, modelType);

                Heatmap hm = plt.AddHeatmap(field.magnitudes, Colormap.Jet);
                hm.Update(field.magnitudes, Colormap.Jet, 0.0, 120.0);
                hm.OffsetX = -1000.0;
                hm.OffsetY = -1000.0;
                hm.CellHeight = dx;
                hm.CellWidth = dx;
                hm.Smooth = true;

                Colorbar colorbar = plt.AddColorbar(hm);
                colorbar.MinValue = 0.0;
                colorbar.MaxValue = 120.0;
                colorbar.Label = "Max Wind Velocity (m/s)";

                patternPlot.Plot.Clear();
                patternPlot.Plot.Style(ScottPlot.Style.Black);
                patternPlot.Plot.Grid(false);
                patternPlot.Plot.YAxis.Ticks(false);
                patternPlot.Plot.YAxis.Line(false);
                patternPlot.Plot.YAxis2.Line(false);

                if ((bool)displayRmax.IsChecked)
                {
                    rmaxCircle = plt.AddCircle(0, 0, rmaxSlider.Value, System.Drawing.Color.Black, 4);
                }
                else
                {
                    plt.Remove(rmaxCircle);
                }

                if (vcSlider.Value < vsSlider.Value + Math.Sqrt(vrSlider.Value * vrSlider.Value + vtSlider.Value * vtSlider.Value))
                {
                    if ((bool)displaySolutionCurve.IsChecked)
                    {

                        switch (modelType)
                        {
                            case 0:
                                var (oxs, oys) = PolarRankine(vrSlider.Value, vtSlider.Value, vsSlider.Value, vcSlider.Value, rmaxSlider.Value, phiSlider.Value, true);

                                outerSolutionPoly = plt.AddScatter(oxs, oys, System.Drawing.Color.White, 4, 1);

                                var (ixs, iys) = PolarRankine(vrSlider.Value, vtSlider.Value, vsSlider.Value, vcSlider.Value, rmaxSlider.Value, phiSlider.Value, false);

                                innerSolutionPoly = plt.AddScatter(ixs, iys, System.Drawing.Color.White, 4, 1);
                                break;

                            case 1:
                                solutionFunc = plt.AddFunction(new Func<double, double?>((x) => solveBakerSterling(x, new double[] { vrSlider.Value, vtSlider.Value, vsSlider.Value, vcSlider.Value, rmaxSlider.Value })),
                                                                System.Drawing.Color.White, 4, LineStyle.Solid);
                                break;
                        }

                    }
                    else
                    {
                        plt.Remove(solutionFunc);
                        plt.Remove(innerSolutionPoly);
                        plt.Remove(outerSolutionPoly);
                    }

                    var pattern = PatternSolver.getPattern(new double[] { vtSlider.Value, vrSlider.Value, vsSlider.Value, vcSlider.Value, rmaxSlider.Value, phiSlider.Value }, modelType, dx, false);

                    var vf = plt.AddVectorFieldList();
                    vf.Color = System.Drawing.Color.Black;
                    vf.ArrowStyle.ScaledArrowheads = true;
                    vf.ArrowStyle.ScaledArrowheadLength = 0.4;

                    foreach (var p in pattern)
                    {
                        //System.Diagnostics.Debug.WriteLine(p[0] + " " + p[1] + " " + p[2] + " " + p[3]);
                        vf.RootedVectors.Add((new Coordinate(p[0], p[1]), new CoordinateVector(p[2] * dx, p[3] * dx)));
                    }

                    var pvf = patternPlot.Plot.AddVectorFieldList();
                    pvf.Color = System.Drawing.Color.White;
                    pvf.ArrowStyle.ScaledArrowheads = true;
                    pvf.ArrowStyle.ScaledArrowheadLength = 0.4;

                    foreach (var p in pattern)
                    {
                        pvf.RootedVectors.Add((new Coordinate(p[0], 0), new CoordinateVector(p[2] * dx, p[3] * dx)));
                    }
                }

                plt.SetAxisLimits(al);
                patternGraphPlot.Refresh();
                patternPlot.Refresh();
            }
            catch (Exception ex)
            {
                MessageBox.Show("An unkown error has occurred\n\nFunction: RenderSolvedPattern\n\nError:\n\n" + ex.Message);
            }

        }


        List<ModelProfile> profilePlots = new();

        public void RenderProfiles()
        {
            try
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

                foreach (var p in profilePlots)
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
            catch (Exception ex)
            {
                MessageBox.Show("An unkown error has occurred\n\nFunction: RenderProfiles\n\nError:\n\n" + ex.Message);
            }

        }

        public double[] generateProfileArgs(string profileType)
        {
            double[] args = Array.Empty<double>();


            //System.Diagnostics.Debug.WriteLine("************" + profileType + "*****************");

            try
            {
                args = profileType.ToLower() switch
                {
                    "vr" => new double[] { vrSlider.Value, phiSlider.Value },
                    "vt" => new double[] { vtSlider.Value, phiSlider.Value },
                    "vs" => new double[] { vsSlider.Value },
                    "vc" => new double[] { vcSlider.Value },
                    _ => Array.Empty<double>()
                };
            }
            catch (Exception ex)
            {
                MessageBox.Show("An unkown error has occurred\n\nFunction: generateProfileArgs\n\nError:\n\n" + ex.Message);
            }

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
            List<double> xs = new List<double>();
            List<double> ys = new List<double>();
            try
            {
                int n = 500;
                
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

                    if ((!outer && r <= R) || (outer && r > R))
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
            }
            catch (Exception ex)
            {
                MessageBox.Show("An unkown error has occurred\n\nFunction: PolarRankine\n\nError:\n\n" + ex.Message);
            }

            return (xs.ToArray(), ys.ToArray());
        }

        public double solveBakerSterling(double x, double[] modelParams)
        {
            try
            {
                double Vr = modelParams[0];
                double Vt = modelParams[1];
                double Vs = modelParams[2];
                double Vc = modelParams[3];
                double Rmax = modelParams[4];

                double x2 = x * x;
                double Vr2 = Vr * Vr;
                double Vt2 = Vt * Vt;
                double Vs2 = Vs * Vs;
                double Vc2 = Vc * Vc;
                double Rmax2 = Rmax * Rmax;

                double q = 2.0 * (Vr2 + Vt2) + Vs2;
                double d = Rmax2 + x2;
                double k = Rmax * Vs;
                double kt = k * Vt;
                double kr = k * Vr;

                double b1 = x2 * Vs2;
                double b2 = d * Vc2;
                double b3 = 2.0 * q * Rmax2;
                double b4 = 4.0 * kt * x;

                double a4_1 = 1.0 / (Vs2 - Vc2);
                double a3 = -4.0 * kr;
                double a2 = 2.0 * (b1 - b2) + b3 + b4;
                double a1 = d * a3;
                double a0 = x2 * (b3 + b1) + Vs2 * Rmax2 * Rmax2 + d * (b4 - b2);

                Complex[] solutions = Quartic.solve_quartic(a3 * a4_1, a2 * a4_1, a1 * a4_1, a0 * a4_1);

                double y = double.MinValue;

                y = Math.Abs(solutions[0].Imaginary) < 1e-7 ? Math.Max(y, solutions[0].Real) : y;
                y = Math.Abs(solutions[1].Imaginary) < 1e-7 ? Math.Max(y, solutions[1].Real) : y;
                y = Math.Abs(solutions[2].Imaginary) < 1e-7 ? Math.Max(y, solutions[2].Real) : y;
                y = Math.Abs(solutions[3].Imaginary) < 1e-7 ? Math.Max(y, solutions[3].Real) : y;
                y = y == double.MinValue ? double.NaN : y;

                return y;
            }
            catch (Exception ex)
            {
                MessageBox.Show("An unkown error has occurred\n\nFunction: solveBakerSterling\n\nError:\n\n" + ex.Message);
                return double.NaN;
            }
        }

        private void paramSliderChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            reRender();
        }

        private void displayChecked(object sender, RoutedEventArgs e)
        {
            reRender();
        }

        private void addNewProfile(object sender, RoutedEventArgs e)
        {
            try
            {
                if (profileModelSelection.SelectedItem == null ||
                    profileTypeSelection.SelectedItem == null ||
                    profileStyleSelection.SelectedItem == null) return;

                string modelType = ((ComboBoxItem)profileModelSelection.SelectedItem).Content.ToString();
                string profileType = ((ComboBoxItem)profileTypeSelection.SelectedItem).Content.ToString();
                string profileStyle = ((ComboBoxItem)profileStyleSelection.SelectedItem).Content.ToString();
                //double[] args = generateProfileArgs(profileType);

                graphedProfilesListBox.Items.Add(modelType + " - " + profileType);

                profilePlots.Add(new ModelProfile(modelType, profileType, profileStyle, null));

                graphedProfilesListBox.SelectedIndex = graphedProfilesListBox.Items.Count - 1;

                RenderProfiles();
            }
            catch (Exception ex)
            {
                MessageBox.Show("An unkown error has occurred\n\nFunction: addNewProfile\n\nError:\n\n" + ex.Message);
            }

        }

        private void deleteSelectedProfile(object sender, RoutedEventArgs e)
        {
            try
            {
                int idx = graphedProfilesListBox.SelectedIndex;

                if (idx == -1) return;

                profilePlot.Plot.Remove(profilePlots[idx].plot);

                profilePlots.RemoveAt(idx);

                graphedProfilesListBox.Items.RemoveAt(idx);

                profilePlot.Refresh();
            }
            catch (Exception ex)
            {
                MessageBox.Show("An unkown error has occurred\n\nFunction: deleteSelectedProfile\n\nError:\n\n" + ex.Message);
            }
        }

        private void modelTypeChanged(object sender, SelectionChangedEventArgs e)
        {
            reRender();
        }

        private void reRender()
        {
            try
            {
                if (utilityTab == null || utilityTab.SelectedItem == null) return;

                String selectedTabHeader = (string)((TabItem)utilityTab.SelectedItem).Header;

                switch (selectedTabHeader)
                {
                    case "Model Grapher":
                        RenderGraph();
                        break;

                    case "Pattern Solver":
                        RenderSolvedPattern();
                        break;

                    case "Simulator":
                        break;

                    case "Model Profiles":
                        RenderProfiles();
                        break;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("An unkown error has occurred\n\nFunction: reRander\n\nError:\n\n" + ex.Message);
            }
        }

        private void utilityTabChanged(object sender, SelectionChangedEventArgs e)
        {
            reRender();
        }
    }
    
}
