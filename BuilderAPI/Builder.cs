using BuilderAPIData;
using System.Collections.Concurrent;

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

        public Builder()
        {
            ProgressionManager.CreateChannel<PipelineProgressionRecord>();
            ProgressionManager.CreateChannel<AssetChangeRecord<FileRecord>>();
            ProgressionManager.CreateChannel<AssetChangeRecord<LoggerRecord>>();
            ProgressionManager.CreateChannel<AssetChangeRecord<TriggerRecord>>();
            ProgressionManager.CreateChannel<AssetChangeRecord<ParameterRecord>>();
            ProgressionManager.CreateChannel<PipelineChangeRecord>();

        }

        public void StartFileMonitor(string root)
        {
            TriggerMonitor.EventHanderCallback.Add(FileTriggerHandler);
            TriggerMonitor.StartMonitor(root);
        }

        // File management
        public void AddFile(FileRecord file)
        {
            Files[file] = new File(file);
            ProgressionManager.Write(new AssetChangeRecord<FileRecord>(file, AssetProgressStatus.Created));
            Files[file].Hash = Files[file].GenerateHash();
        }

        public void RemoveFile(FileRecord file)
        {
            ProgressionManager.Write(new AssetChangeRecord<FileRecord>(file, AssetProgressStatus.Deleted));
            Files.Remove(file);
        }

        public List<FileRecord> GetFiles(List<string> selectionTags)
        {
            return Files.Keys.Where(file =>
            {
                return DoTagMatch(file.Tags, selectionTags);
            }).ToList();
        }

        public void AddScript(FileRecord file)
        {
            var script = new ScriptFile(file, Path.Combine("cache", file.Name), CurrentLogger);
            ProgressionManager.Write(new AssetChangeRecord<FileRecord>(file, AssetProgressStatus.Created));
            Scripts[script.FileInfo] = script;
        }
        public void RemoveScript(FileRecord file)
        {
            ProgressionManager.Write(new AssetChangeRecord<FileRecord>(file, AssetProgressStatus.Created));
            Scripts.Remove(file);
        }
        public List<FileRecord> GetScripts(List<string> selectionTags)
        {
            return Scripts.Keys.Where(file =>
            {
                return DoTagMatch(file.Tags, selectionTags);
            }).ToList();
        }

        // Parameter management
        public void AddParameter(ParameterRecord parameter)
        {
            ProgressionManager.Write(new AssetChangeRecord<ParameterRecord>(parameter, AssetProgressStatus.Created));
            Parameters.Add(parameter);
        }
        public void RemoveParameter(ParameterRecord parameter)
        {
            ProgressionManager.Write(new AssetChangeRecord<ParameterRecord>(parameter, AssetProgressStatus.Deleted));
            Parameters.Remove(parameter);
        }
        public List<ParameterRecord> GetParameter(List<string> selectionTags)
        {
            return FilterParameters(Parameters, selectionTags);
        }

        // Logging management
        public void AddLogFunction(LoggerRecord log)
        {
            ProgressionManager.Write(new AssetChangeRecord<LoggerRecord>(log, AssetProgressStatus.Created));
            LogFunctions.Add(log);
        }
        public void RemoveLogFunction(LoggerRecord log)
        {
            ProgressionManager.Write(new AssetChangeRecord<LoggerRecord>(log, AssetProgressStatus.Deleted));
            LogFunctions.Remove(log);
        }
        public List<LoggerRecord> GetLogFunction(List<string> loggerTags)
        {
            return LogFunctions.Where(log =>
            {
                return DoTagMatch(log.Tags, loggerTags);
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
                return DoTagMatch(log.Tags, loggerTagSelect);
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
            ProgressionManager.Write(new AssetChangeRecord<TriggerRecord>(trigger, AssetProgressStatus.Created));
            Triggers.Add(trigger);
            TriggersByPath.AddTrigger(trigger);
        }

        public void RemoveTrigger(TriggerRecord trigger)
        {
            ProgressionManager.Write(new AssetChangeRecord<TriggerRecord>(trigger, AssetProgressStatus.Deleted));
            Triggers.Remove(trigger);
            TriggersByPath.RemoveTrigger(trigger);
        }
        public List<TriggerRecord> GetTriggers(List<string> triggerTags)
        {
            return Triggers.Where(t =>
            {
                return DoTagMatch(t.Tags, triggerTags);
            }).ToList();
        }


        public PipelineRecord? GetPipeline(string name)
        {
            if (!Pipelines.ContainsKey(name)) return null;
            var p = Pipelines[name];
            List<PipelineStageRecord> stags = p.Steps.Select(s =>
            {
                var pars = GetParameter(s.ParameterTags);
                List<FileRecord> files = s.Nodes.Select(n => n.ScriptFile.FileInfo).ToList();
                return new PipelineStageRecord(s.Name, s.Tags, pars, files);
            }).ToList();
            return new PipelineRecord(p.Name, p.Tags, stags);
        }

        public List<PipelineRecord> GetPipelines(List<string> tags)
        {
            return Pipelines.Values
                .Where( p => { return DoTagMatch(p.Tags, tags); } )
                .Select( p =>
                {
                    List<PipelineStageRecord> stags = p.Steps.Select(s =>
                    {
                        var pars = GetParameter(s.ParameterTags);
                        List<FileRecord> files = s.Nodes.Select(n => n.ScriptFile.FileInfo).ToList();
                        return new PipelineStageRecord(s.Name, s.Tags, pars, files);
                    }).ToList();
                    return new PipelineRecord(p.Name, p.Tags, stags);
                }).ToList();
        }

        // Pipeline creation
        public void CreatePipeline(string name, List<string> pipelineTags)
        {
            ProgressionManager.Write(new PipelineChangeRecord(name, string.Empty, int.MinValue, AssetProgressStatus.Created));
            Pipelines[name] = new Pipeline() { Tags = pipelineTags, Name = name};
        }

        public void AddStepToPipeline(string pipelineName, string step, Func<int, bool> isValidOutput, List<string> parameterSelectionTags, List<string> stepTags)
        {
            var pipeline = Pipelines[pipelineName];
            if (pipeline == null) return;
            ProgressionManager.Write(new PipelineChangeRecord(pipelineName, step, int.MinValue, AssetProgressStatus.Modified));
            pipeline.Steps.Add(new PipelineNode() { Name = step, ValidateOutput = isValidOutput, ParameterTags = parameterSelectionTags, Tags = stepTags });
        }

        public void AddActionToStep(string pipelineName, string stepName, FileRecord script, Func<int, bool> shouldRun)
        {
            if (Scripts[script] == null) return;

            var pipeline = Pipelines[pipelineName];
            if (pipeline == null) return;

            var step = pipeline.Steps.FirstOrDefault(s => s.Name.Equals(stepName));
            if (step == null) return;

            ProgressionManager.Write(new PipelineChangeRecord(pipelineName, stepName, step.Nodes.Count + 1, AssetProgressStatus.Modified));
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
                return DoTagMatch(p.Tags, pipelineTags);
            }).ToList();

            if (!pipelines.Any()) return 0;
            return RunPipelines(pipelines, step, actionIdx, files ?? new List<FileRecord>(), parameters ?? new List<ParameterRecord>());

        }
        public int RunPipeline(List<string> pipelineTags, List<string> stepSelctionTags, int actionIdx = -1, List<FileRecord>? files = null, List<ParameterRecord>? parameters = null)
        {
            var pipelines = Pipelines.Values.Where(p =>
            {
                return DoTagMatch(p.Tags, pipelineTags);
            }).ToList();

            if (!pipelines.Any()) return 0;
            return RunPipelines(pipelines, stepSelctionTags, actionIdx, files ?? new List<FileRecord>(), parameters ?? new List<ParameterRecord>());
        }

        private int RunPipelines(IEnumerable<Pipeline> pipelines, List<string> stepSelctionTags, int action, List<FileRecord> files, List<ParameterRecord> parameters)
        {
            if (stepSelctionTags.Any())
            {
                ConcurrentDictionary<string, List<int>> parallelData = new ConcurrentDictionary<string, List<int>>();
                Parallel.ForEachAsync(pipelines, async (pl, ct) =>
                {
                    var steps = pl.Steps.Where(s =>
                    {
                        return DoTagMatch(s.Tags, stepSelctionTags);
                    }).ToList();
                    var progression = new PipelineProgressionRecord(pl.Name, string.Empty, int.MinValue, PipelineProgression.Running);
                    parallelData.TryAdd(pl.Name, await RunSteps(steps, action, files, parameters, progression));
                }).GetAwaiter().GetResult();

            };
            return -1;
        }

        private int RunPipelines(IEnumerable<Pipeline> pipelines, string stepName, int action, List<FileRecord> files, List<ParameterRecord> parameters)
        {
            ConcurrentDictionary<string, List<int>> parallelData = new ConcurrentDictionary<string, List<int>>();
            if (string.IsNullOrEmpty(stepName))
            {
                Parallel.ForEachAsync(pipelines, async (pl, ct) =>
                {
                    var progression = new PipelineProgressionRecord(pl.Name, string.Empty, int.MinValue, PipelineProgression.Running);
                    parallelData.TryAdd(pl.Name, await RunSteps(pl.Steps, action, files, parameters, progression));
                }).GetAwaiter().GetResult();
            }
            else
            {
                Parallel.ForEachAsync(pipelines, async (pl, ct) =>
                {
                    var steps = pl.Steps.Where(step => { return !string.IsNullOrEmpty(step.Name) && string.Compare(step.Name, stepName) == 0; });
                    var progression = new PipelineProgressionRecord(pl.Name, string.Empty, int.MinValue, PipelineProgression.Running);
                    parallelData.TryAdd(pl.Name, await RunSteps(steps, action, files, parameters, progression));
                }).GetAwaiter().GetResult();
            }
            return -1;
        }

        private async Task<List<int>> RunSteps(IEnumerable<PipelineNode> nodes, int actionToRun, List<FileRecord> files, List<ParameterRecord> parameters, PipelineProgressionRecord progression )
        {
            List<int> stepOutput = new ();
            foreach (PipelineNode node in nodes)
            {
                var filterParams = FilterParameters(parameters, node.ParameterTags);
                if(actionToRun >= 0)
                {
                    var running = progression with { Stage = node.Name, Action = actionToRun };
                    ProgressionManager.Write(running);
                    var retValue = await node.Nodes[actionToRun].ScriptFile.Run(files, filterParams, this, CurrentLogger);

                    var result = running with { Progression = PipelineProgression.Completed, Results = retValue };
                    ProgressionManager.Write(result);

                    stepOutput.Add(retValue);
                }
                else
                {
                    var tempAction = progression with { Stage = node.Name };
                    stepOutput.Add( await RunActions(node.Nodes, files, filterParams, tempAction));
                }
            }
            return stepOutput;
        }

        private async Task<int> RunActions(List<PipelineNode.Node> nodes, List<FileRecord> files, List<ParameterRecord> parameters, PipelineProgressionRecord progres )
        {
            int retValue = int.MinValue;
            for(var i = 0; i < nodes.Count; ++i)
            {
                var node = nodes[i];
                if (retValue == int.MinValue || node.ShouldRun(retValue))
                {
                    var running = progres with { Action = i };
                    ProgressionManager.Write(running);
                    retValue = await node.ScriptFile.Run(files, parameters, this, CurrentLogger);
                    var complete = running with { Progression = PipelineProgression.Completed, Results = retValue };
                    ProgressionManager.Write(complete);
                }
                else
                {
                    var skipped = progres with { Action = i, Progression = PipelineProgression.Skipped };
                    ProgressionManager.Write(skipped);
                }
            }
            return retValue;
        }

        private async Task<int> RunStepAction(PipelineNode.Node node, List<FileRecord> files, List<ParameterRecord> paramsForStep)
        {
            return await node.ScriptFile.Run(files, paramsForStep, this, CurrentLogger);
        }

        private List<ParameterRecord> FilterParameters( IEnumerable<ParameterRecord> parameters, List<string> tags)
        {
            return Parameters.Where(par =>
            {
                return DoTagMatch(par.Tags, tags);
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
            if (DoTagMatch(fileTrigger.Tags, trigger.TriggerTags)) return;
            var now = DateTime.Now;
            var ftRec = new FileRecord(Path.GetFileNameWithoutExtension(fileTrigger.Name), fileTrigger.Path, Path.GetExtension(fileTrigger.Name), []);
            var files = Files.Keys
                .Where(f => f with { Tags = [] } == ftRec)
                .Where(f => DoTagMatch(f.Tags, trigger.FileTags))
                .ToList();
        }

        private bool DoTagMatch(List<string> assetTags, List<string> filterTags)
        {
            return assetTags.Intersect(filterTags).Count() == Math.Min(assetTags.Count, filterTags.Count);
        }
        public void Dispose()
        {
            TriggerMonitor.Dispose();
        }
    }
}
