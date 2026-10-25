using System;
using System.Collections.Generic;
using System.Text;

namespace BuilderAPIData
{
    public record PipelineStageRecord(string Name, List<string> tags, List<ParameterRecord> ParameterTags, List<FileRecord> Actions);
    public record PipelineRecord(string Name, List<string> Tags, List<PipelineStageRecord> Stages);
}
