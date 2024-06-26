using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TreefallPatternAnalysis
{
    internal class MatchingResult
    {
        public string Model { get; set; }
        public string AvgSwirl { get; set; }
        public string MinVelocity { get; set; }
        public double BestError { get; set; }
        public string BestParams { get; set; }
        public string MinParams { get; set; }

        public MatchingResult(string model, string avg_swirl, string min_Velocity, double best_Error, string best_Params, string min_Params)
        {
            Model = model;
            AvgSwirl = avg_swirl;
            MinVelocity = min_Velocity;
            BestError = best_Error;
            BestParams = best_Params;
            MinParams = min_Params;
        }
    }
}
