using System;
using System.Collections.Generic;
using System.Text;

namespace BuilderAPI
{
    public enum PipelineProgression
    {
        Running,
        Completed,
        Skipped,
    }

    public record PipelineProgressionRecord(string Pipeline, string Stage, int Action, PipelineProgression Progression, int Results = int.MinValue);
}
