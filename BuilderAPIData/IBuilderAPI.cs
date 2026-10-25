using System.Collections.Generic;
using System.Text;

namespace BuilderAPIData
{
    public interface IBuilderAPI
    {
        // File management
        public void AddFile(FileRecord file);
        public void RemoveFile(FileRecord file);
        public List<FileRecord> GetFiles(List<string> selectionTags);

        public void AddScript(FileRecord file);
        public void RemoveScript(FileRecord file);
        public List<FileRecord> GetScripts(List<string> selectionTags);

        // Parameter management
        public void AddParameter(ParameterRecord parameter);
        public void RemoveParameter(ParameterRecord parameter);
        public List<ParameterRecord> GetParameter(List<string> selectionTags);

        // Triggers management
        public void AddTrigger(TriggerRecord trigger);
        public void RemoveTrigger(TriggerRecord trigger);
        public List<TriggerRecord> GetTriggers(List<string> triggerTags);

        // Logging management
        public void AddLogFunction(LoggerRecord log);
        public void RemoveLogFunction(LoggerRecord log);
        public List<LoggerRecord> GetLogFunction(List<string> loggerTags);
        public void CreateLogger( 
            List<string> loggerTags,
            List<LoggerRecord>? traceTagSelect = null, 
            List<LoggerRecord>? debugTagSelect = null, 
            List<LoggerRecord>? infoTagSelect = null, 
            List<LoggerRecord>? warningTagSelect = null,
            List<LoggerRecord>? errorTagSelect = null,
            List<LoggerRecord>? criticalTagSelect = null,
            List<LoggerRecord>? alwaysTagSelect = null
        );
        public void SetGlobalLogger(List<string> loggerTagSelect);

        public List<PipelineRecord> GetPipelines(List<string> tags);
        public PipelineRecord? GetPipeline(string name);

        // Pipeline creation
        public void CreatePipeline(string name, List<string> pipelineTags);
        public void AddStepToPipeline(string pipeline, string step, Func<int, bool> isValidOutput, List<string> parameterSelectionTags, List<string> stepTags);
        public void AddActionToStep(string pipeline, string step, FileRecord script, Func<int, bool> shouldRun);

        // Pipeline actions    
        public int RunPipeline(string pipeline, string stepName = "", int actionIdx = -1, List<FileRecord>? files = null, List<ParameterRecord>? parameters = null);
        public int RunPipeline(string pipelineTags, List<string> stageSelctionTags, int actionIdx = -1, List<FileRecord>? files = null, List<ParameterRecord>? parameters = null);
        public int RunPipeline(List<string> pipelineTags, string stepName = "", int actionIdx = -1, List<FileRecord>? files = null, List<ParameterRecord>? parameters = null);
        public int RunPipeline(List<string> pipelineTags, List<string> stageSelctionTags, int actionIdx = -1, List<FileRecord>? files = null, List<ParameterRecord>? parameters = null);
    }
}
