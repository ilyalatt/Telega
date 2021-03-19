using LanguageExt;

namespace Telega.Rpc.Dto.Generator.Generation {
    class GenFile {
        public string Namespace { get; }
        public string Name { get; }
        public string Content { get; }

        public GenFile(Some<string> ns, Some<string> name, Some<string> content) {
            Namespace = ns;
            Name = name;
            Content = content;
        }
    }
}