using System.Collections.Generic;
using System.Text;

namespace BuilderAPIData
{
    public record FileRecord(string Name, string Path, string Extention, List<string> Tags);
}
