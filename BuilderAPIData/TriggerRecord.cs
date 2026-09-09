using System.Collections.Generic;
using System.Text;

namespace BuilderAPIData
{
    public record TriggerRecord(string BasePath, List<string> PipelineTags, List<string> FileTags, List<string> TriggerTags, List<string> Tags);
}
