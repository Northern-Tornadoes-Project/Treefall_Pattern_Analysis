using ScottPlot;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


namespace TreefallPatternAnalysis
{
    
    public class ModelProfile
    {
        public string modelType;
        public string profileType;
        public LineStyle profileStyle;
        public ScottPlot.Plottable.FunctionPlot plot;
        public double[] plotArgs;

        public ModelProfile(string modelType, string profileType, string profileStyle, double[] args)
        {
            this.modelType = modelType.ToLower();
            this.profileType = profileType.ToLower();
            plotArgs = args;

            this.profileStyle = profileStyle.ToLower() switch
            {
                "solid"         => LineStyle.Solid,
                "dash"          => LineStyle.Dash,
                "dot"           => LineStyle.Dot,
                "dashdot"       => LineStyle.DashDot,
                "dashdotdot"    => LineStyle.DashDotDot,
                _               => LineStyle.Solid,
            };
        }

        public void setPlotArgs(double[] args)
        {
            this.plotArgs = args;
        }


        private Func<double, double?> generateFunc()
        {
            if(profileType == "vc" || profileType == "vs")
            {
                return new Func<double, double?>((x) => plotArgs[0]);
            }

            return modelType switch
            {
                "modified-rankine" => new Func<double, double?>((x) =>
                    {
                        if (x < -0.01) return double.NaN;

                        if (x <= 1.0) return plotArgs[0] * Math.Pow(x, plotArgs[1]);

                        return plotArgs[0] * Math.Pow(1 / x, plotArgs[1]);

                    }),
                "baker-sterling" => new Func<double, double?>((x) =>
                    {
                        if (x < -0.01) return double.NaN;

                        return plotArgs[0] * (2.0 * x / (1 + x * x));

                    })
            };
        }

        public void addFuncPlot(ScottPlot.Plot plt)
        {
            var func = generateFunc();

            plot = plt.AddFunction(func, lineWidth: 2.0, lineStyle: profileStyle);

            if (plotArgs.Length > 1)
            {
                plot.Label = capitializeFirstLetters(modelType, "-") + " - " + capitializeFirstLetters(profileType, "-") + " = " + plotArgs[0] + " Phi = " + plotArgs[1];
            }
            else
            {
                plot.Label = capitializeFirstLetters(modelType, "-") + " - " + capitializeFirstLetters(profileType, "-") + " = " + plotArgs[0];
            }

        }

        private string capitializeFirstLetters(string str, string delimiter = "")
        {
            string[] words = str.Split(delimiter);

            for (int i = 0; i < words.Length; i++)
            {
                words[i] = char.ToUpper(words[i][0]) + words[i][1..];
            }

            return string.Join(delimiter, words);
        }
    }
}
