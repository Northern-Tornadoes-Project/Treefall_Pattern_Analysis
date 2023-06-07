using ScottPlot.Drawing.Colormaps;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace TreefallPatternAnalysis
{
    public static class PatternSolver
    {

        [DllImport("F:\\PatternSolver\\x64\\Release\\PatternSolver.dll", CallingConvention = CallingConvention.Cdecl)]
        private static unsafe extern float** matchPattern(float* p, float[] modelparams, float spacing, float wAbove, float wBelow);
        [DllImport("F:\\PatternSolver\\x64\\Release\\PatternSolver.dll", CallingConvention = CallingConvention.Cdecl)]
        private static unsafe extern float** generatePattern(float* p, float spacing);
        [DllImport("F:\\PatternSolver\\x64\\Release\\PatternSolver.dll", CallingConvention = CallingConvention.Cdecl)]
        private static unsafe extern float getConvergenceRankine(float* p);

        public static unsafe List<float[]> solveBestMatches(List<float> p, float[] modelParams, float spacing, float wAbove, float wBelow)
        {
            float[] rp = reverseAndRotateCCW(p).ToArray();

            List<float[]> matches;

            unsafe
            {
                fixed (float* rp_ptr = rp)
                {
                    float** matches_ptr = matchPattern(rp_ptr, modelParams, spacing, wAbove, wBelow);

                    matches = parseNanTerminatedFloatArray(matches_ptr, 7);

                }
            }

            return matches;
        }

        public static unsafe List<float[]> getPattern(float[] p, float spacing)
        {
            List<float[]> pattern;

            unsafe
            {
                fixed (float* p_ptr = p)
                {
                    float** pattern_ptr = generatePattern(p_ptr, spacing);

                    pattern = parseNanTerminatedFloatArray(pattern_ptr, 4);

                    pattern = reverseAndRotateCW(pattern);

                    float c = getConvergence(p);

                    for(int i = 0; i < pattern.Count(); i++)
                    {
                        pattern[i][1] += c;
                    }
                }
            }

            

            return pattern;
        }

        public static unsafe List<float[]> getPattern(float[] p, float spacing, float[] centerVec)
        {
            List<float[]> pattern = getPattern(p, spacing);

            for(int i = 1; i < pattern.Count; i++)
            {
                if (Math.Abs(pattern[i][1] - pattern[i - 1][1]) > 1.0f) continue;

                float d1 = pattern[i][2] * centerVec[0] + pattern[i][3] * centerVec[1];
                float d2 = pattern[i-1][2] * centerVec[0] + pattern[i-1][3] * centerVec[1];

                pattern.RemoveAt(d1 > d2 ? i - 1 : i);

                //System.Diagnostics.Debug.WriteLine("***REMOVED***");
                
                break;
            }

            return pattern;
        }

        public static unsafe float getConvergence(float[] p)
        {
            unsafe
            {
                fixed (float* p_ptr = p)
                {
                    return getConvergenceRankine(p_ptr);
                }
            }
        }

        public static List<float> reverseAndRotateCCW(List<float> farray)
        {
            List<float> newfarray = new List<float>(new float[farray.Count()]);

            for (int i = 0; i < farray.Count() / 2; i++)
            {
                newfarray[farray.Count() - i * 2 - 2] = -farray[i * 2 + 1];
                newfarray[farray.Count() - i * 2 - 1] = farray[i * 2];

            }

            return newfarray;
        }

        public static List<float[]> reverseAndRotateCW(List<float[]> farray)
        {
            List<float[]> newfarray = new List<float[]>();
            for(int i = farray.Count() - 1; i >= 0; i--)
            {
                newfarray.Add(new float[4] { farray[i][1], -farray[i][0], farray[i][3], -farray[i][2] });
            }

            return newfarray;
        }


        /*public static unsafe List<float> reverse(List<float> fList) {

            fList.Add(float.NaN);

            unsafe
            {
                fixed (float* fListptr = fList.ToArray())
                {
                    float* reversed = reverseFloatArray(fListptr);

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

        private static unsafe bool IsNaN(float f)
        {
            int binary = *(int*)(&f);
            return ((binary & 0x7F800000) == 0x7F800000) && ((binary & 0x007FFFFF) != 0);
        }

        private static unsafe List<float[]> parseNanTerminatedFloatArray(float** array_ptr, int size)
        {
            List<float[]> parsedArray = new List<float[]>();

            int i = 0;
            
            while (!IsNaN(array_ptr[i][0]))
            {
                List<float> floats = new List<float>();

                for(int j = 0; j < size; j++)
                {
                    floats.Add(array_ptr[i][j]);
                }

                parsedArray.Add(floats.ToArray());

                i++;
            }

            return parsedArray;
        }
    }
}
