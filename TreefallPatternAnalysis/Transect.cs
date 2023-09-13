using ScottPlot;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TreefallPatternAnalysis
{
    class Transect
    {
        public double x, y, theta, lengthAbove, lengthBelow, width, positionOffset, heightOffset, angleOffset;
        public List<double> patternVecs = new List<double>();
        public List<double[]> matches = new List<double[]>();
        public List<(string, List<double[]>)> results = new List<(string, List<double[]>)>();
        public ScottPlot.Plottable.Polygon box;
        private ScottPlot.Plottable.MarkerPlot marker;
        private ScottPlot.Plottable.ScatterPlot perpLine;


        public Transect(double x, double y, double lengthAbove, double lengthBelow, double width)
        {
            this.x = x;
            this.y = y;
            this.lengthAbove = lengthAbove;
            this.lengthBelow = lengthBelow;
            this.width = width;
            this.heightOffset = 0;
            this.angleOffset = 0;
            this.positionOffset = 0;
        }

        public Transect()
        {
            this.x = 0;
            this.y = 0;
            this.theta = 0;
            this.lengthAbove = 0;
            this.lengthBelow = 0;
            this.width = 0;
            this.heightOffset = 0;
            this.angleOffset = 0; 
            this.positionOffset = 0;
        }

        public void setOutlineColor(System.Drawing.Color c)
        {
            box.LineColor = c;
        }

        public void updateTransect()
        {
            var (centerX, centerY) = getCenter();
            marker.X = centerX;
            marker.Y = centerY;

            (double[], double[]) pts = createTransectPerpLine();

            perpLine.Update(pts.Item1, pts.Item2);

            pts = createTransectBox();

            box.Xs = pts.Item1;
            box.Ys = pts.Item2;
        }

        public void renderTransect(Plot plt)
        {

            (double[], double[]) pts = createTransectPerpLine();

            perpLine = plt.AddScatterLines(pts.Item1, pts.Item2, color: System.Drawing.Color.DarkMagenta, lineWidth: 2);

            pts = createTransectBox();

            box = plt.AddPolygon(pts.Item1, pts.Item2, fillColor: System.Drawing.Color.FromArgb(40, 255, 0, 0), lineWidth: 3, lineColor: System.Drawing.Color.Turquoise);

            double[] pv = new double[2] { Math.Cos(this.theta), Math.Sin(this.theta) };
            marker = plt.AddMarker(this.x + pv[0] * heightOffset, this.y + pv[1] * heightOffset, shape: MarkerShape.filledCircle, color: System.Drawing.Color.Blue, size: 7);

        }

        public (double[], double[]) createTransectBox()
        {
            double[] pv = new double[2] { Math.Cos(this.theta), Math.Sin(this.theta) };
            double[] tv = new double[2] { pv[1], -pv[0] };


            double[] xs = new double[4];
            double[] ys = new double[4];

            xs[0] = this.x + pv[0] * (this.heightOffset + this.lengthAbove) + tv[0] * this.width;
            xs[1] = this.x + pv[0] * (this.heightOffset + this.lengthAbove) - tv[0] * this.width;
            xs[2] = this.x + pv[0] * (this.heightOffset - this.lengthBelow) - tv[0] * this.width;
            xs[3] = this.x + pv[0] * (this.heightOffset - this.lengthBelow) + tv[0] * this.width;

            ys[0] = this.y + pv[1] * (this.heightOffset + this.lengthAbove) + tv[1] * this.width;
            ys[1] = this.y + pv[1] * (this.heightOffset + this.lengthAbove) - tv[1] * this.width;
            ys[2] = this.y + pv[1] * (this.heightOffset - this.lengthBelow) - tv[1] * this.width;
            ys[3] = this.y + pv[1] * (this.heightOffset - this.lengthBelow) + tv[1] * this.width;

            return (xs, ys);
        }

        public (double[], double[]) createTransectPerpLine()
        {
            double[] pv = new double[2] { Math.Cos(this.theta), Math.Sin(this.theta) };

            double[] xs = new double[2];
            double[] ys = new double[2];

            xs[0] = this.x + pv[0] * (this.heightOffset + this.lengthAbove);

            xs[1] = this.x + pv[0] * (this.heightOffset - this.lengthBelow);

            ys[0] = this.y + pv[1] * (this.heightOffset + this.lengthAbove);
            ys[1] = this.y + pv[1] * (this.heightOffset - this.lengthBelow);

            return (xs, ys);
        }

        public void setPerpendicularAngle(double x1, double y1, double x2, double y2)
        {

            if (x1 == x2)
            {
                theta = Math.PI;
                return;
            }

            double a = Math.Atan2(y2 - y1, x2 - x1);

            //convert to 0 - 2PI
            a = a < 0 ? 2.0 * Math.PI - a : a;

            //rotate by 90 degrees
            a = (3.0 * Math.PI / 2.0 > a && a > Math.PI / 2.0) ? a - Math.PI / 2.0 : a + Math.PI / 2.0;

            a += (Math.PI / 180.0) * this.angleOffset;

            //clamp to 0 - 2PI
            a = a > 2 * Math.PI ? a - 2 * Math.PI : a;

            theta = a;
        }

        public (double, double) getCenter()
        {
            double[] pv = new double[2] { Math.Cos(this.theta), Math.Sin(this.theta) };

            return (this.x + pv[0] * heightOffset, this.y + pv[1] * heightOffset);
        }


    }
}
