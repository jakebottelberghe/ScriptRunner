using BuilderAPI;
using BuilderAPIData;
using Microsoft.Extensions.Logging;
using static System.Net.WebRequestMethods;

namespace Builder
{
    internal class BuilderManager
    {
        private BuilderAPI.Builder _builder;
        private string _baseProjectPath = "";


        public List<Action<ProgressEvent>> ProgressProcressors { get; init; } = new();

        public BuilderManager()
        {
            _builder = new BuilderAPI.Builder();
            ProgressionManager.CreateChannel<BuilderRunRecord>();
            ProgressionManager.CreateChannel<BuilderRunRecords>();
        }

        public void RunLoadScript(string root)
        {
            _baseProjectPath = root;
            var tags = new List<string>(["Script", "Init", "Load"]);
            var pipeTags = new List<string>(["Init", "Load"]);
            var progresLogTags = new List<string>(["_progress"]);
            var loadPath = Path.Combine(root, "Scripts");
            var file = new FileRecord("Load", loadPath, "csx", tags);
            
            var alwaysProgFunc = _builder.GetLogFunction(progresLogTags);
            _builder.CreateLogger(progresLogTags, alwaysRecords: alwaysProgFunc);

            _builder.AddScript(file);
            _builder.CreatePipeline("Load", pipeTags);
            _builder.AddStepToPipeline("Load", "Base", (_) => true, [], pipeTags);
            _builder.AddActionToStep("Load", "Base", file, (_) => true);

            _builder.RunPipeline("Load", "Base");
        }

        public void RunPipeline(string pipeline, string step, int actionIdx = -1, HashSet<string>? fileTags = null, HashSet<string>? paramTags = null)
        {
            ProgressionManager.Write(new BuilderRunRecord(pipeline, step, actionIdx, BuilderRunState.Running));
            var files = fileTags != null ? _builder.GetFiles(fileTags.ToList()) : new ();
            var parameters = paramTags != null ? _builder.GetParameter(paramTags.ToList()) : new ();
            _builder.RunPipeline(pipeline, step, actionIdx, files, parameters);
            ProgressionManager.Write(new BuilderRunRecord(pipeline, step, actionIdx, BuilderRunState.Completed));
        }

        public void RunPipelines(List<string> pipelineTags, List<string> stepTags, int actionIdx = -1, HashSet<string>? fileTags = null, HashSet<string>? paramTags = null
        {
            var files = fileTags != null ? _builder.GetFiles(fileTags.ToList()) : new();
            var parameters = paramTags != null ? _builder.GetParameter(paramTags.ToList()) : new();

            ProgressionManager.Write(new BuilderRunRecords(pipelineTags, stepTags, actionIdx, BuilderRunState.Running));
            _builder.RunPipeline(pipelineTags, stepTags, actionIdx, files, parameters);
            ProgressionManager.Write(new BuilderRunRecords(pipelineTags, stepTags, actionIdx, BuilderRunState.Completed));
        }

        public List<FileRecord> GetFiles(List<string> tags)
        {
            return _builder.GetFiles(tags);
        }
        public List<ParameterRecord> GetParams(List<string> tags)
        {
            return _builder.GetParameter(tags);
        }

        public List<PipelineRecord> GetPipelines(List<string> tags)
        {
            return _builder.GetPipelines(tags);
        }
        public PipelineRecord? GetPipeline(string name)
        {
            return _builder.GetPipeline(name);
        }
        public List<TriggerRecord> GetTriggers(List<string> tags)
        {
            return _builder.GetTriggers(tags);
        }

        public void AddFile(string filePath, List<string>? tags = null)
        {
            var path = Path.GetFullPath(filePath);
            var name = Path.GetFileNameWithoutExtension(filePath);
            var extention = Path.GetExtension(filePath);
            var relitivePath = Path.GetRelativePath(_baseProjectPath, path);
            var generateTags = relitivePath
                                .Split(Path.PathSeparator, StringSplitOptions.RemoveEmptyEntries)
                                .ToList();
            generateTags.Add(extention);
            generateTags.Add(relitivePath);
            var record = new FileRecord(name, path, extention, tags ?? generateTags);
            _builder.AddFile(record);
        }

        public void AddTrigger(string basePath, List<string> pipelineTags, List<string> fileTags, List<string> triggerTags, List<string> tags)
        {
            _builder.AddTrigger(new TriggerRecord(basePath, pipelineTags, fileTags, triggerTags, tags));
        }
    }
}
