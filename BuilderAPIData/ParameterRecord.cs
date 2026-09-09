using System.Collections.Generic;
using System.Text;

namespace BuilderAPIData
{
    public record ParameterRecord(string Name, object? Value, List<string> Tags);
}