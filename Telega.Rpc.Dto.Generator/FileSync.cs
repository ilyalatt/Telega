using System;
using System.IO;
using System.Threading.Tasks;
using LanguageExt;
using Telega.Rpc.Dto.Generator.Generation;

namespace Telega.Rpc.Dto.Generator
{
    static class FileSync
    {
        const string Project = "Telega";
        const string RpcDtoNamespace = "Rpc.Dto";
        const string BaseNamespace = Project + "." + RpcDtoNamespace;
        static readonly string BasePath = Path.Combine("..", Project);

        static string NamespaceToPath(string ns) =>
            ns.Replace(".", "/");

        public static void Clear()
        {
            var dtoPath = Path.Combine(BasePath, NamespaceToPath(RpcDtoNamespace));
            if (!Directory.Exists(BasePath)) throw new Exception("WTF");

            new[] { "Types", "Functions" }
            .Map(dir => Path.Combine(dtoPath, dir))
            .Filter(Directory.Exists)
            .Iter(dir => Directory.Delete(dir, recursive: true));
        }

        public static async Task Sync(Some<GenFile> someFile)
        {
            if (!Directory.Exists(BasePath)) throw new Exception("WTF");

            var file = someFile.Value;
            if (!file.Namespace.StartsWith(BaseNamespace)) throw new Exception("WTF");
            var fileSubDir = file.Namespace.Substring(Project.Length).TrimStart('.').Apply(NamespaceToPath);
            var filePathDir = Path.Combine(BasePath, fileSubDir);
            var filePath = Path.Combine(filePathDir, file.Name + ".cs");

            if (!Directory.Exists(filePathDir)) Directory.CreateDirectory(filePathDir);
            await File.WriteAllTextAsync(filePath, file.Content);
        }
    }
}
