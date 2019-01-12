using LanguageExt;

namespace Telega.Rpc.Dto.Generator.Generation
{
    class GenFile
    {
        public readonly string Namespace;
        public readonly string Name;
        public readonly string Content;

        public GenFile(Some<string> ns, Some<string> name, Some<string> content)
        {
            Namespace = ns;
            Name = name;
            Content = content;
        }
    }
}
