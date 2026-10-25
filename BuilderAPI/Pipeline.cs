using BuilderAPIData;
using System;
using System.Collections.Generic;
using System.Text;

namespace BuilderAPI
{
    internal class PipelineNode
    {
        internal class Node
        {
            public ScriptFile ScriptFile { get; set; }
            public Func<int, bool> ShouldRun { get; set; } = (int _) => { return true; };
        }

        public string Name { get; set; } = "";
        public List<string> ParameterTags { get; set; } = new();
        public List<string> Tags { get; set; } = new();
        public List<Node> Nodes { get; set; } = new();
        public Func<int, bool> ValidateOutput { get; set; } = (int o) => { return o == 0; };
    }

    internal class Pipeline
    {
        public string Name { get; set; } = "";

        public List<PipelineNode> Steps { get; set; } = new();

        public int CurrentStep { get; set; } = 0;
        public int LastResult { get; set; } = 0;

        public List<string> Tags { get; set; } = new();
    }
}
