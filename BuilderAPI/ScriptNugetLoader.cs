using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Scripting;
using NuGet.Common;
using NuGet.Configuration;
using NuGet.Protocol;
using NuGet.Protocol.Core.Types;
using NuGet.Versioning;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO.Compression;
using System.IO.Hashing;
using System.Text;

namespace BuilderAPI
{
    internal class ScriptNugetLoader : MetadataReferenceResolver
    {
        private readonly MetadataReferenceResolver _fallthough = ScriptMetadataResolver.Default;
        private readonly string _nugetReference = "nuget:";
        private readonly string _httpsReference = "nuget:https://";
        private readonly string _httpReference = "nuget:http://";
        private readonly string _baseNugetUrl = "https://api.nuget.org/v3/index.json";
        private readonly string _osLibaryExtention = System.OperatingSystem.IsLinux() ? "so" : "dll";
        private readonly Logger _logger;

        private bool _resolveMissingAssemblies = false;
        private bool _stickyNugetUrl = false;
        private string _cacheLocation = "";
        private string _nugetUrl = "https://api.nuget.org/v3/index.json";

        public ScriptNugetLoader(string cacheLocation, Logger logger, bool stickyNugetUrl = false)
        {
            _logger = logger;
            _cacheLocation = cacheLocation;
            _stickyNugetUrl = stickyNugetUrl;
        }

        public override bool ResolveMissingAssemblies => _resolveMissingAssemblies;

        public override bool Equals(object? other) => other is ScriptNugetLoader && Equals((ScriptNugetLoader)other);

        public override int GetHashCode() => _cacheLocation.GetHashCode();

        public override ImmutableArray<PortableExecutableReference> ResolveReference(string reference, string? baseFilePath, MetadataReferenceProperties properties)
        {
            if( reference.StartsWith(_httpsReference, StringComparison.OrdinalIgnoreCase) ||
                reference.StartsWith(_httpReference, StringComparison.OrdinalIgnoreCase) )
            {
                _nugetUrl = reference.Substring(_nugetReference.Length);
            }
            else if(reference.StartsWith(_nugetReference, StringComparison.OrdinalIgnoreCase))
            {
                if( !Directory.Exists(_cacheLocation) )
                {
                    Directory.CreateDirectory(_cacheLocation);
                }

                var nugetInfo = reference.Substring(_nugetReference.Length).Split(',', StringSplitOptions.RemoveEmptyEntries);
                var packageName = nugetInfo?[0] ?? "";
                var packageVersion = nugetInfo?.Length > 1 ? nugetInfo?[1] ?? "" : "";

                var packageFiles = Directory.GetFiles(Path.Combine(_cacheLocation, packageName, "lib"), $"*.{_osLibaryExtention}");

                if (packageFiles.Length == 0)
                {
                    DownloadPackage(packageName, packageVersion).RunSynchronously();
                    packageFiles = Directory.GetFiles(Path.Combine(_cacheLocation, packageName), $"*.{_osLibaryExtention}");
                }

                return packageFiles.Select(lib => PortableExecutableReference.CreateFromFile(lib)).ToImmutableArray();
                
            }
            return _fallthough.ResolveReference(reference, baseFilePath, properties);
        }

        public override PortableExecutableReference? ResolveMissingAssembly(MetadataReference definition, AssemblyIdentity referenceIdentity)
        {
            return _fallthough.ResolveMissingAssembly(definition, referenceIdentity);
        }

        private async Task DownloadPackage(string packageName, string packageVersion)
        {
            try
            {
                var packageSource = new PackageSource(_nugetUrl, packageName);
                var sourceRepository = Repository.Factory.GetCoreV3(packageSource);
                var resource = (await sourceRepository.GetResourceAsync<FindPackageByIdResource>())!;
                var cancelationToken = new CancellationToken();
                var cacheContext = new SourceCacheContext();
                var nullLogger = new NullLogger();

                NuGetVersion? version;
                if (String.IsNullOrEmpty(packageVersion))
                {
                    var versions = await resource.GetAllVersionsAsync(packageName, cacheContext, nullLogger, cancelationToken);
                    version = versions.OrderDescending().First();
                }
                else if (NuGetVersion.TryParse(packageVersion, out version))
                {
                    _logger.LogFunc(Microsoft.Extensions.Logging.LogLevel.Critical, "Failed to parse Nuget Version {packageVersion}", null, [packageVersion]);
                    return;
                }

                using var stream = new MemoryStream();

                if (resource != null &&
                    await resource.CopyNupkgToStreamAsync(
                        packageName,
                        version,
                        stream,
                        cacheContext,
                        nullLogger,
                        cancelationToken
                    ))
                {
                    _logger.LogFunc(Microsoft.Extensions.Logging.LogLevel.Critical, "Failed to copy package to memory stream for {packageName}", null, [packageName]);
                    return;
                }

                stream.Position = 0;
                using var archive = new ZipArchive(stream);
                await archive.ExtractToDirectoryAsync(Path.Combine(_cacheLocation, packageName), cancelationToken);
            }
            catch(Exception exeption)
            {
                _logger.LogFunc(Microsoft.Extensions.Logging.LogLevel.Critical, "Nuget download throw an error for package {packageName}", exeption, [packageName]);
            }
        }
    }
}
