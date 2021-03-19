using System;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using LanguageExt;
using LanguageExt.SomeHelp;
using Telega.Rpc.Dto.Generator.Generation;
using Telega.Rpc.Dto.Generator.TgScheme;

namespace Telega.Rpc.Dto.Generator {
    static class Program {
        // https://github.com/telegramdesktop/tdesktop/commits/dev/Telegram/Resources

        // layer 124
        static readonly string[] SchemeUrls = {
            "https://raw.githubusercontent.com/telegramdesktop/tdesktop/1fc24398a03818a8fa228e9c4dba0966b30055bd/Telegram/Resources/tl/api.tl",
            "https://raw.githubusercontent.com/telegramdesktop/tdesktop/1fc24398a03818a8fa228e9c4dba0966b30055bd/Telegram/Resources/tl/mtproto.tl"
        };

        static string[] DownloadLatestTgScheme() =>
            SchemeUrls.Map(x => new WebClient().DownloadString(x)).ToArray();

        static async Task Main() {
            var rawScheme = DownloadLatestTgScheme();
            var scheme = rawScheme
               .Map(SomeExt.ToSome)
               .Map(TgSchemeParser.Parse)
               .Reduce(Scheme.Merge)
               .Apply(SomeExt.ToSome).Apply(TgSchemePatcher.Patch)
               .Apply(SomeExt.ToSome).Apply(TgSchemeNormalizer.Normalize);
            var files = Gen.GenTypes(scheme).Concat(Gen.GenFunctions(scheme))
               .Concat(new[] { Gen.GenSchemeInfo(scheme) });

            FileSync.Clear();
            foreach (var file in files.AsParallel().WithDegreeOfParallelism(Environment.ProcessorCount)) {
                await FileSync.Sync(file);
            }
        }
    }
}