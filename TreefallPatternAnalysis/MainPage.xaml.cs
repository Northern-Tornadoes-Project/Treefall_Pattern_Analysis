using ArcGIS.Desktop.Mapping;
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
using ArcGIS.Core.CIM;
using ArcGIS.Desktop.Mapping;
using ArcGIS.Desktop.Framework.Threading.Tasks;
using ArcGIS.Core.Internal.CIM;
using ArcGIS.Core.Data;
using ArcGIS.Core.Geometry;
using ScottPlot;
using System.Drawing;
using System.Security.Cryptography.Xml;
using System.Runtime.Intrinsics.X86;
using System.Globalization;
using ArcGIS.Desktop.Internal.Mapping;
using System.Configuration;
using System.Runtime.InteropServices;
using ActiproSoftware.Products.Ribbon;
using System.Threading;
using System.Text.RegularExpressions;
using Syncfusion.XlsIO.Implementation;
using System.Data;
using System.Reflection;
using System.Windows.Automation;
using System.IO;
using ArcGIS.Core.Data.UtilityNetwork.Trace;
using Microsoft.Office.Core;
using ArcGIS.Desktop.Core;
using Syncfusion.Windows.Shared;
using ArcGIS.Core.Internal;
using Excel = Microsoft.Office.Interop.Excel;

namespace TreefallPatternAnalysis
{
    /// <summary>
    /// Interaction logic for MainPage.xaml
    /// </summary>
    public partial class MainPage : ArcGIS.Desktop.Framework.Controls.ProWindow
    {

        //current arcgis map
        private ArcGIS.Desktop.Mapping.Map map;
        //all feature layers on map
        private System.Collections.Generic.IEnumerable<FeatureLayer> fLayers;
        FeatureLayer selectedConvergence = null;
        FeatureLayer selectedVector = null;

        private List<Transect> transects = new List<Transect>();

        private List<(double, double, double, double)> overviewVectors = new List<(double, double, double, double)>();
        private List<(double, double, double, double)> transectVectors = new List<(double, double, double, double)>();

        private double[] convergenceLineX = Array.Empty<double>();
        private double[] convergenceLineY = Array.Empty<double>();
        private double[] convergenceLineDists = Array.Empty<double>();
        //private List<double> patternVecs = new List<double>();
        private int centerIdx = 0;
        private double lastRunSpacing = 40.0;

        private Dictionary<Slider, TextBox> sliderTextDict = new Dictionary<Slider, TextBox>();

        public MainPage()
        {
            try
            {
                InitializeComponent();

                //get current map
                map = MapView.Active.Map;

                //get all raster layers on map
                fLayers = map.GetLayersAsFlattenedList().OfType<FeatureLayer>();

                //add raster names to windows selection box
                foreach (var layer in fLayers)
                {
                    VectorsSelectionBox.Items.Add(layer.Name);
                    ConvergenceSelectionBox.Items.Add(layer.Name);

                }

                sliderTextDict.Add(transectPositionSlider, transectPositionBox);
                sliderTextDict.Add(transectHeightOffsetSlider, transectHeightOffsetBox);
                sliderTextDict.Add(transectAngleOffsetSlider, transectAngleOffsetBox);
                sliderTextDict.Add(transectLengthAboveSlider, transectLengthAboveBox);
                sliderTextDict.Add(transectLengthBelowSlider, transectLengthBelowBox);
                sliderTextDict.Add(transectWidthSlider, transectWidthBox);
            }
            catch (Exception ex)
            {
                MessageBox.Show("An error occurred while initializing the application\n\nError:\n\n" + ex.Message);
            }

            /*double[] p = { -0.882947593f, -0.469471563f,  -0.891006524f, -0.4539905f,  -0.866025404f, -0.5f,  -0.121869343f, -0.992546152f,  -0.087155743f, -0.996194698f,  0.838670568f, -0.544639035f,  0.992546152f, 0.121869343f,  0.809016994f, 0.587785252f,  0.838670568f, 0.544639035f,  0.819152044f, 0.573576436f,  0.838670568f, 0.544639035f,  0.951056516f, 0.309016994f,  0.891006524f, 0.4539905f,  0.838670568f, 0.544639035f,  0.529919264f, 0.848048096f,  0.64278761f, 0.766044443f };

            var matches = PatternSolver.solveBestMatches(p, 40.0f, 360.0f, 240.0f);

            var bestMatch = PatternSolver.getPattern(new double[] { matches[0][1], matches[0][2], matches[0][3], matches[0][4], matches[0][5] }, 40.0f, new double[] { 0.819152044f, 0.573576436f });

            System.Diagnostics.Debug.WriteLine("****************" + bestMatch.Count() + "****************");*/

        }

        private async void inputSelectionNextButton(object sender, RoutedEventArgs e)
        {
            try
            {
                if (VectorsSelectionBox.SelectedItem == null || ConvergenceSelectionBox.SelectedItem == null) return;

                string vecName = (string)VectorsSelectionBox.SelectedItem;
                string covName = (string)ConvergenceSelectionBox.SelectedItem;

                foreach (var layer in fLayers)
                {
                    if (layer.Name.Equals(vecName))
                    {
                        selectedVector = layer;
                    }
                    else if (layer.Name.Equals(covName))
                    {
                        selectedConvergence = layer;
                    }
                }

                if (selectedVector == null || selectedConvergence == null) return;

                await QueuedTask.Run(() =>
                {
                    //var table = selectedConvergence.GetTable();

                    /*ArcGIS.Core.Data.QueryFilter queryFilter = new ArcGIS.Core.Data.QueryFilter
                    {
                        WhereClause = "OBJECTID = 1"
                    };

                    var selection = selectedConvergence.Select(queryFilter);*/


                    using (ArcGIS.Core.Data.Table shp_table = selectedVector.GetTable())
                    {
                        using (RowCursor rowCursor = shp_table.Search())
                        {
                            overviewVectors.Clear();
                            while (rowCursor.MoveNext())
                            {
                                using (Feature f = (Feature)rowCursor.Current)
                                {
                                    ArcGIS.Core.Geometry.Polyline pl = (ArcGIS.Core.Geometry.Polyline)f.GetShape();
                                    overviewVectors.Add((pl.Points[0].X, pl.Points[0].Y, pl.Points[1].X, pl.Points[1].Y));
                                }
                            }
                        }
                    }

                    using (ArcGIS.Core.Data.Table shp_table = selectedConvergence.GetTable())
                    {
                        using (RowCursor rowCursor = shp_table.Search())
                        {
                            while (rowCursor.MoveNext())
                            {
                                using (Feature f = (Feature)rowCursor.Current)
                                {
                                    ArcGIS.Core.Geometry.Polyline pl = (ArcGIS.Core.Geometry.Polyline)f.GetShape();

                                    convergenceLineX = new double[pl.Points.Count];
                                    convergenceLineY = new double[pl.Points.Count];
                                    convergenceLineDists = new double[pl.Points.Count];
                                    convergenceLineDists[0] = 0;

                                    for (int i = 0; i < pl.Points.Count; i++)
                                    {
                                        convergenceLineX[i] = pl.Points[i].X;
                                        convergenceLineY[i] = pl.Points[i].Y;
                                    }

                                    for (int i = 1; i < pl.Points.Count; i++)
                                    {
                                        double dx = (pl.Points[i].X - pl.Points[i - 1].X);
                                        double dy = (pl.Points[i].Y - pl.Points[i - 1].Y);

                                        convergenceLineDists[i] = convergenceLineDists[i - 1] + Math.Sqrt(dx * dx + dy * dy);
                                    }
                                }
                            }
                        }
                    }


                });

                transectPositionSlider.Maximum = convergenceLineDists[convergenceLineDists.Length - 1];
                transectPositionSlider.TickFrequency = convergenceLineDists[convergenceLineDists.Length - 1] / 100;

                headerTabs.SelectedIndex = 1;

                reDrawTransectOverview();

                transectsOverviewPlot.Configuration.MiddleClickAutoAxis = false;
            }
            catch (Exception ex)
            {
                MessageBox.Show("Failed to load specified Files, the files must be in a\n" +
                                "polyline shapefile where each vector is a polyline, and the\n" +
                                "convergence line must be a single polyline\n\nError:\n\n" + ex.Message);
            }

        }

        private void reDrawTransectOverview()
        {
            try
            {
                transectsOverviewPlot.Plot.Clear();
                var field = transectsOverviewPlot.Plot.AddVectorFieldList();
                field.Color = System.Drawing.Color.Black;
                field.ArrowStyle.ScaledArrowheads = true;
                foreach (var v in overviewVectors)
                {
                    field.RootedVectors.Add((new Coordinate((v.Item1 + v.Item3) / 2.0, (v.Item2 + v.Item4) / 2.0), new CoordinateVector(v.Item3 - v.Item1, v.Item4 - v.Item2)));
                }

                transectsOverviewPlot.Plot.AddScatterLines(convergenceLineX, convergenceLineY, lineWidth: 3);

                foreach (var t in transects)
                {
                    t.renderTransect(transectsOverviewPlot.Plot);
                }

                transectsOverviewPlot.Plot.Layout(left: 0, right: 0, bottom: 0, top: 0);
                rescalePlot(transectsOverviewPlot, null);
                transectsOverviewPlot.Refresh();
            }
            catch (Exception ex)
            {
                MessageBox.Show("An unkown error has occurred\n\nFunction: reDrawTransectOverview\n\nError:\n\n" + ex.Message);
            }
        }

        private void rescaleTransectPlot(object sender, MouseButtonEventArgs e)
        {
            try
            {
                if (e == null || e.MiddleButton == MouseButtonState.Pressed)
                {
                    var plt = (WpfPlot)sender;

                    int idx = transectCreationList.SelectedIndex;

                    if (idx < 0)
                    {
                        return;
                    }

                    plt.Plot.SetAxisLimits(-transects[idx].width * 1.1, transects[idx].width * 1.1 + 1, -transects[idx].lengthBelow * 1.1, transects[idx].lengthAbove * 1.1 + 1);

                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("An unkown error has occurred\n\nFunction: rescaleTransectPlot\n\nError:\n\n" + ex.Message);
            }
        }

        private void rescalePatternPlot(object sender, MouseButtonEventArgs e)
        {
            try
            {
                if (e == null || e.MiddleButton == MouseButtonState.Pressed)
                {
                    var plt = (WpfPlot)sender;
                    int spacing = (int)Math.Floor(double.Parse(vectorSpacing.Text, CultureInfo.InvariantCulture));

                    int idx = transectCreationList.SelectedIndex;

                    if (idx < 0)
                    {
                        return;
                    }

                    plt.Plot.XAxis.Ticks(false);

                    var w = plt.Plot.Width;
                    var h = plt.Plot.Height;

                    var dx = (Math.Ceiling(transects[idx].lengthBelow / spacing) * spacing + spacing / 2.0 + Math.Ceiling(transects[idx].lengthAbove / spacing) * spacing + spacing / 2.0) * (w / h) / 4.0;

                    plt.Plot.SetAxisLimits(-dx,
                                           dx,
                                           -(Math.Ceiling(transects[idx].lengthBelow / spacing) * spacing + spacing / 2.0),
                                           Math.Ceiling(transects[idx].lengthAbove / spacing) * spacing + spacing / 2.0 + 1);

                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("An unkown error has occurred\n\nFunction: rescalePatternPlot\n\nError:\n\n" + ex.Message);
            }
        }

        private void rescaleMatchedPatternPlot(object sender, MouseButtonEventArgs e)
        {
            try
            {
                if (e == null || e.MiddleButton == MouseButtonState.Pressed)
                {
                    var plt = (WpfPlot)sender;
                    double spacing = lastRunSpacing;

                    /* int idx = resultTransectsList.SelectedIndex;

                    if (idx < 0)
                    {
                        return;
                    }*/

                    //plt.Plot.XAxis.Ticks(false);

                    /*var w = plt.Plot.Width;
                    var h = plt.Plot.Height;

                    var dx = (Math.Ceiling(transects[idx].lengthBelow / spacing) * spacing + (spacing / 2.0) + Math.Ceiling(transects[idx].lengthAbove / spacing) * spacing + (spacing / 2.0)) * (w / h) / 4.0;

                    plt.Plot.SetAxisLimits(-dx,
                                           dx,
                                           -(Math.Ceiling(transects[idx].lengthBelow / spacing) * spacing + spacing / 2.0),
                                           Math.Ceiling(transects[idx].lengthAbove / spacing) * spacing + spacing / 2.0 + 1);*/

                    plt.Plot.AxisAuto();
                    var a = plt.Plot.GetAxisLimits();
                    var w = plt.Plot.Width;
                    var h = plt.Plot.Height;

                    var dx = Math.Abs(a.YMax - a.YMin) * 0.22;

                    plt.Plot.SetAxisLimitsY(a.YMin + spacing, a.YMax + spacing);
                    plt.Plot.SetAxisLimitsX(-dx / 2, dx / 2);

                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("An unkown error has occurred\n\nFunction: rescaleMatchedPatternPlot\n\nError:\n\n" + ex.Message);
            }
        }

        private void rescalePlot(object sender, MouseButtonEventArgs e)
        {
            try
            {
                if (e == null || e.MiddleButton == MouseButtonState.Pressed)
                {
                    var plt = (WpfPlot)sender;

                    plt.Plot.AxisAuto();
                    var a = plt.Plot.GetAxisLimits();
                    var w = plt.Plot.Width;
                    var h = plt.Plot.Height;
                    plt.Plot.SetAxisLimitsX(a.XMin, a.XMin + 1 + Math.Abs(a.YMax - a.YMin) * (w / h));
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("An unkown error has occurred\n\nFunction: rescalePlot\n\nError:\n\n" + ex.Message);
            }
        }

        //used to ensure you can only enter a positive doubleing point number into the textboxes
        private void NumberValidationTextBox(object sender, TextCompositionEventArgs e)
        {
            //text already in the text box
            string textBoxText = ((TextBox)sender).Text;

            //the key that was just entered
            char newChar = e.Text[0];

            bool foundDot = textBoxText.Contains('.');

            if (!char.IsDigit(newChar))
            {
                if (textBoxText.Length == 0 || newChar != '.' || foundDot)
                {
                    //causes the entered key to not be added the the text box
                    e.Handled = true;
                    return;
                }
            }

            e.Handled = false;

        }

        //used to ensure you can only enter a positive doubleing point number into the textboxes
        private void NegativeNumberValidationTextBox(object sender, TextCompositionEventArgs e)
        {
            /*//text already in the text box
            string textBoxText = ((TextBox)sender).Text;

            //the key that was just entered
            char newChar = e.Text[0];

            bool foundDot = textBoxText.Contains('.');
            bool foundNeg = textBoxText.Contains('-');

            if (!char.IsDigit(newChar))
            {
                if ((textBoxText.Length == 0 && newChar != '-') || (textBoxText.Length != 0 && newChar == '-') || (textBoxText.Length == 0 && newChar == '.') || (textBoxText.Length == 1 && newChar == '.' && foundNeg) || (newChar != '.' && newChar != '-') || (foundDot && newChar == '.') || (foundNeg && newChar == '-'))
                {
                    //causes the entered key to not be added the the text box
                    e.Handled = true;
                    return;
                }
            }

            e.Handled = false;*/

            var textBox = sender as TextBox;

            var fullText = textBox.Text.Insert(textBox.SelectionStart, e.Text);

            if(fullText == "-")
            {
                e.Handled = false;
                return;
            }

            double val;
            // If parsing is successful, set Handled to false
            e.Handled = !double.TryParse(fullText,
                                         NumberStyles.AllowDecimalPoint | NumberStyles.AllowLeadingSign,
                                         CultureInfo.InvariantCulture,
                                         out val);

        }

        private int numOfTransectsCreated = 0;

        private void newTransect(object sender, RoutedEventArgs e)
        {
            try
            {
                transectCreationList.Items.Add("T" + ++numOfTransectsCreated);

                if (convergenceLineX.Length == 0)
                {
                    transects.Add(new Transect());
                }
                else
                {
                    double lengthAbove = double.Parse(transectLengthAboveBox.Text, CultureInfo.InvariantCulture);
                    double lengthBelow = double.Parse(transectLengthBelowBox.Text, CultureInfo.InvariantCulture);
                    double width = double.Parse(transectWidthBox.Text, CultureInfo.InvariantCulture);

                    var t = new Transect(convergenceLineX[0], convergenceLineY[0], lengthAbove, lengthBelow, width);
                    t.setPerpendicularAngle(convergenceLineX[0], convergenceLineY[0], convergenceLineX[1], convergenceLineY[1]);
                    t.renderTransect(transectsOverviewPlot.Plot);

                    transects.Add(t);
                    transectsOverviewPlot.Refresh();
                }
                transectCreationList.SelectedIndex = transectCreationList.Items.Count - 1;
                transectSelected(null, null);
            }
            catch (Exception ex)
            {
                MessageBox.Show("An unkown error has occurred\n\nFunction: newTransect\n\nError:\n\n" + ex.Message);
            }
        }

        private void fineAdjustment(object sender, RoutedEventArgs e)
        {
            try
            {
                var b = sender as Button;

                if (b == null) return;

                switch (b.Tag)
                {
                    case "up":
                        transectHeightOffsetSlider.Value = Math.Clamp(transectHeightOffsetSlider.Value + 1, transectHeightOffsetSlider.Minimum, transectHeightOffsetSlider.Maximum);
                        break;

                    case "down":
                        transectHeightOffsetSlider.Value = Math.Clamp(transectHeightOffsetSlider.Value - 1, transectHeightOffsetSlider.Minimum, transectHeightOffsetSlider.Maximum);
                        break;

                    case "cw":
                        transectAngleOffsetSlider.Value = Math.Clamp(transectAngleOffsetSlider.Value - 0.5, transectAngleOffsetSlider.Minimum, transectAngleOffsetSlider.Maximum);
                        break;

                    case "ccw":
                        transectAngleOffsetSlider.Value = Math.Clamp(transectAngleOffsetSlider.Value + 0.5, transectAngleOffsetSlider.Minimum, transectAngleOffsetSlider.Maximum);
                        break;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("An unkown error has occurred\n\nFunction: fineAdjustment\n\nError:\n\n" + ex.Message);
            }
        }


        private async void autoGenTransects(object sender, RoutedEventArgs e)
        {
            autoGenTransectsButton.IsEnabled = false;

            try
            {
                if (convergenceLineX.IsNullOrEmpty()) return;

                double spacing = double.Parse(autoGenSpacing.Text, CultureInfo.InvariantCulture);


                for (double i = spacing; i < convergenceLineDists[convergenceLineDists.Length - 1]; i += spacing)
                {
                    transectCreationList.Items.Add("T" + ++numOfTransectsCreated);
                    double lengthAbove = double.Parse(transectLengthAboveBox.Text, CultureInfo.InvariantCulture);
                    double lengthBelow = double.Parse(transectLengthBelowBox.Text, CultureInfo.InvariantCulture);
                    double width = double.Parse(transectWidthBox.Text, CultureInfo.InvariantCulture);

                    for (int j = 1; j < convergenceLineDists.Length; j++)
                    {
                        if (i < convergenceLineDists[j])
                        {

                            double n = (i - convergenceLineDists[j - 1]) / (convergenceLineDists[j] - convergenceLineDists[j - 1]);

                            double x = convergenceLineX[j] * n + convergenceLineX[j - 1] * (1 - n);
                            double y = convergenceLineY[j] * n + convergenceLineY[j - 1] * (1 - n);

                            var t = new Transect(x, y, lengthAbove, lengthBelow, width);

                            t.setPerpendicularAngle(convergenceLineX[j - 1], convergenceLineY[j - 1], convergenceLineX[j], convergenceLineY[j]);

                            t.positionOffset = i;

                            t.renderTransect(transectsOverviewPlot.Plot);

                            transects.Add(t);

                            break;
                        }
                    }
                    transectCreationList.SelectedIndex = transectCreationList.Items.Count - 1;
                    transectSelected(null, null);

                }

                transectsOverviewPlot.Refresh();
            }
            catch (Exception ex)
            {
                MessageBox.Show("An unkown error has occurred\n\nFunction: autoGenTransects\n\nError:\n\n" + ex.Message);
            }

            await Task.Delay(500);
            autoGenTransectsButton.IsEnabled = true;
        }

        private void removeTransect(object sender, RoutedEventArgs e)
        {
            try
            {
                int idx = transectCreationList.SelectedIndex;

                if (idx < 0) return;

                transectCreationList.Items.RemoveAt(idx);

                if (idx < resultTransectsList.Items.Count)
                {
                    resultTransectsList.Items.RemoveAt(idx);
                }

                transects.RemoveAt(idx);

                reDrawTransectOverview();
            }
            catch (Exception ex)
            {
                MessageBox.Show("An unkown error has occurred\n\nFunction: removeTransect\n\nError:\n\n" + ex.Message);
            }
        }

        
        private bool lockSliders = false;
        private async void SliderValueChanged(object sender, RoutedEventArgs e)
        {
            try
            {
                if (lockSliders) return;
                //lockSliders = true;

                Slider slider = null;
                TextBox tb;

                if (sender is Slider)
                {
                    slider = (Slider)sender;
                    slider.IsEnabled = false;

                    tb = sliderTextDict.GetValueOrDefault(slider);

                    if (tb == null || tb.Text == "" || tb.Text == "-")
                    {
                        slider.IsEnabled = true;
                        return;
                    }

                    tb.Text = (Math.Round(slider.Value, 2)).ToString();
                }
                else if (sender is TextBox)
                {
                    tb = (TextBox)sender;

                    slider = sliderTextDict.FirstOrDefault(x => x.Value == tb).Key;

                    if (slider != null)
                    {
                        if (tb.Text.Length == 0 || tb.Text == "-")
                        {
                            slider.Value = slider.Minimum;
                        }
                        else
                        {
                            slider.Value = Math.Clamp(double.Parse(tb.Text, CultureInfo.InvariantCulture), slider.Minimum, slider.Maximum);
                        }
                    }
                }

                if (transectAngleOffsetSlider == null || transectPositionSlider == null || transectLengthAboveSlider == null || transectLengthBelowSlider == null || transectWidthSlider == null || transectHeightOffsetSlider == null)
                {
                    if (slider != null)
                    {
                        slider.IsEnabled = true;
                    }
                    return;
                }

                double positionValue = transectPositionSlider.Value;

                if (transectCreationList == null) return;

                int idx = transectCreationList.SelectedIndex;

                if (idx < 0)
                {
                    if (slider != null)
                    {
                        slider.IsEnabled = true;
                    }
                    return;
                }

                transects[idx].positionOffset = positionValue;
                transects[idx].heightOffset = transectHeightOffsetSlider.Value;
                transects[idx].lengthAbove = transectLengthAboveSlider.Value;
                transects[idx].lengthBelow = transectLengthBelowSlider.Value;
                transects[idx].width = transectWidthSlider.Value;
                transects[idx].angleOffset = transectAngleOffsetSlider.Value;

                for (int i = 1; i < convergenceLineDists.Length; i++)
                {
                    if (positionValue < convergenceLineDists[i])
                    {
                        double n = (positionValue - convergenceLineDists[i - 1]) / (convergenceLineDists[i] - convergenceLineDists[i - 1]);

                        transects[idx].x = convergenceLineX[i] * n + convergenceLineX[i - 1] * (1 - n);
                        transects[idx].y = convergenceLineY[i] * n + convergenceLineY[i - 1] * (1 - n);
                        transects[idx].setPerpendicularAngle(convergenceLineX[i - 1], convergenceLineY[i - 1], convergenceLineX[i], convergenceLineY[i]);
                        break;
                    }
                }


                transects[idx].updateTransect();
                transectsOverviewPlot.Refresh();

                try
                {
                    await QueuedTask.Run(() =>
                    {
                        using (ArcGIS.Core.Data.Table shp_table = selectedVector.GetTable())
                        {
                            SpatialQueryFilter spatialQueryFilter = new SpatialQueryFilter
                            {
                                FilterGeometry = new PolygonBuilderEx(new List<Coordinate2D>
                                    {
                                      new Coordinate2D(transects[idx].box.Xs[0], transects[idx].box.Ys[0]),
                                      new Coordinate2D(transects[idx].box.Xs[1], transects[idx].box.Ys[1]),
                                      new Coordinate2D(transects[idx].box.Xs[2], transects[idx].box.Ys[2]),
                                      new Coordinate2D(transects[idx].box.Xs[3], transects[idx].box.Ys[3])
                                    }).ToGeometry(),
                                SpatialRelationship = SpatialRelationship.Intersects

                            };


                            double rotAngle = (Math.PI / 2.0) - transects[idx].theta;
                            var (x, y) = transects[idx].getCenter();
                            var (centerX, centerY) = rotatePoint(x, y, rotAngle);

                            using (RowCursor rowCursor = shp_table.Search(spatialQueryFilter, false))
                            {
                                lock (transectVectors)
                                {
                                    transectVectors.Clear();
                                    while (rowCursor.MoveNext())
                                    {
                                        using (Feature f = (Feature)rowCursor.Current)
                                        {
                                            ArcGIS.Core.Geometry.Polyline pl = (ArcGIS.Core.Geometry.Polyline)f.GetShape();

                                            var (x1, y1) = rotatePoint(pl.Points[0].X, pl.Points[0].Y, rotAngle);
                                            var (x2, y2) = rotatePoint(pl.Points[1].X, pl.Points[1].Y, rotAngle);

                                            transectVectors.Add((x1 - centerX, y1 - centerY, x2 - centerX, y2 - centerY));
                                        }
                                    }
                                }
                            }
                        }
                    });
                }
                catch(Exception ex1)
                {
                    MessageBox.Show("An error occurred when the ArcGIS query tried to find vectors inside the current transect\n\nError: " + ex1.Message);
                }

                if (vectorSpacing.Text != null && vectorSpacing.Text.Length != 0 && Math.Floor(double.Parse(vectorSpacing.Text, CultureInfo.InvariantCulture)) != 0.0)
                {
                    transectPatternPlot.Plot.Clear();
                    var patternField = transectPatternPlot.Plot.AddVectorFieldList();
                    patternField.Color = System.Drawing.Color.Blue;
                    patternField.ArrowStyle.ScaledArrowheads = true;

                    int spacing = (int)Math.Floor(double.Parse(vectorSpacing.Text, CultureInfo.InvariantCulture));
                    int length = ((int)Math.Ceiling(transects[idx].lengthAbove / (double)spacing) + (int)Math.Ceiling(transects[idx].lengthBelow / (double)spacing)) * spacing;
                    int size = (int)Math.Ceiling(length / (double)spacing) + 1;

                    List<(double, double)>[] patternBins = new List<(double, double)>[size];

                    for (int i = 0; i < size; i++)
                    {
                        patternBins[i] = new();
                    }

                    lock (transectVectors)
                    {
                        foreach (var v in transectVectors)
                        {
                            double middle = (v.Item2 + v.Item4) / 2.0;

                            patternBins[(int)Math.Clamp(Math.Abs(Math.Ceiling(transects[idx].lengthAbove / (double)spacing) * (double)spacing - middle + (double)spacing / 2.0) / spacing, 0, size - 1)].Add((v.Item3 - v.Item1, v.Item4 - v.Item2));
                        }

                        transects[idx].patternVecs.Clear();

                        if ((bool)avgCheckbox.IsChecked)
                        {
                            for (int i = 0; i < size; i++)
                            {
                                if (patternBins[i].Count() == 0)
                                {
                                    transects[idx].patternVecs.Add(0.0f);
                                    transects[idx].patternVecs.Add(0.0f);
                                    continue;
                                }

                                double avgX = 0;
                                double avgY = 0;

                                foreach (var v in patternBins[i])
                                {
                                    avgX += v.Item1;
                                    avgY += v.Item2;
                                }


                                double mag = Math.Sqrt(avgX * avgX + avgY * avgY);

                                avgX /= mag;
                                avgY /= mag;

                                transects[idx].patternVecs.Add((double)avgX);
                                transects[idx].patternVecs.Add((double)avgY);

                                avgX *= spacing;
                                avgY *= spacing;

                                patternField.RootedVectors.Add((new Coordinate(0, Math.Ceiling(transects[idx].lengthAbove / spacing) * spacing - spacing * i), new CoordinateVector(avgX, avgY)));
                            }
                        }
                        else
                        {

                            for (int i = 0; i < size; i++)
                            {
                                if (patternBins[i].Count() == 0)
                                {
                                    transects[idx].patternVecs.Add(0.0f);
                                    transects[idx].patternVecs.Add(0.0f);
                                    continue;
                                }

                                var sortedX = patternBins[i].OrderBy(x => x.Item1).ToList();
                                var sortedY = patternBins[i].OrderBy(x => x.Item2).ToList();

                                double medX;
                                double medY;

                                if (patternBins[i].Count() % 2 == 1)
                                {
                                    medX = sortedX[(int)Math.Floor(sortedX.Count() / 2.0)].Item1;
                                    medY = sortedY[(int)Math.Floor(sortedY.Count() / 2.0)].Item2;
                                }
                                else
                                {
                                    medX = (sortedX[(int)Math.Floor(sortedX.Count() / 2.0)].Item1 + sortedX[(int)Math.Floor(sortedX.Count() / 2.0) - 1].Item1) / 2.0;
                                    medY = (sortedY[(int)Math.Floor(sortedY.Count() / 2.0)].Item2 + sortedY[(int)Math.Floor(sortedY.Count() / 2.0) - 1].Item2) / 2.0;
                                }
                                double mag = Math.Sqrt(medX * medX + medY * medY);

                                medX /= mag;
                                medY /= mag;

                                transects[idx].patternVecs.Add((double)medX);
                                transects[idx].patternVecs.Add((double)medY);

                                medX *= spacing;
                                medY *= spacing;

                                patternField.RootedVectors.Add((new Coordinate(0, Math.Ceiling(transects[idx].lengthAbove / spacing) * spacing - spacing * i), new CoordinateVector(medX, medY)));
                            }
                        }
                    }

                    transectPatternPlot.Plot.Layout(left: 0, right: 0, bottom: 0, top: 0);
                    rescalePatternPlot(transectPatternPlot, null);
                    transectPatternPlot.Refresh();

                    transectPatternPlot.Configuration.MiddleClickAutoAxis = false;
                }

                transectVectorsPlot.Plot.Clear();

                transectVectorsPlot.Plot.AddHorizontalLine(0, color: System.Drawing.Color.Red, width: 2, style: LineStyle.Dash);

                var field = transectVectorsPlot.Plot.AddVectorFieldList();
                field.Color = System.Drawing.Color.Blue;
                field.ArrowStyle.ScaledArrowheads = true;

                //var vecs = transectVectors.ToList();

                lock (transectVectors)
                {
                    foreach (var v in transectVectors)
                    {
                        field.RootedVectors.Add((new Coordinate((v.Item1 + v.Item3) / 2.0, (v.Item2 + v.Item4) / 2.0), new CoordinateVector(v.Item3 - v.Item1, v.Item4 - v.Item2)));
                    }
                }

                transectVectorsPlot.Plot.Layout(left: 0, right: 0, bottom: 0, top: 0);
                rescaleTransectPlot(transectVectorsPlot, null);
                transectVectorsPlot.Refresh();

                transectVectorsPlot.Configuration.MiddleClickAutoAxis = false;

                //lockSliders = false;
                if (slider != null)
                {
                    slider.IsEnabled = true;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("An unkown error has occurred\n\nFunction: SliderValueChanged\n\nError:\n\n" + ex.Message);
            }
        }

        private void transectSelected(object sender, SelectionChangedEventArgs e)
        {
            try
            {
                int idx = transectCreationList.SelectedIndex;

                if (idx < 0)
                {
                    return;
                }

                lockSliders = true;

                transectHeightOffsetSlider.Value = transects[idx].heightOffset;
                transectLengthAboveSlider.Value = transects[idx].lengthAbove;
                transectLengthBelowSlider.Value = transects[idx].lengthBelow;
                transectWidthSlider.Value = transects[idx].width;
                transectAngleOffsetSlider.Value = transects[idx].angleOffset;

                transectHeightOffsetBox.Text = transects[idx].heightOffset.ToString();
                transectLengthAboveBox.Text = transects[idx].lengthAbove.ToString();
                transectLengthBelowBox.Text = transects[idx].lengthBelow.ToString();
                transectWidthBox.Text = transects[idx].width.ToString();
                transectAngleOffsetBox.Text = transects[idx].angleOffset.ToString();

                lockSliders = false;

                transectPositionSlider.Value = transects[idx].positionOffset;

                SliderValueChanged(null, null);

                for (int i = 0; i < transects.Count; i++)
                {
                    if (i == idx)
                    {
                        transects[i].setOutlineColor(System.Drawing.Color.Red);
                    }
                    else
                    {
                        transects[i].setOutlineColor(System.Drawing.Color.Turquoise);
                    }
                }

                transectsOverviewPlot.Refresh();
            }
            catch (Exception ex)
            {
                MessageBox.Show("An unkown error has occurred\n\nFunction: transectSelected\n\nError:\n\n" + ex.Message);
            }
        }

        private (double, double) rotatePoint(double x, double y, double rotAngle)
        {
            double x1 = x * Math.Cos(rotAngle) - y * Math.Sin(rotAngle);
            double y1 = x * Math.Sin(rotAngle) + y * Math.Cos(rotAngle);

            return (x1, y1);
        }

        bool matchLock = false;

        private async void runPatternMatching(object sender, RoutedEventArgs e)
        {
            if(matchLock) return;

            matchLock = true;

            try
            {
                double[] modelParams = getModelParameters();

                if (modelParams.IsNullOrEmpty())
                {
                    matchLock = false;
                    return;
                }

                Window loadingBarWindow = new Window();
                loadingBarWindow.Title = "Matching Patterns";
                loadingBarWindow.Height = 200;
                loadingBarWindow.Width = 500;

                loadingBarWindow.Show();

                ProgressBar pb = new ProgressBar();

                pb.Name = "pbStatus";
                pb.Minimum = 0;
                pb.Maximum = transects.Count;
                pb.Value = 0;
                pb.Width = 400;
                pb.Height = 30;

                TextBlock tb = new TextBlock();
                tb.HorizontalAlignment = System.Windows.HorizontalAlignment.Center;
                tb.VerticalAlignment = System.Windows.VerticalAlignment.Center;
                tb.FontWeight = FontWeights.Bold;

                Binding b = new Binding();
                b.Source = pb;
                b.Path = new PropertyPath("Value");
                b.StringFormat = "{0}/" + transects.Count.ToString();

                tb.SetBinding(TextBlock.TextProperty, b);

                Grid g = new Grid();
                g.Children.Add(pb);
                g.Children.Add(tb);

                loadingBarWindow.Content = g;

                double spacing = (int)Math.Floor(double.Parse(vectorSpacing.Text, CultureInfo.InvariantCulture));
                lastRunSpacing = spacing;

                int modelType = modelTypeListView.SelectedIndex > 0 ? modelTypeListView.SelectedIndex : 0;
                int compareType = errorTypeListView.SelectedIndex > 0 ? errorTypeListView.SelectedIndex : 0;
                int weightType = weightTypeListView.SelectedIndex > 0 ? weightTypeListView.SelectedIndex : 0;

                bool failedToMatch = false;

                for (int i = 0; i < transects.Count(); i++)
                {
                    await QueuedTask.Run(() =>
                    {
                        transects[i].matches = PatternSolver.solveBestMatches(transects[i].patternVecs, modelParams, modelType, compareType, weightType, spacing,
                                                                              Math.Ceiling(transects[i].lengthAbove / spacing) * spacing,
                                                                              Math.Ceiling(transects[i].lengthBelow / spacing) * spacing);

                    });

                    if (transects[i].matches.Count == 0)
                    {
                        failedToMatch = true;
                    }

                    if (!loadingBarWindow.IsLoaded)
                    {
                        matchLock = false;
                        return;
                    }

                    pb.Value++;
                }

                if(failedToMatch)
                {
                    MessageBox.Show("FAILED TO MATCH PATTERN\n\n" +
                                    "Failed to find adequete matches for some transects consider\n" +
                                    "increasing the match tolerance or fitting better patterns");
                }

                loadingBarWindow.Close();

                resultTransectsList.Items.Clear();

                foreach (var t in transectCreationList.Items)
                {
                    resultTransectsList.Items.Add(t);
                }

                resultTransectsList.SelectedIndex = 0;

                headerTabs.SelectedIndex = 3;
            }
            catch (Exception ex)
            {
                MessageBox.Show("An unkown error has occurred\n\nFunction: runPatternMatching\n\nError:\n\n" + ex.Message);
            }

            matchLock = false;
        }


        private void testMatch(object sender, RoutedEventArgs e)
        {
            try
            {
                int idx = transectCreationList.SelectedIndex;

                if (idx < 0)
                {
                    return;
                }

                double spacing = (int)Math.Floor(double.Parse(vectorSpacing.Text, CultureInfo.InvariantCulture));

                //patternVecs.Reverse();

                //List<double> reversedPatternVecs = PatternSolver.reverseAndRotate(patternVecs);

                //System.Diagnostics.Debug.WriteLine(patternVecs.Count());

                int modelType = modelTypeListView.SelectedIndex > 0 ? modelTypeListView.SelectedIndex : 0;
                int compareType = errorTypeListView.SelectedIndex > 0 ? errorTypeListView.SelectedIndex : 0;
                int weightType = weightTypeListView.SelectedIndex > 0 ? weightTypeListView.SelectedIndex : 0;

                var matches = PatternSolver.solveBestMatches(transects[idx].patternVecs, getModelParameters(), modelType, compareType, weightType, spacing,
                                                                              Math.Ceiling(transects[idx].lengthAbove / spacing) * spacing,
                                                                              Math.Ceiling(transects[idx].lengthBelow / spacing) * spacing);

                System.Diagnostics.Debug.WriteLine(matches.Count());

                centerIdx = (int)Math.Ceiling(transects[idx].lengthBelow / spacing);

                //System.Diagnostics.Debug.WriteLine("************" + matches[0][1] + ", " + matches[0][2] + ", " + matches[0][3] + ", " + matches[0][4] + ", " + matches[0][5]);

                var bestMatch = PatternSolver.getPattern(new double[] { matches[0][1], matches[0][2], matches[0][3], matches[0][4], matches[0][5], matches[0][6] }, modelType, spacing,
                                                         new double[] { transects[idx].patternVecs[centerIdx * 2], transects[idx].patternVecs[centerIdx * 2 + 1] });

                var patternField = transectPatternPlot.Plot.AddVectorFieldList();
                patternField.Color = System.Drawing.Color.DarkRed;
                patternField.ArrowStyle.ScaledArrowheads = true;

                foreach (double[] v in bestMatch)
                {
                    patternField.RootedVectors.Add((new Coordinate(0, v[1]), new CoordinateVector(v[2] * spacing, v[3] * spacing)));
                }

                transectPatternPlot.Refresh();

                //List<double> matchVelocities = new List<double>();
                //double min = 999.0;
                //double max = 0.0;

                //foreach (var m in matches)
                //{
                //    matchVelocities.Add((double)m[6]);

                //    max = Math.Max(max, m[6]);
                //    min = Math.Min(min, m[6]);
                //}

                //double scale = (max - min) / 10.0;

                ////max = Math.Ceiling(max / scale) * scale;
                ////min = Math.Floor(min / scale) * scale;

                //var hist = ScottPlot.Statistics.Histogram.WithFixedBinCount(min, max, 10);

                //hist.AddRange(matchVelocities);

                //var bar = windSpeedDistributionPlot.Plot.AddBar(hist.GetProbability(), positions: hist.Bins);
                //bar.BarWidth = scale;

                //windSpeedDistributionPlot.Plot.AddFunction(hist.GetProbabilityCurve(matchVelocities.ToArray(), true), System.Drawing.Color.Black, 2, LineStyle.Dash);

                ////windSpeedDistributionPlot.Plot.TopAxis.IsVisible = true;
                ////windSpeedDistributionPlot.Plot.TopAxis.Ticks(true);
                ////windSpeedDistributionPlot.Plot.TopAxis.Label("EF-scale Rating");
                ////windSpeedDistributionPlot.Plot.TopAxis.TickLabelFormat(efScaleTickFormatter);
                ////windSpeedDistributionPlot.Plot.BottomAxis.ManualTickSpacing(12.5);
                ////windSpeedDistributionPlot.Plot.TopAxis.ManualTickPositions(new double[] { 43.0, 55.5, 68.0, 80.5, 93.0 }, new string[] { "EF1", "EF2", "EF3", "EF4", "EF5" });

                ////bar.XAxisIndex = windSpeedDistributionPlot.Plot.TopAxis.AxisIndex;

                ////var pt1 = windSpeedDistributionPlot.Plot.AddPoint(min, 0);
                ////pt1.XAxisIndex = windSpeedDistributionPlot.Plot.TopAxis.AxisIndex;
                ////var pt2 = windSpeedDistributionPlot.Plot.AddPoint(max, 0);
                ////pt2.XAxisIndex = windSpeedDistributionPlot.Plot.TopAxis.AxisIndex;

                //windSpeedDistributionPlot.Refresh();
            }
            catch (Exception ex)
            {
                MessageBox.Show("An unkown error has occurred\n\nFunction: testMatch\n\nError:\n\n" + ex.Message);
            }
        }

        private double[] getModelParameters()
        {
            try
            {
                double[] modelParameters = new double[22];

                modelParameters[0] = double.Parse(vtmin.Text, CultureInfo.InvariantCulture);
                modelParameters[1] = double.Parse(vtmax.Text, CultureInfo.InvariantCulture);
                modelParameters[2] = double.Parse(vtstep.Text, CultureInfo.InvariantCulture);
                modelParameters[3] = double.Parse(vrmin.Text, CultureInfo.InvariantCulture);
                modelParameters[4] = double.Parse(vrmax.Text, CultureInfo.InvariantCulture);
                modelParameters[5] = double.Parse(vrstep.Text, CultureInfo.InvariantCulture);
                modelParameters[6] = double.Parse(vsmin.Text, CultureInfo.InvariantCulture);
                modelParameters[7] = double.Parse(vsmax.Text, CultureInfo.InvariantCulture);
                modelParameters[8] = double.Parse(vsstep.Text, CultureInfo.InvariantCulture);
                modelParameters[9] = double.Parse(vcmin.Text, CultureInfo.InvariantCulture);
                modelParameters[10] = double.Parse(vcmax.Text, CultureInfo.InvariantCulture);
                modelParameters[11] = double.Parse(vcstep.Text, CultureInfo.InvariantCulture);
                modelParameters[12] = double.Parse(rmaxmin.Text, CultureInfo.InvariantCulture);
                modelParameters[13] = double.Parse(rmaxmax.Text, CultureInfo.InvariantCulture);
                modelParameters[14] = double.Parse(rmaxstep.Text, CultureInfo.InvariantCulture);
                modelParameters[15] = double.Parse(phimin.Text, CultureInfo.InvariantCulture);
                modelParameters[16] = double.Parse(phimax.Text, CultureInfo.InvariantCulture);
                modelParameters[17] = double.Parse(phistep.Text, CultureInfo.InvariantCulture);
                modelParameters[18] = double.Parse(allowedLengthDifference.Text, CultureInfo.InvariantCulture);
                modelParameters[19] = double.Parse(maxLengthDifference.Text, CultureInfo.InvariantCulture);
                modelParameters[20] = double.Parse(cutoffThreshold.Text, CultureInfo.InvariantCulture);
                modelParameters[21] = (bool)cutoffToggle.IsChecked ? 1.0 : 0.0;

                if (modelParameters[0] < 1 || modelParameters[1] > 50 || modelParameters[3] < 20 || modelParameters[4] > 80 || modelParameters[6] < 5 || modelParameters[7] > 25 || modelParameters[9] < 35 || modelParameters[10] > 65 || modelParameters[15] < 0.5 || modelParameters[16] > 1.5 || modelParameters[20] <= 0 || modelParameters[20] > 1)
                {
                    MessageBox.Show("Invalid parameter ranges entered, Current Vaild Ranges are:\n\nVr: 20 - 80\nVt: 1 - 50\nVs: 5 - 25\nVc: 35 - 65\nPhi: 0.5 - 1.5\nCutoff: 0 - 1");
                    return Array.Empty<double>();
                }

                return modelParameters;
            }
            catch(Exception ex)
            {
                MessageBox.Show("Invalid parameters entered");
                return Array.Empty<double>();
            }
        }

        private void gotoModelParameters(object sender, RoutedEventArgs e)
        {
            headerTabs.SelectedIndex = 2;
        }

        private void resultTransectSelected(object sender, SelectionChangedEventArgs e)
        {
            try
            {
                int idx = resultTransectsList.SelectedIndex;

                if (idx < 0 || transects[idx].matches.IsNullOrEmpty()) return;

                bestMatchesListBox.Items.Clear();

                for (int i = 0; i < transects[idx].matches.Count(); i++)
                {
                    bestMatchesListBox.Items.Add((i + 1).ToString());
                }

                bestMatchesListBox.SelectedIndex = 0;

                double[] matchVelocities = new double[transects[idx].matches.Count];

                double[] minMatch = transects[idx].matches[0];

                for (int i = 0; i < transects[idx].matches.Count; i++)
                {
                    var m = transects[idx].matches[i];
                    matchVelocities[i] = m[7];

                    if (m[7] < minMatch[7])
                    {
                        minMatch = m;
                    }
                }

                System.Diagnostics.Debug.WriteLine(string.Join("\n", minMatch));

                var stats = new ScottPlot.Statistics.BasicStats(matchVelocities);

                double scale = (stats.Max - stats.Min) / 10.0;

                //max = Math.Ceiling(max / scale) * scale;
                //min = Math.Floor(min / scale) * scale;

                windSpeedDistributionPlot.Plot.Clear();

                var hist = ScottPlot.Statistics.Histogram.WithFixedBinSize(stats.Min, stats.Max, 5);

                hist.AddRange(matchVelocities);

                var bar = windSpeedDistributionPlot.Plot.AddBar(hist.GetProbability(), positions: hist.Bins);
                bar.BarWidth = 5;

                var minLine = windSpeedDistributionPlot.Plot.AddVerticalLine(stats.Min, System.Drawing.Color.Black, 2, LineStyle.Solid);
                minLine.PositionLabel = true;
                //medLine.PositionLabelAxis = windSpeedDistributionPlot.Plot.TopAxis;

                windSpeedDistributionPlot.Plot.AddFunction(hist.GetProbabilityCurve(matchVelocities, true), System.Drawing.Color.Black, 2, LineStyle.Dash);

                windSpeedDistributionPlot.Refresh();
            }
            catch (Exception ex)
            {
                MessageBox.Show("An unkown error has occurred\n\nFunction: resultTransectSelected\n\nError:\n\n" + ex.Message);
            }
        }

        private void bestMatchSelected(object sender, SelectionChangedEventArgs e)
        {
            try
            {
                int tIdx = resultTransectsList.SelectedIndex;
                int mIdx = bestMatchesListBox.SelectedIndex;

                if (mIdx < 0 || tIdx < 0) return;

                double[] match = transects[tIdx].matches[mIdx];

                matchInfoListBox.Items.Clear();

                matchInfoListBox.Items.Add("Error: " + match[0].ToString());
                matchInfoListBox.Items.Add("Vt: " + match[1].ToString());
                matchInfoListBox.Items.Add("Vr: " + match[2].ToString());
                matchInfoListBox.Items.Add("Vs: " + match[3].ToString());
                matchInfoListBox.Items.Add("Vc: " + match[4].ToString());
                matchInfoListBox.Items.Add("Rmax: " + match[5].ToString());
                matchInfoListBox.Items.Add("Phi: " + Math.Round(match[6], 2).ToString());
                matchInfoListBox.Items.Add("Vmax: " + match[7].ToString());

                centerIdx = (int)Math.Ceiling(transects[tIdx].lengthAbove / lastRunSpacing);


                int modelType = modelTypeListView.SelectedIndex > 0 ? modelTypeListView.SelectedIndex : 0;

                var bestMatch = PatternSolver.getPattern(new double[] { match[1], match[2], match[3], match[4], match[5], match[6] }, modelType, lastRunSpacing, new double[] { transects[tIdx].patternVecs[centerIdx * 2], transects[tIdx].patternVecs[centerIdx * 2 + 1] });

                matchedPatternPlot.Plot.Clear();

                var patternField = matchedPatternPlot.Plot.AddVectorFieldList();
                //patternField.Color = System.Drawing.Color.DarkRed;
                patternField.ArrowStyle.ScaledArrowheads = true;

                double above = Math.Ceiling(transects[tIdx].lengthAbove / lastRunSpacing) * lastRunSpacing;

                for (int i = 0; i < transects[tIdx].patternVecs.Count() / 2; i++)
                {
                    patternField.RootedVectors.Add((new Coordinate(0, above - i * lastRunSpacing), new CoordinateVector(transects[tIdx].patternVecs[i * 2] * lastRunSpacing, transects[tIdx].patternVecs[i * 2 + 1] * lastRunSpacing)));
                }

                var matchedPatternField = matchedPatternPlot.Plot.AddVectorFieldList();
                matchedPatternField.Color = System.Drawing.Color.DarkRed;
                matchedPatternField.ArrowStyle.ScaledArrowheads = true;

                foreach (double[] v in bestMatch)
                {
                    matchedPatternField.RootedVectors.Add((new Coordinate(0, v[1]), new CoordinateVector(v[2] * lastRunSpacing, v[3] * lastRunSpacing)));
                }

                rescaleMatchedPatternPlot(matchedPatternPlot, null);
                matchedPatternPlot.Refresh();
            }
            catch (Exception ex)
            {
                MessageBox.Show("An unkown error has occurred\n\nFunction: bestMatchSelected\n\nError:\n\n" + ex.Message);
            }
        }

        private void paramSliderChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            try
            {
                int idx = transectCreationList.SelectedIndex;

                if (idx < 0)
                {
                    return;
                }

                centerIdx = (int)Math.Ceiling(transects[idx].lengthBelow / lastRunSpacing);

                //replace 
                int modelType = 0;

                var bestMatch = PatternSolver.getPattern(new double[] { vtSlider.Value, vrSlider.Value, vsSlider.Value, vcSlider.Value, rmaxSlider.Value, phiSlider.Value }, modelType, lastRunSpacing, new double[] { transects[idx].patternVecs[centerIdx * 2], transects[idx].patternVecs[centerIdx * 2 + 1] });

                manualMatchPatternPlot.Plot.Clear();

                var patternField = manualMatchPatternPlot.Plot.AddVectorFieldList();
                //patternField.Color = System.Drawing.Color.DarkRed;
                patternField.ArrowStyle.ScaledArrowheads = true;

                double above = Math.Ceiling(transects[idx].lengthAbove / lastRunSpacing) * lastRunSpacing;

                for (int i = 0; i < transects[idx].patternVecs.Count() / 2; i++)
                {
                    patternField.RootedVectors.Add((new Coordinate(0, above - i * lastRunSpacing), new CoordinateVector(transects[idx].patternVecs[i * 2] * lastRunSpacing, transects[idx].patternVecs[i * 2 + 1] * lastRunSpacing)));
                }

                var matchedPatternField = manualMatchPatternPlot.Plot.AddVectorFieldList();
                matchedPatternField.Color = System.Drawing.Color.DarkRed;
                matchedPatternField.ArrowStyle.ScaledArrowheads = true;

                foreach (double[] v in bestMatch)
                {
                    matchedPatternField.RootedVectors.Add((new Coordinate(0, v[1]), new CoordinateVector(v[2] * lastRunSpacing, v[3] * lastRunSpacing)));
                }

                rescaleMatchedPatternPlot(manualMatchPatternPlot, null);
                manualMatchPatternPlot.Refresh();
            }
            catch (Exception ex)
            {
                MessageBox.Show("An unkown error has occurred\n\nFunction: ParamSliderChanged\n\nError:\n\n" + ex.Message);
            }
        }

        private async void runAllPatternMatching(object sender, RoutedEventArgs e)
        {
            if (matchLock) return;
            matchLock = true;

            try
            {
                double[] modelParams = getModelParameters();

                if (modelParams.IsNullOrEmpty())
                {
                    matchLock = false;
                    return;
                }

                fullResultPanel.Children.Clear();

                Window loadingBarWindow = new Window();
                loadingBarWindow.Title = "Matching Patterns";
                loadingBarWindow.Height = 200;
                loadingBarWindow.Width = 500;

                loadingBarWindow.Show();

                ProgressBar pb = new ProgressBar();
                ProgressBar pbm = new ProgressBar();

                //pb.Name = "pbStatus";
                pb.VerticalAlignment = System.Windows.VerticalAlignment.Top;
                pb.Minimum = 0;
                pb.Maximum = transects.Count;
                pb.Value = 0;
                pb.Width = 400;
                pb.Height = 30;
                pb.Margin = new Thickness(30);

                pbm.VerticalAlignment = System.Windows.VerticalAlignment.Bottom;
                pbm.Minimum = 0;
                pbm.Maximum = modelTypeListView.Items.Count;
                pbm.Value = 0;
                pbm.Width = 400;
                pbm.Height = 30;
                pbm.Margin = new Thickness(30);

                TextBlock tb = new TextBlock();
                tb.HorizontalAlignment = System.Windows.HorizontalAlignment.Center;
                tb.VerticalAlignment = System.Windows.VerticalAlignment.Top;
                tb.FontWeight = FontWeights.Bold;
                tb.Margin = new Thickness(37);

                TextBlock tbm = new TextBlock();
                tbm.HorizontalAlignment = System.Windows.HorizontalAlignment.Center;
                tbm.VerticalAlignment = System.Windows.VerticalAlignment.Bottom;
                tbm.FontWeight = FontWeights.Bold;
                tbm.Margin = new Thickness(37);

                Binding b = new Binding();
                b.Source = pb;
                b.Path = new PropertyPath("Value");
                b.StringFormat = "{0}/" + transects.Count.ToString();

                Binding bm = new Binding();
                bm.Source = pbm;
                bm.Path = new PropertyPath("Value");
                bm.StringFormat = "{0}/" + modelTypeListView.Items.Count.ToString();

                tb.SetBinding(TextBlock.TextProperty, b);
                tbm.SetBinding(TextBlock.TextProperty, bm);

                Grid g = new Grid();
                g.Children.Add(pb);
                g.Children.Add(tb);
                g.Children.Add(pbm);
                g.Children.Add(tbm);

                loadingBarWindow.Content = g;

                double spacing = (int)Math.Floor(double.Parse(vectorSpacing.Text, CultureInfo.InvariantCulture));
                lastRunSpacing = spacing;

                //int modelType = modelTypeListView.SelectedIndex > 0 ? modelTypeListView.SelectedIndex : 0;
                int compareType = errorTypeListView.SelectedIndex > 0 ? errorTypeListView.SelectedIndex : 0;
                int weightType = weightTypeListView.SelectedIndex > 0 ? weightTypeListView.SelectedIndex : 0;

                bool failedToMatch = false;

                for (int i = 0; i < transects.Count; i++)
                {
                    double swirlSum = 0.0;
                    double minVelSum = 0.0;
                    double bestErrorSum = 0.0;
                    List<MatchingResult> results = new List<MatchingResult>();
                    transects[i].results.Clear();

                    for (int j = 0; j < modelTypeListView.Items.Count; j++)
                    {
                        List<double[]> matches = new List<double[]>();
                        await QueuedTask.Run(() =>
                        {
                            matches = PatternSolver.solveBestMatches(transects[i].patternVecs, modelParams, j, compareType, weightType, spacing,
                                                                         Math.Ceiling(transects[i].lengthAbove / spacing) * spacing,
                                                                         Math.Ceiling(transects[i].lengthBelow / spacing) * spacing);
                        });

                        if (!loadingBarWindow.IsLoaded)
                        {
                            matchLock = false;
                            return;
                        }

                        if (matches.Count > 0)
                        {
                            transects[i].results.Add((((ListBoxItem)modelTypeListView.Items[j]).Content.ToString(), matches));
                            double avgSwirl = Math.Round(matches.Average(x => x[1]/x[2]), 2);
                            double[] minMatch = matches.MinBy(x => x[7]);
                            double bestError = Math.Round(matches[0][0], 5);

                            double minVel = Math.Round(minMatch[7]);
                            double error = (1 - bestError);
                            error *= error;
                            swirlSum += avgSwirl * error;
                            minVelSum += minMatch[7] * error;
                            bestErrorSum += error;

                            string bestParams = string.Format("{0}, {1}, {2}, {3}, {4}, {5}", matches[0][2], matches[0][1], matches[0][3], matches[0][4], matches[0][5], Math.Round(matches[0][6], 2));
                            string minParams = string.Format("{0}, {1}, {2}, {3}, {4}, {5}", minMatch[2], minMatch[1], minMatch[3], minMatch[4], minMatch[5], Math.Round(minMatch[6], 2));

                            results.Add(new MatchingResult(((ListBoxItem)modelTypeListView.Items[j]).Content.ToString(), avgSwirl.ToString(), minVel + " m/s", bestError, bestParams, minParams));
                        }
                        else
                        {
                            results.Add(new MatchingResult(((ListBoxItem)modelTypeListView.Items[j]).Content.ToString(), "N/A", "N/A", 0, "x", "x"));
                            failedToMatch = true;
                        }

                        if (j == 0)
                        {
                            modelParams[15] = 1.0;
                            modelParams[16] = 1.0;
                        }

                        pbm.Value++;
                    }

                    Label label = new Label();
                    label.Content = "Transect #" + (i + 1);
                    label.Padding = new Thickness(0, 10, 0, 10);
                    label.FontWeight = FontWeights.Bold;
                    fullResultPanel.Children.Add(label);

                    results.Add(new MatchingResult("Weighted Average", Math.Round(swirlSum / bestErrorSum, 2).ToString(), Math.Round(minVelSum / bestErrorSum) + " m/s", Double.NaN, "x", "x"));

                    DataGrid dg = new DataGrid();
                    dg.ItemsSource = new List<MatchingResult>(results);

                    dg.ColumnWidth = new DataGridLength(1, DataGridLengthUnitType.Star);
                    dg.HorizontalContentAlignment = System.Windows.HorizontalAlignment.Center;
                    dg.MouseLeftButtonUp += matchSelected;
                    fullResultPanel.Children.Add(dg);

                    pbm.Value = 0;
                    pb.Value++;
                }

                if (failedToMatch)
                {
                    MessageBox.Show("FAILED TO MATCH PATTERN\n\n" +
                                "Failed to find adequete matches for some transects consider\n" +
                                "increasing the match tolerance or fitting better patterns");
                }

                loadingBarWindow.Close();

                headerTabs.SelectedIndex = 4;
            }
            catch (Exception ex)
            {
                MessageBox.Show("An unkown error has occurred\n\nFunction: runAllPatternMatching\n\nError:\n\n" + ex.Message);
            }
            matchLock = false;
        }

        private void matchSelected(object sender, MouseButtonEventArgs e)
        {
            try
            {
                DataGrid dg = sender as DataGrid;

                int tIdx = fullResultPanel.Children.IndexOf(dg) / 2;

                MatchingResult mr = dg.SelectedItem as MatchingResult;

                if (mr == null) return;

                int modelType = -1;

                for (int i = 0; i < modelTypeListView.Items.Count; i++)
                {
                    if (((ListBoxItem)modelTypeListView.Items[i]).Content.ToString() == mr.Model)
                    {
                        modelType = i;
                    }
                }

                if (modelType < 0) return;

                string[] matchParams = mr.BestParams.Split(", ");
                double[] match = new double[matchParams.Length];

                for (int i = 0; i < matchParams.Length; i++)
                {
                    try
                    {
                        match[i] = double.Parse(matchParams[i], CultureInfo.InvariantCulture);
                    }
                    catch (Exception ex)
                    {
                        return;
                    }
                }

                centerIdx = (int)Math.Ceiling(transects[tIdx].lengthAbove / lastRunSpacing);

                var bestMatch = PatternSolver.getPattern(new double[] { match[1], match[0], match[2], match[3], match[4], match[5] }, modelType, lastRunSpacing, new double[] { transects[tIdx].patternVecs[centerIdx * 2], transects[tIdx].patternVecs[centerIdx * 2 + 1] });

                fullMatchedPatternPlot.Plot.Clear();

                var patternField = fullMatchedPatternPlot.Plot.AddVectorFieldList();
                //patternField.Color = System.Drawing.Color.DarkRed;
                patternField.ArrowStyle.ScaledArrowheads = true;

                double above = Math.Ceiling(transects[tIdx].lengthAbove / lastRunSpacing) * lastRunSpacing;

                for (int i = 0; i < transects[tIdx].patternVecs.Count() / 2; i++)
                {
                    patternField.RootedVectors.Add((new Coordinate(0, above - i * lastRunSpacing), new CoordinateVector(transects[tIdx].patternVecs[i * 2] * lastRunSpacing, transects[tIdx].patternVecs[i * 2 + 1] * lastRunSpacing)));
                }

                var matchedPatternField = fullMatchedPatternPlot.Plot.AddVectorFieldList();
                matchedPatternField.Color = System.Drawing.Color.DarkRed;
                matchedPatternField.ArrowStyle.ScaledArrowheads = true;

                foreach (double[] v in bestMatch)
                {
                    matchedPatternField.RootedVectors.Add((new Coordinate(0, v[1]), new CoordinateVector(v[2] * lastRunSpacing, v[3] * lastRunSpacing)));
                }

                rescaleMatchedPatternPlot(fullMatchedPatternPlot, null);
                fullMatchedPatternPlot.Refresh();
            }
            catch (Exception ex)
            {
                MessageBox.Show("An unkown error has occurred\n\nFunction: matchSelected\n\nError:\n\n" + ex.Message);
            }
        }

        private void ExportFullResults(object sender, RoutedEventArgs e)
        {
            try { 
                if (fullResultPanel.Children.IsNullOrEmpty()) return;

                System.Windows.Forms.SaveFileDialog saveDialog = new();
            
                saveDialog.FileName = Project.Current.Name.Split(".")[0] + "_Transect_Analysis";
                saveDialog.DefaultExt = ".csv";
                saveDialog.AddExtension = true;
                saveDialog.Filter = "CSV |*.csv|Excel |*.xlsx";

                try
                {
                    if (saveDialog.ShowDialog() == System.Windows.Forms.DialogResult.OK && saveDialog.FileName.Trim() != "")
                    {
                        var strParts = saveDialog.FileName.Split(".");

                        //export as unformatted csv
                        if (strParts[strParts.Length - 1] == "csv")
                        {
                            string csvData = "";

                            for (int i = 1; i < fullResultPanel.Children.Count; i += 2)
                            {

                                Transect t = transects[i / 2];

                                Label label = fullResultPanel.Children[i - 1] as Label;
                                csvData += label.Content.ToString() + "\n";

                                csvData += "X, Y, Width, Length Above, Length Below, Angle, Position Offset, Height Offset, Angle Offset\n";

                                var center = t.getCenter();
                                csvData += String.Format("{0},{1},{2},{3},{4},{5},{6},{7},{8}\n", center.Item1, center.Item2, t.width, t.lengthAbove, t.lengthBelow, t.theta, t.positionOffset, t.heightOffset, t.angleOffset);

                                DataGrid dg = fullResultPanel.Children[i] as DataGrid;

                                dg.SelectAllCells();

                                dg.ClipboardCopyMode = DataGridClipboardCopyMode.IncludeHeader;
                                ApplicationCommands.Copy.Execute(null, dg);

                                dg.UnselectAllCells();

                                csvData += (string)System.Windows.Clipboard.GetData(System.Windows.DataFormats.CommaSeparatedValue);
                                csvData += "\n";
                            }
                            File.WriteAllText(saveDialog.FileName, csvData, UnicodeEncoding.UTF8);
                        }
                        //export as excel spread sheet
                        else if (strParts[strParts.Length - 1] == "xlsx")
                        {

                            var excelApp = new Excel.Application();
                            var workbooks = excelApp.Workbooks;
                            workbooks.Add();
                            Excel._Worksheet workSheet = (Excel.Worksheet)excelApp.ActiveSheet;

                            Excel.Style transectHeaderStyle = excelApp.ActiveWorkbook.Styles.Add("THS");
                            transectHeaderStyle.Font.Size = 14;
                            transectHeaderStyle.Font.Bold = true;
                            transectHeaderStyle.HorizontalAlignment = Excel.XlHAlign.xlHAlignCenter;

                            Excel.Style headerStyle = excelApp.ActiveWorkbook.Styles.Add("HS");
                            headerStyle.Font.Size = 12;
                            headerStyle.Font.Bold = true;
                            headerStyle.HorizontalAlignment = Excel.XlHAlign.xlHAlignCenter;

                            Excel.Style bodyStyle = excelApp.ActiveWorkbook.Styles.Add("BS");
                            bodyStyle.Font.Size = 12;
                            bodyStyle.HorizontalAlignment = Excel.XlHAlign.xlHAlignCenter;

                            int row = 1;

                            for (int i = 1; i < fullResultPanel.Children.Count; i += 2)
                            {
                                int startRow = row;

                                Transect t = transects[i / 2];

                                Label label = fullResultPanel.Children[i - 1] as Label;
                                workSheet.Cells[row, 1] = label.Content.ToString();
                                workSheet.Range[workSheet.Cells[row, 1], workSheet.Cells[row, 9]].merge();
                                workSheet.Range[workSheet.Cells[row, 1], workSheet.Cells[row, 9]].Style = "THS";
                                workSheet.Range[workSheet.Cells[row, 1], workSheet.Cells[row, 9]].Borders[Excel.XlBordersIndex.xlEdgeBottom].Weight = 4.0;
                                row++;

                                var center = t.getCenter();
                                workSheet.Cells[row, 1] = "X";
                                workSheet.Cells[row, 2] = "Y";
                                workSheet.Cells[row, 3] = "Width";
                                workSheet.Cells[row, 4] = "Length Above";
                                workSheet.Cells[row, 5] = "Length Below";
                                workSheet.Cells[row, 6] = "Angle";
                                workSheet.Cells[row, 7] = "Position Offset";
                                workSheet.Cells[row, 8] = "Height Offset";
                                workSheet.Cells[row, 9] = "Angle Offset";
                                workSheet.Range[workSheet.Cells[row, 1], workSheet.Cells[row, 9]].Style = "HS";
                                workSheet.Range[workSheet.Cells[row, 1], workSheet.Cells[row, 9]].Borders[Excel.XlBordersIndex.xlEdgeBottom].Weight = 2.0;
                                row++;

                                workSheet.Cells[row, 1] = center.Item1;
                                workSheet.Cells[row, 2] = center.Item2;
                                workSheet.Cells[row, 3] = t.width;
                                workSheet.Cells[row, 4] = t.lengthAbove;
                                workSheet.Cells[row, 5] = t.lengthBelow;
                                workSheet.Cells[row, 6] = Math.Round(t.theta, 5);
                                workSheet.Cells[row, 7] = t.positionOffset;
                                workSheet.Cells[row, 8] = t.heightOffset;
                                workSheet.Cells[row, 9] = t.angleOffset;
                                workSheet.Range[workSheet.Cells[row, 1], workSheet.Cells[row, 9]].Style = "BS";
                                workSheet.Range[workSheet.Cells[row, 1], workSheet.Cells[row, 9]].Borders[Excel.XlBordersIndex.xlEdgeBottom].Weight = 2.0;
                                row++;

                                workSheet.Cells[row, 1] = "Model";
                                workSheet.Cells[row, 2] = "Min Velocity";
                                workSheet.Cells[row, 3] = "Best Error";
                                workSheet.Cells[row, 4] = "Best Parameters\n(Vr, Vt, Vs, Vc, Rmax, Phi)  ";
                                workSheet.Cells[row, 5] = "Min Parameters\n(Vr, Vt, Vs, Vc, Rmax, Phi)   ";
                                workSheet.Range[workSheet.Cells[row, 1], workSheet.Cells[row, 5]].Style = "HS";
                                workSheet.Range[workSheet.Cells[row, 1], workSheet.Cells[row, 5]].Borders[Excel.XlBordersIndex.xlEdgeBottom].Weight = 2.0;

                                DataGrid dg = fullResultPanel.Children[i] as DataGrid;
                                var matchResults = dg.ItemsSource as List<MatchingResult>;

                                double bestError = 9999999999.0;
                                int bestIdx = 0;

                                foreach (var result in matchResults)
                                {
                                    if (!Double.IsNaN(result.BestError) && result.BestError < bestError)
                                    {
                                        bestError = result.BestError;
                                        bestIdx = matchResults.IndexOf(result);
                                    }
                                }
                                workSheet.Range[workSheet.Cells[row, 6], workSheet.Cells[row + matchResults.Count, 9]].merge();
                                workSheet.Columns.AutoFit();

                                dg.SelectedIndex = bestIdx;
                                matchSelected(dg, null);
                                var split = saveDialog.FileName.Split('\\');
                                split = split.Take(split.Count() - 1).ToArray();
                                string path = String.Join("\\", split) + "\\";

                                fullMatchedPatternPlot.Plot.SaveFig(path + "temp.png");

                                float w = 0.0f;

                                for (int j = 6; j <= 9; j++)
                                {
                                    w += (float)workSheet.Cells[row, j].Width;
                                }
                                w /= 2.0f;

                                float h = 310.0f;

                                workSheet.Shapes.AddPicture(path + "temp.png", Microsoft.Office.Core.MsoTriState.msoFalse, Microsoft.Office.Core.MsoTriState.msoCTrue, (float)workSheet.Cells[row, 6].Left + w / 2.0f, (float)workSheet.Cells[row, 6].Top + 5, w, h);

                                row++;

                                foreach (var result in matchResults)
                                {
                                    if (!Double.IsNaN(result.BestError) && result.BestError < bestError)
                                    {
                                        bestError = result.BestError;
                                        bestIdx = matchResults.IndexOf(result);
                                    }

                                    workSheet.Cells[row, 1] = result.Model;
                                    workSheet.Cells[row, 2] = result.MinVelocity;
                                    workSheet.Cells[row, 3] = Double.IsNaN(result.BestError) ? "x" : result.BestError;
                                    workSheet.Cells[row, 4] = result.BestParams + "          ";
                                    workSheet.Cells[row, 5] = result.MinParams + "          ";
                                    workSheet.Range[workSheet.Cells[row, 1], workSheet.Cells[row, 5]].Style = "BS";
                                    row++;
                                }
                                row++;

                                //workSheet.Range[workSheet.Cells[startRow, 1], workSheet.Cells[row, 9]].Columns.AutoFit();
                                workSheet.Columns.AutoFit();
                                workSheet.Range[workSheet.Cells[row - 1 - matchResults.Count, 1], workSheet.Cells[row - 2, 1]].Borders[Excel.XlBordersIndex.xlEdgeRight].Weight = 2.0;
                                workSheet.Range[workSheet.Cells[row - 2 - matchResults.Count, 5], workSheet.Cells[row - 2, 5]].Borders[Excel.XlBordersIndex.xlEdgeRight].Weight = 2.0;
                                workSheet.Range[workSheet.Cells[row - 3, 1], workSheet.Cells[row - 3, 5]].Borders[Excel.XlBordersIndex.xlEdgeBottom].Weight = 1.0;
                                workSheet.Range[workSheet.Cells[row - 3, 1], workSheet.Cells[row - 3, 5]].Borders[Excel.XlBordersIndex.xlEdgeBottom].LineStyle = Excel.XlLineStyle.xlDouble;
                                workSheet.Range[workSheet.Cells[startRow, 1], workSheet.Cells[row, 9]].Rows.RowHeight = 40;
                                //workSheet.Range[workSheet.Cells[startRow, 1], workSheet.Cells[row, 9]].Style.HorizontalAlignment = Excel.XlHAlign.xlHAlignCenter;
                                workSheet.Range[workSheet.Cells[startRow, 1], workSheet.Cells[row - 2, 9]].Borders[Excel.XlBordersIndex.xlEdgeRight].Weight = 2.0;
                                workSheet.Range[workSheet.Cells[startRow, 1], workSheet.Cells[row - 2, 9]].Borders[Excel.XlBordersIndex.xlEdgeLeft].Weight = 2.0;
                                workSheet.Range[workSheet.Cells[startRow, 1], workSheet.Cells[row - 2, 9]].Borders[Excel.XlBordersIndex.xlEdgeTop].Weight = 2.0;
                                workSheet.Range[workSheet.Cells[startRow, 1], workSheet.Cells[row - 2, 9]].Borders[Excel.XlBordersIndex.xlEdgeBottom].Weight = 2.0;
                            }

                            workSheet.SaveAs(saveDialog.FileName);
                            workbooks.Close();
                            excelApp.Quit();

                        }
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Failed to Export Image\nError:\n\n" + ex.Message);
                }

                MessageBox.Show("Export Complete");
            }
            catch(Exception ex)
            {
                MessageBox.Show("An unkown error has occurred\n\nFunction: ExportFullResults\n\nError:\n\n" + ex.Message);
            }
        }

        private void ExportFullMatches(object sender, RoutedEventArgs e)
        {
            try
            {
                if (fullResultPanel.Children.IsNullOrEmpty()) return;

                System.Windows.Forms.SaveFileDialog saveDialog = new();

                saveDialog.FileName = Project.Current.Name.Split(".")[0] + "_Transect_Matches";
                saveDialog.DefaultExt = ".csv";
                saveDialog.AddExtension = true;
                saveDialog.Filter = "CSV |*.csv|Excel |*.xlsx";

                if (saveDialog.ShowDialog() == System.Windows.Forms.DialogResult.OK && saveDialog.FileName.Trim() != "")
                {
                    var strParts = saveDialog.FileName.Split(".");

                    //export as unformatted csv
                    if (strParts[strParts.Length - 1] == "csv")
                    {
                        //string csvData = "";
                        var sw = new StreamWriter(saveDialog.FileName, false);

                        for (int i = 1; i < fullResultPanel.Children.Count; i += 2)
                        {

                            Transect t = transects[i / 2];

                            Label label = fullResultPanel.Children[i - 1] as Label;
                            sw.WriteLine(label.Content.ToString());

                            sw.WriteLine("X, Y, Width, Length Above, Length Below, Angle, Position Offset, Height Offset, Angle Offset");

                            var center = t.getCenter();
                            sw.WriteLine(String.Format("{0},{1},{2},{3},{4},{5},{6},{7},{8}\n", center.Item1, center.Item2, t.width, t.lengthAbove, t.lengthBelow, t.theta, t.positionOffset, t.heightOffset, t.angleOffset));

                            foreach (var r in t.results)
                            {
                                sw.WriteLine("Model," + r.Item1);
                                sw.WriteLine("Error,Vt,Vr,Vs,Vc,Rmax,Phi,Vmax");
                                foreach (var m in r.Item2)
                                {
                                    sw.WriteLine(String.Format("{0},{1},{2},{3},{4},{5},{6},{7}", m[0], m[1], m[2], m[3], m[4], m[5], m[6], m[7]));
                                }
                                sw.WriteLine("\n");
                            }
                        }
                        //File.WriteAllText(saveDialog.FileName, csvData, UnicodeEncoding.UTF8);
                        sw.Close();

                        MessageBox.Show("export successful");
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error exporting matches\n\nFunction: ExportFullMatches\n\nError:\n\n" + ex.Message);
            }
        }
    }
}
