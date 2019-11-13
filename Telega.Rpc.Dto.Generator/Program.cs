using System;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using LanguageExt;
using LanguageExt.SomeHelp;
using Telega.Rpc.Dto.Generator.Generation;
using Telega.Rpc.Dto.Generator.TgScheme;

namespace Telega.Rpc.Dto.Generator
{
    static class Program
    {
        // https://github.com/telegramdesktop/tdesktop/commits/dev/Telegram/Resources/scheme.tl

        // layer 104
        const string SchemeUrl = "https://raw.githubusercontent.com/telegramdesktop/tdesktop/4544a2e331597eae48fad93b0fbb583ecc91f7c4/Telegram/Resources/scheme.tl";
        static string DownloadLatestTgScheme() =>
            new WebClient().DownloadString(SchemeUrl);

        static async Task Main()
        {
            var rawScheme = DownloadLatestTgScheme();
            var scheme = TgSchemeParser.Parse(rawScheme)
                .Apply(SomeExt.ToSome).Apply(TgSchemePatcher.Patch)
                .Apply(SomeExt.ToSome).Apply(TgSchemeNormalizer.Normalize);
            var files = Gen.GenTypes(scheme).Concat(Gen.GenFunctions(scheme)).Concat(new[] { Gen.GenSchemeInfo(scheme) });

            FileSync.Clear();
            foreach (var file in files.AsParallel().WithDegreeOfParallelism(Environment.ProcessorCount)) await FileSync.Sync(file);
        }
    }
}
