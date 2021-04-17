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
        // https://github.com/telegramdesktop/tdesktop/commits/dev/Telegram/Resources

        static readonly int Layer = 124;
        static readonly string CommitHash = "1fc24398a03818a8fa228e9c4dba0966b30055bd";
        static readonly string RepoPath = $"https://raw.githubusercontent.com/telegramdesktop/tdesktop/{CommitHash}/Telegram/Resources/tl";
        static readonly string[] SchemeUrls = { $"{RepoPath}/api.tl", $"{RepoPath}/mtproto.tl" };

        static string[] DownloadLatestTgScheme() =>
            SchemeUrls.AsParallel().Select(x => new WebClient().DownloadString(x)).ToArray();

        // TODO: Tru to use caching interface when it become public
        // https://github.com/dotnet/roslyn/blob/main/docs/features/source-generators.cookbook.md#participate-in-the-ide-experience
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

            // var ctx = FileSyncContext.Extract("/home/ilyalatt/dev/Telega/Telega");
            // FileSync.Clear(ctx);
            // foreach (var file in files.AsParallel().WithDegreeOfParallelism(Environment.ProcessorCount)) {
            //     FileSync.Sync(ctx, file);
            // }
            //
            // return;

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