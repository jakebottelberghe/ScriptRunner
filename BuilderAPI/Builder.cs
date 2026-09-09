using BuilderAPIData;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.IO.Pipelines;
using System.Text;
using static System.Net.WebRequestMethods;

namespace BuilderAPI
{
    public class Builder : IBuilderAPI, IDisposable
    {
        internal Dictionary<FileRecord, File> Files { get; init; } = new ();
        internal Dictionary<FileRecord, ScriptFile> Scripts { get; init; } = new ();
        internal Dictionary<string, Pipeline> Pipelines { get; init; } = new ();
        internal HashSet<LoggerRecord> LogFunctions { get; init; } = new ();
        internal HashSet<ParameterRecord> Parameters { get; init; } = new ();
        internal HashSet<Logger> Loggers { get; init; } = new ();
        internal Logger CurrentLogger { get; set; } = new ();
        internal TriggerPathSearcher TriggersByPath { get; init; } = new ();
        internal HashSet<TriggerRecord> Triggers { get; init; } = new ();
        internal FileMonitor TriggerMonitor { get; init; } = new ();

        public void StartFileMonitor(string root)
        {
            TriggerMonitor.EventHanderCallback.Add(FileTriggerHandler);
            TriggerMonitor.StartMonitor(root);
        }

        // File management
        public void AddFile(FileRecord file)
        {
            Files[file] = new File(file);
            Files[file].Hash = Files[file].GenerateHash();
        }

        public void RemoveFile(FileRecord file)
        {
            Files.Remove(file);
        }

        public List<FileRecord> GetFiles(List<string> selectionTags)
        {
            return Files.Keys.Where(file =>
            {
                return file.Tags.Intersect(selectionTags).Any();
            }).ToList();
        }

        public void AddScript(FileRecord file)
        {
            var script = new ScriptFile(file, Path.Combine("cache", file.Name), CurrentLogger);
            Scripts[script.FileInfo] = script;
        }
        public void RemoveScript(FileRecord file)
        {
            Scripts.Remove(file);
        }
        public List<FileRecord> GetScripts(List<string> selectionTags)
        {
            return Scripts.Keys.Where(file =>
            {
                return file.Tags.Intersect(selectionTags).Any();
            }).ToList();
        }

        // Parameter management
        public void AddParameter(ParameterRecord parameter)
        {
            Parameters.Add(parameter);
        }
        public void RemoveParameter(ParameterRecord parameter)
        {
            Parameters.Remove(parameter);
        }
        public List<ParameterRecord> GetParameter(List<string> selectionTags)
        {
            return FilterParameters(Parameters, selectionTags);
        }

        // Logging management
        public void AddLogFunction(LoggerRecord log)
        {
            LogFunctions.Add(log);
        }
        public void RemoveLogFunction(LoggerRecord log)
        {
            LogFunctions.Remove(log);
        }
        public List<LoggerRecord> GetLogFunction(List<string> loggerTags)
        {
            return LogFunctions.Where(log =>
            {
                return log.Tags.Intersect(loggerTags).Any();
            }).ToList();
        }

        public void CreateLogger(
            List<string> loggerTags,
            List<LoggerRecord>? traceRecords = null,
            List<LoggerRecord>? debugRecords = null,
            List<LoggerRecord>? infoRecords = null,
            List<LoggerRecord>? warningRecords = null,
            List<LoggerRecord>? errorRecords = null,
            List<LoggerRecord>? criticalRecords = null,
            List<LoggerRecord>? alwaysRecords = null
        )
        {
            Loggers.Add(new Logger()
            {
                Tags = loggerTags,
                Always = alwaysRecords ?? new List<LoggerRecord>(),
                Trace = traceRecords ?? new List<LoggerRecord>(),
                Debug = debugRecords ?? new List<LoggerRecord>(),
                Information = infoRecords ?? new List<LoggerRecord>(),
                Warning = warningRecords ?? new List<LoggerRecord>(),
                Error = errorRecords ?? new List<LoggerRecord>(),
                Critical = criticalRecords ?? new List<LoggerRecord>()
            });
        }

        public void SetGlobalLogger(List<string> loggerTagSelect)
        {
            var temp = Loggers.Where( log => {
                return log.Tags.Intersect(loggerTagSelect).Any();
            }).ToList();
            if (temp.Count == 1) CurrentLogger = temp.First();
            else
            {
                CurrentLogger = new Logger()
                {
                    Always = temp.SelectMany(i => i.Always).ToList(),
                    Trace = temp.SelectMany(i => i.Trace).ToList(),
                    Information = temp.SelectMany(i => i.Information).ToList(),
                    Debug = temp.SelectMany(i => i.Debug).ToList(),
                    Warning = temp.SelectMany(i => i.Warning).ToList(),
                    Error = temp.SelectMany(i => i.Error).ToList(),
                    Critical = temp.SelectMany(i => i.Critical).ToList(),
                };
            }

        }

        // Triggers management
        public void AddTrigger(TriggerRecord trigger)
        {
            if (Triggers.Contains(trigger)) return;
            Triggers.Add(trigger);
            TriggersByPath.AddTrigger(trigger);
        }

        public void RemoveTrigger(TriggerRecord trigger)
        {
            Triggers.Remove(trigger);
            TriggersByPath.RemoveTrigger(trigger);
        }
        public List<TriggerRecord> GetTriggers(List<string> triggerTags)
        {
            return Triggers.Where(t =>
            {
                return t.Tags.Intersect(triggerTags).Any();
            }).ToList();
        }

        // Pipeline creation
        public void CreatePipeline(string name, List<string> pipelineTags)
        {
            Pipelines[name] = new Pipeline() { Tags = pipelineTags, Name = name};
        }

        public void AddStepToPipeline(string pipelineName, string step, Func<int, bool> isValidOutput, List<string> parameterSelectionTags, List<string> stepTags)
        {
            var pipeline = Pipelines[pipelineName];
            if (pipeline == null) return;
            pipeline.Steps.Add(new PipelineNode() { Name = step, ValidateOutput = isValidOutput, ParameterTags = parameterSelectionTags, Tags = stepTags });
        }

        public void AddActionToStep(string pipelineName, string stepName, FileRecord script, Func<int, bool> shouldRun)
        {
            if (Scripts[script] == null) return;

            var pipeline = Pipelines[pipelineName];
            if (pipeline == null) return;

            var step = pipeline.Steps.FirstOrDefault(s => s.Name.Equals(stepName));
            if (step == null) return;

            step.Nodes.Add(new PipelineNode.Node() { ScriptFile = Scripts[script], ShouldRun = shouldRun });
        }

        // Pipeline actions   
        public int RunPipeline(string pipeline, string step, int actionIdx = -1, List<FileRecord>? files = null, List<ParameterRecord>? parameters = null)
        {
            var pipe = Pipelines[pipeline];
            if(pipe == null) return 0;

            return RunPipelines([pipe], step, actionIdx, files ?? new List<FileRecord>(), parameters ?? new List<ParameterRecord>());
        }

        public int RunPipeline(string pipeline, List<string> stepSelctionTags, int actionIdx = -1, List<FileRecord>? files = null, List<ParameterRecord>? parameters = null)
        { 
            var pipe = Pipelines[pipeline];
            if(pipe == null) return 0;

            return RunPipelines([pipe], stepSelctionTags, actionIdx, files ?? new List<FileRecord>(), parameters ?? new List<ParameterRecord>());

        }

        public int RunPipeline(List<string> pipelineTags, string step, int actionIdx = -1, List<FileRecord>? files = null, List<ParameterRecord>? parameters = null)
        {
            var pipelines = Pipelines.Values.Where(p =>
            {
                return p.Tags.Intersect(pipelineTags).Any();
            }).ToList();

            if (!pipelines.Any()) return 0;
            return RunPipelines(pipelines, step, actionIdx, files ?? new List<FileRecord>(), parameters ?? new List<ParameterRecord>());

        }
        public int RunPipeline(List<string> pipelineTags, List<string> stepSelctionTags, int actionIdx = -1, List<FileRecord>? files = null, List<ParameterRecord>? parameters = null)
        {
            var pipelines = Pipelines.Values.Where(p =>
            {
                return p.Tags.Intersect(pipelineTags).Any();
            }).ToList();

            if (!pipelines.Any()) return 0;
            return RunPipelines(pipelines, stepSelctionTags, actionIdx, files ?? new List<FileRecord>(), parameters ?? new List<ParameterRecord>());
        }

        private int RunPipelines(IEnumerable<Pipeline> pipelines, List<string> stepSelctionTags, int action, List<FileRecord> files, List<ParameterRecord> parameters)
        {
            if (stepSelctionTags.Any())
            {
                var progressionLogger = Loggers.FirstOrDefault(l => l.Tags.Contains("_progression"));
                ConcurrentDictionary<string, List<int>> parallelData = new ConcurrentDictionary<string, List<int>>();
                Parallel.ForEachAsync(pipelines, async (pl, ct) =>
                {
                    var steps = pl.Steps.Where(s =>
                    {
                        return s.Tags.Intersect(stepSelctionTags).Any();
                    }).ToList();

                    parallelData.TryAdd(pl.Name, await RunSteps(steps, action, files, parameters, progressionLogger, pl.Name));
                }).GetAwaiter().GetResult();

            };
            return -1;
        }

        private int RunPipelines(IEnumerable<Pipeline> pipelines, string stepName, int action, List<FileRecord> files, List<ParameterRecord> parameters)
        {
            ConcurrentDictionary<string, List<int>> parallelData = new ConcurrentDictionary<string, List<int>>();
            var progressionLogger = Loggers.FirstOrDefault(l => l.Tags.Contains("_progression"));
            if (string.IsNullOrEmpty(stepName))
            {
                Parallel.ForEachAsync(pipelines, async (pl, ct) =>
                {
                    parallelData.TryAdd(pl.Name, await RunSteps(pl.Steps, action, files, parameters, progressionLogger, pl.Name));
                }).GetAwaiter().GetResult();
            }
            else
            {
                Parallel.ForEachAsync(pipelines, async (pl, ct) =>
                {
                    var steps = pl.Steps.Where(step => { return !string.IsNullOrEmpty(step.Name) && string.Compare(step.Name, stepName) == 0; });
                    parallelData.TryAdd(pl.Name, await RunSteps(steps, action, files, parameters, progressionLogger, pl.Name));
                }).GetAwaiter().GetResult();
            }
            return -1;
        }

        private async Task<List<int>> RunSteps(IEnumerable<PipelineNode> nodes, int actionToRun, List<FileRecord> files, List<ParameterRecord> parameters, Logger? progLogger, string plName )
        {
            List<int> stepOutput = new ();
            foreach (PipelineNode node in nodes)
            {
                var filterParams = FilterParameters(parameters, node.ParameterTags);
                if(actionToRun >= 0)
                {
                    var retValue = await node.Nodes[actionToRun].ScriptFile.Run(files, filterParams, this, CurrentLogger);
                    stepOutput.Add(retValue);
                    progLogger?.LogFunc(LogLevel.Information, "{Pipeline}:{Step}:{Action}-Ran with return value {Value}", null, [plName, node.Name, actionToRun, retValue]);
                }
                else
                {
                    stepOutput.Add( await RunActions(node.Nodes, files, filterParams, progLogger, plName, node.Name));
                }
            }
            return stepOutput;
        }

        private async Task<int> RunActions(List<PipelineNode.Node> nodes, List<FileRecord> files, List<ParameterRecord> parameters, Logger? progLogger, string plName, string stepName)
        {
            int retValue = int.MinValue;
            for(var i = 0; i < nodes.Count; ++i)
            {
                var node = nodes[i];
                if (retValue == int.MinValue || node.ShouldRun(retValue))
                {
                    retValue = await node.ScriptFile.Run(files, parameters, this, CurrentLogger);
                    progLogger?.LogFunc(LogLevel.Information, "{Pipeline}:{Step}:{Action}-Did step Run:{Ran}-Step returned value:{Value}", null, [plName, stepName, i, true, retValue]);
                }
                else
                {
                    progLogger?.LogFunc(LogLevel.Information, "{Pipeline}:{Step}:{Action}-Did step run:{Ran}-Step filtered because of return value:{Value}", null, [plName, stepName, i, false, retValue]);
                    break;
                }
            }
            return retValue;
        }

        
        private async Task<int> RunPipelineAtStep(Pipeline pipeline, int startStep, List<FileRecord> files, List<ParameterRecord> parameters)
        {
            var totalSteps = pipeline.Steps.Count;
            if (totalSteps <= startStep) return -1;
            for(var i = startStep; i < pipeline.Steps.Count; ++i)
            {
                var step = pipeline.Steps[i];
                pipeline.LastResult = await RunStep(pipeline.LastResult, step, files, parameters);

                if (!step.ValidateOutput(pipeline.LastResult)) break;
            }
            return pipeline.LastResult;
        }

        private async Task<int> RunStep(int lastResult, PipelineNode step, List<FileRecord> files, List<ParameterRecord> parameters)
        {
            var paramsForStep = FilterParameters(parameters, step.ParameterTags);
            foreach(var stepNode in step.Nodes)
            {
                if(stepNode.ShouldRun(lastResult))
                {
                    lastResult = await RunStepAction(stepNode, files, paramsForStep);
                }
            }

            return lastResult;
        }

        private async Task<int> RunStepAction(PipelineNode.Node node, List<FileRecord> files, List<ParameterRecord> paramsForStep)
        {
            return await node.ScriptFile.Run(files, paramsForStep, this, CurrentLogger);
        }

        private List<ParameterRecord> FilterParameters( IEnumerable<ParameterRecord> parameters, List<string> tags)
        {
            return Parameters.Where(par =>
            {
                return par.Tags.Intersect(tags).Any();
            }).ToList();
        }

        private void FileTriggerHandler(MonitorRecord fileTrigger)
        {
            var triggers = TriggersByPath.GetTriggers(fileTrigger.Path, fileTrigger.Tags);
            foreach(var t in triggers)
            {
                ApplyTrigger(fileTrigger, t);
            }

        }

        private void ApplyTrigger(MonitorRecord fileTrigger, TriggerRecord trigger)
        {
            if (!trigger.TriggerTags.Intersect(fileTrigger.Tags).Any()) return;
            var now = DateTime.Now;
            var ftRec = new FileRecord(Path.GetFileNameWithoutExtension(fileTrigger.Name), fileTrigger.Path, Path.GetExtension(fileTrigger.Name), now, []);
            var files = Files.Keys
                .Where(f => f with { LastUpdate = now, Tags = [] } == ftRec)
                .Where(f => f.Tags.Intersect(trigger.FileTags).Any())
                .ToList();
            
            
        }

        public void Dispose()
        {
            TriggerMonitor.Dispose();
        }
    }
}
