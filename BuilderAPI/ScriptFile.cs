using BuilderAPIData;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Scripting;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Scripting;
using Microsoft.Extensions.Logging;
using NuGet.Packaging.Licenses;
using System.Reflection;
using System.Runtime.CompilerServices;

namespace BuilderAPI
{
    public class ScriptData
    {
        public IBuilderAPI? BuilderSystem { get; set; }
        public List<FileRecord>? Files { get; set; }
        public List<ParameterRecord>? Parameters { get; set; }
        public Action<LogLevel, string, Exception, object?[]>? Logger { get; set; }
    };

    internal class ScriptFile : File
    {
        private string? _scriptData;
        private Script<int>? _script;
        private readonly ScriptOptions _options;
        private readonly string _osLibaryExtention = System.OperatingSystem.IsLinux() ? "so" : "dll";

        public string CacheDirectory { get; set; }

        public ScriptFile(FileRecord file, string cacheLocation, Logger logger)
            : base(file) 
        {
            CacheDirectory = cacheLocation;


            var systemRef = AppDomain.CurrentDomain.GetAssemblies()
                               .Where(a => !a.IsDynamic && !string.IsNullOrEmpty(a.Location))
                               .Select(a => MetadataReference.CreateFromFile(a.Location))
                               .ToList();

            _options = ScriptOptions.Default
                        .WithMetadataResolver(new ScriptNugetLoader(CacheDirectory, logger))
                        .WithReferences(systemRef)
                        .WithWarningLevel(4)
                        .WithOptimizationLevel(OptimizationLevel.Release);
            
            LoadScript();
        }

        public async Task<int> Run(List<FileRecord> files, List<ParameterRecord> parameters, IBuilderAPI builder, Logger logger)
        {
            var currentHash = GenerateHash();
            if(currentHash != Hash)
            {
                Hash = currentHash;
                LoadScript();
            }
            if (_script == null) throw new NullReferenceException("Script file did not load correctly.");

            var global = new ScriptData { Files = files, Parameters = parameters, BuilderSystem = builder, Logger = logger.LogFunc };
            var ret = await _script.RunAsync(global);

            if (ret.Exception != null)
            {
                throw ret.Exception;
            }
            return ret.ReturnValue;
        }

        private void LoadScript()
        {
            _scriptData = System.IO.File.ReadAllText(GetFullPath());

            _script = CSharpScript.Create<int>(
                _scriptData,
                globalsType: typeof(ScriptData),
                options: _options
            );
        }
    }
}
