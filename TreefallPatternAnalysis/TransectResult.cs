using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TreefallPatternAnalysis
{
    internal class TransectResult
    {
        public string TransectNumber { get; set; }
        public List<MatchingResult> Results { get; set; } = new List<MatchingResult>();

        public TransectResult(string transectNumber, List<MatchingResult> results)
        {
            TransectNumber = transectNumber;
            Results = results;
        }
    }
}
