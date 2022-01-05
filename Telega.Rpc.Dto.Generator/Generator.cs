using System;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using LanguageExt;
using LanguageExt.SomeHelp;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;
using Telega.Rpc.Dto.Generator.Generation;
using Telega.Rpc.Dto.Generator.TgScheme;

namespace Telega.Rpc.Dto.Generator {
    [Generator]
    public sealed class Generator : ISourceGenerator {
        // This generator is called when you try to build Telega project
        // It is a [source generator](https://devblogs.microsoft.com/dotnet/introducing-c-source-generators/)
        // It is called each time some file in Telega changes
        // If you change the generator code but there are no Telega changes and you try to rebuild Telega usually it WILL NOT run the update generator
        // Because of that usually you need to change some Telega file (you can just insert a new line for example)
        
        // To update API layer you need to change `Layer` and `CommitHash` constants below
        // 1. Open https://github.com/telegramdesktop/tdesktop/commits/dev/Telegram/Resources
        // 2. Find the latest commit that looks like `Update API scheme on layer 136`
        // 3. Replace `Layer` and `CommitHash` with the found commit hash
        // 4. Make some change in `Telega` project to make a trigger for the generator
        // 5. Run `dotnet build Telega`
        
        // Sometimes you want to see the generated code
        // If you use Rider then you can press Ctrl and click on the needed class
        // Also you can debug the generator via `Telega.Rpc.Dto.Generator.Debug`
        
        // TODO: Transform into an incremental generator
        // https://andrewlock.net/exploring-dotnet-6-part-9-source-generator-updates-incremental-generators/

        static readonly int Layer = 136;
        static readonly string CommitHash = "118072db77553a070fb740a961aedfef323f72ef";
        static readonly string RepoPath = $"https://raw.githubusercontent.com/telegramdesktop/tdesktop/{CommitHash}/Telegram/Resources/tl";
        static readonly string[] SchemeUrls = { $"{RepoPath}/api.tl", $"{RepoPath}/mtproto.tl" };

        static string[] DownloadLatestTgScheme() =>
            SchemeUrls.AsParallel().Select(x => new WebClient().DownloadString(x)).ToArray();

        public static void Sync(GeneratorExecutionContext? contextOption = null) {
            var rawScheme = DownloadLatestTgScheme();
            var scheme = rawScheme
                .AsParallel()
                .Map(SomeExt.ToSome)
                .Map(TgSchemeParser.Parse)
                .Reduce(Scheme.Merge)
                .Apply(SomeExt.ToSome).Apply(TgSchemePatcher.Patch)
                .Apply(SomeExt.ToSome).Apply(TgSchemeNormalizer.Normalize);
            if (scheme.LayerVersion != Layer) {
                throw new Exception("Layer constant in Generator must be updated to match the fetched scheme.");
            }

            var files = Gen.GenTypes(scheme).Concat(Gen.GenFunctions(scheme))
                .Concat(new[] { Gen.GenSchemeInfo(scheme) });


            if (contextOption == null) {
                return;
            }

            var context = contextOption.Value;
            files.Iter(x => context.AddSource(
                $"{x.Namespace}.{x.Name}",
                SourceText.From(x.Content, Encoding.UTF8))
            );
        }

        public void Initialize(GeneratorInitializationContext context) { }

        public void Execute(GeneratorExecutionContext context) => Sync(context);
    }
}
