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
        const string BaseNamespace = Project + ".Rpc.Dto";
        static readonly string BasePath = Path.Combine("..", Project);

        public static async Task Sync(Some<GenFile> someFile)
        {
            if (!Directory.Exists(BasePath)) throw new Exception("WTF");

            var file = someFile.Value;
            if (!file.Namespace.StartsWith(BaseNamespace)) throw new Exception("WTF");
            var fileSubDir = file.Namespace.Substring(Project.Length).TrimStart('.').Replace(".", "/");
            var filePathDir = Path.Combine(BasePath, fileSubDir);
            var filePath = Path.Combine(filePathDir, file.Name + ".cs");

            if (!Directory.Exists(filePathDir)) Directory.CreateDirectory(filePathDir);
            await File.WriteAllTextAsync(filePath, file.Content);
        }
    }
}
