using ArcGIS.Core.Data.UtilityNetwork.Trace;
using ScottPlot;
using ScottPlot.Drawing.Colormaps;
using ScottPlot.Statistics;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.DirectoryServices;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace TreefallPatternAnalysis
{
    public static class PatternSolver
    {
        //private static const string pathSource = System.IO.Path.GetDirectoryName(new System.Uri(Assembly.GetEntryAssembly().Location).AbsolutePath).Replace("%20", " ") + "\\TreefallAnalysis";

        [DllImport("cppBin\\PatternSolver.dll", CallingConvention = CallingConvention.Cdecl)]
        private static unsafe extern double** matchPattern(double* p, double[] modelParams, int modelType, int compareType, int weightType, double dx, double wAbove, double wBelow);
        [DllImport("cppBin\\PatternSolver.dll", CallingConvention = CallingConvention.Cdecl)]
        private static unsafe extern double** generatePattern(double[] modelParams, int modelType, double dx);
        [DllImport("cppBin\\PatternSolver.dll", CallingConvention = CallingConvention.Cdecl)]
        private static unsafe extern double*** generateField(double[] fieldParams, double[] modelParams, int modelType);
        [DllImport("cppBin\\PatternSolver.dll", CallingConvention = CallingConvention.Cdecl)]
        private static unsafe extern double* generateCurve(int n, double[] modelParams, int modelType);

        [DllImport("cppBin\\PatternSolver.dll", CallingConvention = CallingConvention.Cdecl)]
        private static unsafe extern double** generatePatternLP(double[] modelParams, double[] lineData, int lineDataSize, double dx);
        [DllImport("cppBin\\PatternSolver.dll", CallingConvention = CallingConvention.Cdecl)]
        private static unsafe extern double*** generateFieldLP(double[] fieldParams, double[] modelParams, double[] lineData, int lineDataSize);
        [DllImport("cppBin\\PatternSolver.dll", CallingConvention = CallingConvention.Cdecl)]
        private static unsafe extern double* generateCurveLP(int n, double[] modelParams, double[] lineData, int lineDataSize);

        [DllImport("cppBin\\PatternSolver.dll", CallingConvention = CallingConvention.Cdecl)]
        private static unsafe extern double* freeMemory(double* p);
        [DllImport("cppBin\\PatternSolver.dll", CallingConvention = CallingConvention.Cdecl)]
        private static unsafe extern double* freeMemory2(double** p, int n);
        [DllImport("cppBin\\PatternSolver.dll", CallingConvention = CallingConvention.Cdecl)]
        private static unsafe extern double* freeMemory3(double*** p, int n, int m);

        public record Field(double[,] m, Vector2[,] v, double[] x, double[] y)
        {
            public double[,] magnitudes { get; init; } = m;
            public Vector2[,] unitVecs { get; init; } = v;
            public double[] xPositions { get; init; } = x;
            public double[] yPositions { get; init; } = y;
        }

        public static unsafe Field getField(double[] fieldParams, double[] modelParams, int modelType)
        {
            double wx = fieldParams[2] - fieldParams[0];
            double wy = fieldParams[3] - fieldParams[1];
            double dx = fieldParams[4];

            double[,] magnitudes = new double[(int)Math.Floor(wy / dx) + 1, (int)Math.Floor(wx / dx) + 1];
            Vector2[,] unitVecs = new Vector2[(int)Math.Floor(wx / dx) + 1, (int)Math.Floor(wy / dx) + 1];
            double[] xPositions = new double[(int)Math.Floor(wx / dx) + 1];
            double[] yPositions = new double[(int)Math.Floor(wy / dx) + 1];

            unsafe
            {
                double*** field_ptr = generateField(fieldParams, modelParams, modelType);

                for(int i = 0; i < (int)Math.Floor(wy / dx) + 1; i++)
                {
                    yPositions[i] = field_ptr[i][0][1];
                    for (int j = 0; j < (int)Math.Floor(wx / dx) + 1; j++)
                    {
                        magnitudes[i, j] = field_ptr[i][j][4];
                        unitVecs[j, i] = new Vector2(field_ptr[i][j][2], field_ptr[i][j][3]);

                        if (i != 0) continue;

                        xPositions[j] = field_ptr[0][j][0];
                    }
                }

                freeMemory3(field_ptr, (int)Math.Floor(wy / dx) + 1, (int)Math.Floor(wx / dx) + 1);
            }

            return new Field(magnitudes, unitVecs, xPositions, yPositions);
        }

        public static unsafe Field getFieldLP(double[] fieldParams, double[] modelParams, double[] lineData, int lineDataSize)
        {
            double wx = fieldParams[2] - fieldParams[0];
            double wy = fieldParams[3] - fieldParams[1];
            double dx = fieldParams[4];

            double[,] magnitudes = new double[(int)Math.Floor(wy / dx) + 1, (int)Math.Floor(wx / dx) + 1];
            Vector2[,] unitVecs = new Vector2[(int)Math.Floor(wx / dx) + 1, (int)Math.Floor(wy / dx) + 1];
            double[] xPositions = new double[(int)Math.Floor(wx / dx) + 1];
            double[] yPositions = new double[(int)Math.Floor(wy / dx) + 1];

            unsafe
            {
                double*** field_ptr = generateFieldLP(fieldParams, modelParams, lineData, lineDataSize);

                for (int i = 0; i < (int)Math.Floor(wy / dx) + 1; i++)
                {
                    yPositions[i] = field_ptr[i][0][1];
                    for (int j = 0; j < (int)Math.Floor(wx / dx) + 1; j++)
                    {
                        magnitudes[i, j] = field_ptr[i][j][4];
                        unitVecs[j, i] = new Vector2(field_ptr[i][j][2], field_ptr[i][j][3]);

                        if (i != 0) continue;

                        xPositions[j] = field_ptr[0][j][0];
                    }
                }

                freeMemory3(field_ptr, (int)Math.Floor(wy / dx) + 1, (int)Math.Floor(wx / dx) + 1);
            }

            return new Field(magnitudes, unitVecs, xPositions, yPositions);
        }

        public static unsafe List<double[]> solveBestMatches(List<double> p, double[] modelParams, int modelType, int compareType, int weightType, double dx, double wAbove, double wBelow)
        {
            double[] rp = reverseAndRotateCCW(p).ToArray();

            List<double[]> matches;

            unsafe
            {
                fixed (double* rp_ptr = rp)
                {
                    double** matches_ptr = matchPattern(rp_ptr, modelParams, modelType, compareType, weightType, dx, wAbove, wBelow);

                    matches = parseNanTerminatedDoubleArray(matches_ptr, 8);

                    //freeMemory2(matches_ptr, matches.Count + 1);
                }
            }

            return matches;
        }

        public static unsafe List<double[]> getPattern(double[] modelParams, int modelType, double dx, bool rotate = true)
        {
            List<double[]> pattern;

            unsafe
            {
                double** pattern_ptr = generatePattern(modelParams, modelType, dx);

                pattern = parseNanTerminatedDoubleArray(pattern_ptr, 4);

                //get axis of convergence
                double c = pattern[pattern.Count - 1][0];
                pattern.RemoveAt(pattern.Count - 1);

                if (rotate)
                {
                    pattern = reverseAndRotateCW(pattern);

                    for (int i = 0; i < pattern.Count(); i++)
                    {
                        pattern[i][1] += c;
                    }
                }
                 
                freeMemory2(pattern_ptr, pattern.Count + 2);
            }

            return pattern;
        }

        public static unsafe (List<double[]>, double) getPatternLP(double[] modelParams, double[] lineData, int lineDataSize, double dx, bool rotate = true)
        {
            List<double[]> pattern;
            double w;

            unsafe
            {
                double** pattern_ptr = generatePatternLP(modelParams, lineData, lineDataSize, dx);

                pattern = parseNanTerminatedDoubleArray(pattern_ptr, 4);

                if (pattern.Count == 0) return (new List<double[]>(), 0.0);

                //get axis of convergence
                double c = pattern[pattern.Count - 1][0];

                w = pattern[pattern.Count - 1][2] - pattern[pattern.Count - 1][1];
                pattern.RemoveAt(pattern.Count - 1);

                if (rotate)
                {
                    pattern = reverseAndRotateCW(pattern);

                    for (int i = 0; i < pattern.Count(); i++)
                    {
                        pattern[i][1] += c;
                    }
                }

                freeMemory2(pattern_ptr, pattern.Count + 2);
            }

            return (pattern, w);
        }

        public static unsafe List<double[]> getPattern(double[] modelParams, int modelType, double dx, double[] centerVec)
        {
            List<double[]> pattern = getPattern(modelParams, modelType, dx);

            for(int i = 1; i < pattern.Count; i++)
            {
                if (Math.Abs(pattern[i][1] - pattern[i - 1][1]) > 1.0) continue;

                double d1 = pattern[i][2] * centerVec[0] + pattern[i][3] * centerVec[1];
                double d2 = pattern[i-1][2] * centerVec[0] + pattern[i-1][3] * centerVec[1];

                pattern.RemoveAt(d1 > d2 ? i - 1 : i);

                //System.Diagnostics.Debug.WriteLine("************** " + centerVec[0] + ", " + centerVec[1]);
                
                break;
            }

            return pattern;
        }


        public static unsafe (double[], double[]) getCurve(int n, double[] modelParams, int modelType)
        {
            List<double> xs = [];
            List<double> ys = [];

            unsafe
            {
                double* curve_ptr = generateCurve(n, modelParams, modelType);

                int i = 0;

                while (!IsNaN(curve_ptr[i]))
                {
                    xs.Add(curve_ptr[i]);
                    ys.Add(curve_ptr[i + 1]);
                    i += 2;
                }

                freeMemory(curve_ptr);
            }

            return (xs.ToArray(), ys.ToArray());
        }

        public static unsafe (double[], double[]) getCurveLP(int n, double[] modelParams, double[] lineData, int lineDataSize)
        {
            List<double> xs = [];
            List<double> ys = [];

            unsafe
            {
                double* curve_ptr = generateCurveLP(n, modelParams, lineData, lineDataSize);

                int i = 0;

                while (!IsNaN(curve_ptr[i]))
                {
                    xs.Add(curve_ptr[i]);
                    ys.Add(curve_ptr[i + 1]);
                    i += 2;
                }

                freeMemory(curve_ptr);
            }

            if (xs.Count == 0)
            {
                xs.Add(0.0);
                ys.Add(0.0);
            }

            return (xs.ToArray(), ys.ToArray());
        }


        /*public static unsafe double getConvergence(double[] p)
        {
            unsafe
            {
                fixed (double* p_ptr = p)
                {
                    return getConvergenceRankine(p_ptr);
                }
            }
        }*/

        public static List<double> reverseAndRotateCCW(List<double> farray)
        {
            List<double> newfarray = new List<double>(new double[farray.Count()]);

            for (int i = 0; i < farray.Count() / 2; i++)
            {
                newfarray[i * 2] = -farray[i * 2 + 1];
                newfarray[i * 2 + 1] = farray[i * 2];

            }

            return newfarray;
        }

        public static List<double[]> reverseAndRotateCW(List<double[]> farray)
        {
            List<double[]> newfarray = new List<double[]>();
            for(int i = 0; i < farray.Count(); i++)
            {
                newfarray.Add(new double[4] { farray[i][1], -farray[i][0], farray[i][3], -farray[i][2] });
            }

            return newfarray;
        }


        /*public static unsafe List<double> reverse(List<double> fList) {

            fList.Add(double.NaN);

            unsafe
            {
                fixed (double* fListptr = fList.ToArray())
                {
                    double* reversed = reverseFloatArray(fListptr);

                    fList.Clear();

                    int i = 0;

                    while (!IsNaN(reversed[i]))
                    {
                        fList.Add(reversed[i]);
                        i++;
                    }
                }
            }

            return fList;
        }*/

        private static unsafe bool IsNaN(double d)
        {
            /*int binary = *(int*)(&f);
            return ((binary & 0x7F800000) == 0x7F800000) && ((binary & 0x007FFFFF) != 0);*/
            return (*(long*)(&d) & 0x7FF0000000000000L) == 0x7FF0000000000000L;
        }

        private static unsafe List<double[]> parseNanTerminatedDoubleArray(double** array_ptr, int size)
        {
            List<double[]> parsedArray = new List<double[]>();

            int i = 0;
            
            while (!IsNaN(array_ptr[i][0]))
            {
                List<double> doubles = new List<double>();

                for(int j = 0; j < size; j++)
                {
                    doubles.Add(array_ptr[i][j]);
                }

                parsedArray.Add(doubles.ToArray());

                i++;
            }

            return parsedArray;
        }
    }
}
