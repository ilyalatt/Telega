using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using LanguageExt;
using Telega.Rpc.Dto.Generator.Generation;
using static LanguageExt.Prelude;

namespace Telega.Rpc.Dto.Generator {
    public class FileSyncContext {
        public string RootPath { get; init; }
        public string DtoDirPath { get; init; }

        static string FindTelegaRootPath(string currentDirectory) {
            while (currentDirectory != null) {
                var hasSlnFile = Directory.GetFiles(currentDirectory, "Telega.sln").Any();
                if (hasSlnFile) {
                    Console.WriteLine(currentDirectory);
                    return currentDirectory;
                }
                currentDirectory = Path.GetDirectoryName(currentDirectory);
            }

            throw new("Can not find Telega root directory.");
        }

        public static FileSyncContext Extract(string entryDirectory) {
            var rootPath = FindTelegaRootPath(entryDirectory);
            var dtoDirPath = Path.Combine(rootPath, "Telega/Rpc/Dto");
            return new() { RootPath = rootPath, DtoDirPath = dtoDirPath };
        }
    }
    
    static class FileSync {
        static string NamespaceToPath(string ns) =>
            ns.Replace(".", "/");

        static IEnumerable<string> GetOutputDirs(FileSyncContext ctx) {
            return new[] { "Types", "Functions" }.Map(dir => Path.Combine(ctx.DtoDirPath, dir));
        }

        public static bool IsClear(FileSyncContext ctx) =>
            !GetOutputDirs(ctx).Exists(Directory.Exists);

        public static void Clear(FileSyncContext ctx) =>
            GetOutputDirs(ctx)
               .Filter(Directory.Exists)
               .Iter(dir => Directory.Delete(dir, recursive: true));

        public static Option<int> GetCurrentLayerVersion(FileSyncContext ctx) {
            var schemeInfoPath = Path.Combine(ctx.DtoDirPath, "SchemeInfo.cs");
            if (!File.Exists(schemeInfoPath)) {
                return None;
            }

            var text = File.ReadAllText(schemeInfoPath);
            var regex = new Regex(@"LayerVersion = (\d+);");
            var match = regex.Match(text);
            if (match == null) {
                throw new("Can not extract layer version from SchemeInfo.cs file.");
            }

            var layerVersion = int.Parse(match.Groups[1].Value);
            return layerVersion;
        }

        public static void Sync(FileSyncContext ctx, Some<GenFile> someFile) {
            const string prefix = "Telega.Rpc.Dto";
            var file = someFile.Value;
            if (!file.Namespace.StartsWith(prefix)) {
                throw new($"A file namespace must start with '{prefix}' prefix.");
            }

            var fileSubDir = file.Namespace[prefix.Length..].TrimStart('.').Apply(NamespaceToPath);
            var filePathDir = Path.Combine(ctx.DtoDirPath, fileSubDir);
            var filePath = Path.Combine(filePathDir, file.Name + ".cs");

            if (!Directory.Exists(filePathDir)) {
                Directory.CreateDirectory(filePathDir);
            }

            File.WriteAllText(filePath, file.Content);
        }
    }
}