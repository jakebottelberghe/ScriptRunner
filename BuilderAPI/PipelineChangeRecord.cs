using System;
using System.Collections.Generic;
using System.Text;

namespace BuilderAPI
{
    public record PipelineChangeRecord(string Pipeline, string Stage, int Action, AssetProgressStatus Status);
}
