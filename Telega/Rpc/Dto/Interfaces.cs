using System.IO;

namespace Telega.Rpc.Dto {
    // it should not be ITgSerializable
    interface ITgTypeTag {
        uint TypeNumber { get; }
        void SerializeTag(BinaryWriter bw);
    }

    public interface ITgSerializable {
        void Serialize(BinaryWriter bw);
    }

    public interface ITgType : ITgSerializable { }

    public interface ITgFunc<out TRes> : ITgSerializable {
        TRes DeserializeResult(BinaryReader br);
    }
}