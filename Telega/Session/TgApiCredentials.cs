namespace Telega.Session {
    public sealed record TgApiCredentials(
        int Id,
        string Hash
    ) {
        public static TgApiCredentials Test = new(
            Id: 17349,
            Hash: "344583e45741c457fe1862106095a5eb"
        );
    }
}