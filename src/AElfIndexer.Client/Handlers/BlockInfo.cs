namespace AElfIndexer.Client.Handlers;

public class BlockInfo : BlockChainDataBase
{
    public string Id { get; set; }
    public string SignerPubkey { get; set; }
    public string Signature { get; set; }
}