using BuilderAPIData;
using System.CommandLine;
using System.CommandLine.Parsing;
using System.Text;
using static Microsoft.CodeAnalysis.CSharp.SyntaxTokenParser;

namespace Builder
{
    internal class CommandLineController
    {
        private RootCommand _cliCommands;
        private BuilderManager _builder;

        public CommandLineController(BuilderManager builder)
        {
            _builder = builder;
           
            // Setting up command commands
            _cliCommands = new RootCommand("CLI for build applicatoin.");
            _cliCommands.Add(CreateRunCommand());
            _cliCommands.Add(CreateCreateCommand());
            _cliCommands.Add(CreateViewCommand());
        }

        public Task Run(CancellationToken canCancel)
        {
            return Task.Run(() =>
            {
                Console.BackgroundColor = CLIConstance.CMDBGColor;
                while (!canCancel.IsCancellationRequested)
                {
                    Console.ForegroundColor = CLIConstance.CMDStartColor;
                    Console.Write(CLIConstance.LineStart);
                    Console.ForegroundColor = CLIConstance.CMDInputColor;
                    var consoleLine = Console.ReadLine();
                    if (consoleLine == null) continue;
                    // var splitConsoleLine = consoleLine.Split(" ", StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
                    var parseResults = _cliCommands.Parse(consoleLine);
                    parseResults.Invoke();
                }
            });
        }

        private Command CreateRunCommand()
        {
            var commadParams = CLIConstance.Commands["run"];
            var runCommand = new Command("run", "Will run a given pipeline/s.");
            var pipelineName = new Option<string>(
                commadParams["pipeline"].Name,
                commadParams["pipeline"].Aliases
            )
            {
                Hidden = true,
                Description = "Name of the pipeline to run."
            };

            var pipelineTags = new Option<HashSet<string>>(
                commadParams["pipelineTags"].Name,
                commadParams["pipelineTags"].Aliases
            )
            {
                AllowMultipleArgumentsPerToken = true,
                Hidden = true,
                Description = "Select pipeline/s based on the tags that have been set on pipeline/s.",
                CustomParser = CreateHashSetFromInput
            };

            var stageName = new Option<string>(
                commadParams["stage"].Name,
                commadParams["stage"].Aliases
            )
            {
                Hidden = true,
                Description = "Name of the only stage to run on the selected pipeline/s."
            };

            var stageTags = new Option<HashSet<string>>(
                commadParams["stageTags"].Name,
                commadParams["stageTags"].Aliases
            )
            {
                AllowMultipleArgumentsPerToken = true,
                Hidden = true,
                Description = "Select stage/s based on the tags that have been set on stage/s of the selected pipeline/s.",
                CustomParser = CreateHashSetFromInput
            };

            var actionIdx = new Option<int>(
                commadParams["action"].Name,
                commadParams["action"].Aliases

            )
            {
                Description= "The action index of the selected stage/s to run on the selected pipeline/s."
            };

            var fileSelectionTags = new Option<HashSet<string>>(
                commadParams["fileTags"].Name,
                commadParams["fileTags"].Aliases
            )
            {
                AllowMultipleArgumentsPerToken = true,
                Required = true,
                Description = "Selection tag filters for what files should be passed to the pipeline.",
                CustomParser = CreateHashSetFromInput
            };

            var paramSelectionTags = new Option<HashSet<string>>(
                commadParams["paramTags"].Name,
                commadParams["paramTags"].Aliases
            )
            {
                AllowMultipleArgumentsPerToken = true,
                Description = "Selection tags to filters what params should be passed to the pipeline.",
                CustomParser = CreateHashSetFromInput
            };

            runCommand.Add(pipelineName);
            runCommand.Add(pipelineTags);
            runCommand.Add(stageName);
            runCommand.Add(stageTags);
            runCommand.Add(actionIdx);

            runCommand.Validators.Add((CommandResult result) => {

                // checking all the optional name of tag parameters
                if (CountOptionsInResults([pipelineName, pipelineTags], result) != 1)
                {
                    result.AddError(CreateErrorMessage([pipelineName, pipelineTags]));
                }
                if (CountOptionsInResults([stageName, stageTags], result) > 1)
                {
                    result.AddError(CreateErrorMessage([stageName, stageTags]));
                }

            });
            runCommand.SetAction(EventForRunCommand);
            return runCommand;
        }

        private Command CreateCreateCommand()
        {
            var command = new Command("create", "That creates new assets for the build system.");

            var cmdParm = CLIConstance.Commands["file"];
            var createFile = new Command("file", "Creates new file asset.");
            var fileName = new Argument<string>(cmdParm["file"].Name) { Description = "The file name with either a relitive path or fully qualified path" };
            var fileTags = new Option<HashSet<string>>(
                cmdParm["tags"].Name,
                cmdParm["tags"].Aliases
            ) { Required=true, Description = "Tags for file." };
            
            createFile.Add(fileName);
            createFile.Add(fileTags);
            createFile.SetAction(EventForCreateFile);
            
            command.Add(createFile);

            cmdParm = CLIConstance.Commands["param"];
            var createParam = new Command("param", "Creates new input paramater.");
            var parmName = new Argument<string>(cmdParm["name"].Name) { Description = "Name of the paramaeter." };
            var parmValue = new Argument<string>(cmdParm["value"].Name) { Description = "Value of the paramaeter." };
            var parmTags = new Option<HashSet<string>>(
                cmdParm["tags"].Name,
                cmdParm["tags"].Aliases
            )
            { Required = true, Description = "Tags for this parameter." };

            createParam.Add(parmName);
            createParam.Add(parmValue);
            createParam.Add(parmTags);
            createParam.SetAction(EventForCreateParam);
            command.Add(createParam);


            cmdParm = CLIConstance.Commands["trigger"];
            var createTrigger = new Command("trigger", "Sets a trigger when files change.");
            var triggerPath = new Argument<string>(cmdParm["path"].Name) { Description = "Path that the trigger will match on." };
            
            var triggerPipelineNames = new Option<HashSet<string>>(
                cmdParm["pipeline"].Name,
                cmdParm["pipeline"].Aliases
            )
            { Required = true, Description = "Name of the pipelines that should fire when trigger activates." };

            var triggerChangeTags = new Option<HashSet<string>>(
                cmdParm["changeTags"].Name,
                cmdParm["changeTags"].Aliases
            )
            { Description = "Tags to filter action based on change type." };
            
            var triggerFileTags = new Option<HashSet<string>>(
                cmdParm["fileTags"].Name,
                cmdParm["fileTags"].Aliases
            )
            { Description = "Tags to filter action based on files tags." };

            var triggerTags = new Option<HashSet<string>>(
                cmdParm["tags"].Name,
                cmdParm["tags"].Aliases
            )
            { Required = true, Description = "Tags for this parameter." };

            createTrigger.Add(triggerPath);
            createTrigger.Add(triggerPipelineNames);
            createTrigger.Add(triggerChangeTags);
            createTrigger.Add(triggerFileTags);
            createTrigger.Add(triggerTags);
            createTrigger.SetAction(EventForCreateTrigger);
            command.Add(createTrigger);

            var createPipeline = new Command("pipeline", "Create or modify a pipeline.");
            var pipelineName = new Argument<string>("name") { Description = "Name of the pipeline" };
            var pipelineTags = new Option<HashSet<string>>(
                cmdParm["tags"].Name,
                cmdParm["tags"].Aliases
            )
            { Required = true, Description = "Tags for this pipeline." };

            createPipeline.Add(pipelineName);
            createPipeline.Add(pipelineTags);
            createPipeline.SetAction(EventForCreatePipeline);
            command.Add(createPipeline);


            var createStage = new Command("stage", "Create a stage on a pipeline.");
            var stageName = new Argument<string>("name") { Description = "Name of the new stage." };
            var stagePipeline = new Argument<string>("pipeline") { Description = "Name of the pipeline." };
            var stageValidOutput = new Option<int>("validReturns") { Description = "Valid values of the return of a stage." };
            var stageTags = new Option<HashSet<string>>(
                cmdParm["tags"].Name,
                cmdParm["tags"].Aliases
            )
            { Required = true, Description = "Tags for this pipeline stage." };

            createStage.Add(stageName);
            createStage.Add(stagePipeline);
            createStage.Add(stageValidOutput);
            createStage.Add(stageTags);
            createStage.SetAction(EventForCreateStage);
            command.Add(createStage);

            var createAction = new Command("action", "create an action on a stage.");
            var actionName = new Argument<string>("pipeline") { Description = "Pipeline name to add the give script on." };
            var actionStage = new Argument<string>("stage") { Description = "Stage in the pipeline to add the give script on." };
            var actionValidOutput = new Option<int>("validReturns") { Description = "Valid values of the return of a stage." };
            var actionTags = new Option<HashSet<string>>(
                cmdParm["tags"].Name,
                cmdParm["tags"].Aliases
            );

            createAction.Add(actionName);
            createAction.Add(actionValidOutput);
            createAction.Add(actionTags);
            createAction.SetAction(EventForCreateAction);
            command.Add(createAction);

            return command;
        }
        
        private Command CreateViewCommand()
        {
            var command = new Command("view", "Will manage the displaying of content.");

            var viewType = new Argument<string>("type")
                { Description = "The type to display." }
                .AcceptOnlyFromAmong(
                    "file", 
                    "files",
                    "param", 
                    "params", 
                    "script", 
                    "scripts", 
                    "triggers",
                    "trigger"
                );
            var viewName = new Option<string>("-name", "-n") { Description = "The name of the resource to be viewed" };
            var viewTags = new Option<List<string>>("-tags", "-t") { Description = "The tags to seledt a set of resources to be viewed" };
            var viewExpand = new Option<bool>("-expand", "-e") { Description = "Should display in an expanded mode." };
            command.Add(viewType);
            command.Add(viewName);
            command.Add(viewTags);
            command.Validators.Add( (result) =>
            {
                if (CountOptionsInResults([viewName, viewTags], result) > 1)
                {
                    result.AddError(CreateErrorMessage([viewName, viewTags]));
                }
            });
            command.SetAction(EventForView);
            return command;
        }

        private int CountOptionsInResults(Option[] options, CommandResult from)
        {
            int ret = 0;
            foreach (Option option in options)
            {
                ret += from.GetResult(option) != null ? 1 : 0;
            }
            return ret;
        }

        private string CreateErrorMessage(Option[] options)
        {
            var errorBuilder = new StringBuilder("You must supply one and only one of ");
            foreach(var option in options)
            {
                errorBuilder.Append($"{option.Name} ({string.Join(", ", option.Aliases)}) ");
            }

            return errorBuilder.ToString();
        }

        private HashSet<string> CreateHashSetFromInput(ArgumentResult argResult)
        {
            // Fetch all tokens passed to this argument/option
            var values = argResult.Tokens.Select(t => t.Value);
            return new HashSet<string>(values);
        }

        private void EventForCreateFile(ParseResult args)
        {
        }

        private void EventForCreateParam(ParseResult args)
        {

        }
        private void EventForCreateTrigger(ParseResult args)
        {

        }

        private void EventForCreatePipeline(ParseResult args)
        {

        }

        private void EventForCreateAction(ParseResult args)
        {

        }

        private void EventForCreateStage(ParseResult args)
        {

        }

        private void EventForRunCommand(ParseResult args)
        {
            var commandPar = CLIConstance.Commands["run"];
            var pipeline = args.GetValue<string>(commandPar["pipeline"].Name);

            var stage = args.GetValue<string>(commandPar["stage"].Name) ?? "";

            var action = args.GetValue<int>(commandPar["action"].Name);

            var fileTags = args.GetValue<HashSet<string>>(commandPar["fileTags"].Name);
            var paramTags = args.GetValue<HashSet<string>>(commandPar["-paramTags"].Name);

            if (!string.IsNullOrWhiteSpace(pipeline) && !string.IsNullOrWhiteSpace(stage))
            {
                _builder.RunPipeline(pipeline, stage, action, fileTags, paramTags);
            }
            else
            {
                var pipelineTags = args.GetValue<HashSet<string>>(commandPar["pipelieTags"].Name);
                var stageTags = args.GetValue<HashSet<string>>(commandPar["stageTags"].Name);
                if (pipelineTags != null && stageTags != null)
                {
                    _builder.RunPipelines(tags.ToList(), stageTags.ToList(), action, fileTags, paramTags);
                }
            }
            
        }

        private void EventForView(ParseResult args)
        {

        }
    }
}
