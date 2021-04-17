namespace Telega.Session {
    public sealed record TgAppInfo(
        string AppVersion,
        string DeviceModel,
        string LangCode,
        string SystemVersion,
        string SystemLangCode,
        string LangPack
    ) {
        public static TgAppInfo Default => new(
            AppVersion: "1.0.0",
            DeviceModel: "PC",
            LangCode: "en",
            SystemVersion: "Win 10.0",
            SystemLangCode: "en",
            LangPack: "tdesktop"
        );
    }
}