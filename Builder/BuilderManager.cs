using BuilderAPI;
using BuilderAPIData;
using Microsoft.Extensions.Logging;

namespace Builder
{
    internal class BuilderManager
    {
        private BuilderAPI.Builder _builder;

        public List<Action<ProgressEvent>> ProgressProcressors { get; init; } = new();

        public BuilderManager()
        {
            _builder = new BuilderAPI.Builder();
        }

        public void RunLoadScript(string root)
        {
            var tags = new List<string>(["Script", "Init", "Load"]);
            var pipeTags = new List<string>(["Init", "Load"]);
            var progresLogTags = new List<string>(["_progress"]);
            var loadPath = Path.Combine(root, "Scripts");
            var file = new FileRecord("Load", loadPath, "csx", DateTime.Now, tags);
            var progressionLogFunc = new LoggerRecord(progresLog, progresLogTags);
            
            _builder.AddLogFunction(progressionLogFunc);
            var alwaysProgFunc = _builder.GetLogFunction(progresLogTags);
            _builder.CreateLogger(progresLogTags, alwaysRecords: alwaysProgFunc);

            _builder.AddScript(file);
            _builder.CreatePipeline("Load", pipeTags);
            _builder.AddStepToPipeline("Load", "Base", (_) => true, [], pipeTags);
            _builder.AddActionToStep("Load", "Base", file, (_) => true);

            _builder.RunPipeline("Load", "Base");
        }

        private void progresLog(LogLevel level, string message, Exception? exception, object?[] param)
        {
            if(param.Length >= 5)
            {
                var pipelineName = param[0] as string ?? "";
                var stepName = param[1] as string ?? "";
                var actionIndex = param[2] != null ? (int)param[2] : -1;
                var wasFiltered = param[3] != null ? !(bool)param[3] : false;
                var actionResult = param[4] != null ? (int)param[4] : -1;
                var progressEvent = new ProgressEvent() {
                    Action = actionIndex,
                    Step = stepName,
                    Pipeline= pipelineName,
                    Result = actionResult,
                    WasFilteredByResult = wasFiltered
                };
                ProgressProcressors.ForEach(p => p(progressEvent));
            }
        }
    }
}
