using MessagePack;

namespace BAMWallet.Model;

[MessagePackObject]
public class Peer
{
    [Key(0)] public byte[] IpAddress { get; init; }
    [Key(1)] public uint NodeId { get; init; }
    [Key(2)] public byte[] TcpPort { get; set; }
    [Key(3)] public byte[] Name { get; set; }
    [Key(4)] public byte[] PublicKey { get; set; }
    [Key(5)] public byte[] Version { get; set; }
}