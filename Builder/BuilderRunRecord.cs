using System;
using System.Collections.Generic;
using System.Text;

namespace Builder
{
    internal enum BuilderRunState
    {
        Running,
        Completed
    }

    internal record BuilderRunRecord(string Pipeline, string Stage, int Action, BuilderRunState State);
    internal record BuilderRunRecords(List<string> PipelineTags, List<string> StageTags, int Action, BuilderRunState State);
}
