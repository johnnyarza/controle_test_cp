using System.Collections.Generic;

namespace ProbetaControl.Models
{
    /// <summary>Active records that point at a row (e.g. designs using a material), for "in use" messages.</summary>
    public sealed class RecordUsage
    {
        /// <summary>How many names a message lists before "y N más".</summary>
        public const int MaxExamples = 5;

        public RecordUsage(int count, IReadOnlyList<string> examples)
        {
            Count = count;
            Examples = examples;
        }

        public int Count { get; }

        /// <summary>Up to <see cref="MaxExamples"/> names.</summary>
        public IReadOnlyList<string> Examples { get; }

        public bool IsInUse => Count > 0;
    }
}
