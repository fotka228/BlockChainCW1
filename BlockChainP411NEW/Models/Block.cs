namespace BlockChainP411NEW.Models;

public class Block
{
    public int Index { get; set; }
    public long Timestamp { get; set; }
    public List<Transaction> Transactions { get; set; } = new();
    public string PreviousHash { get; set; } = string.Empty;
    public string Hash { get; set; } = string.Empty;
    public int Nonce { get; set; }
    public string MerkleRoot { get; set; } = string.Empty;
    public string MinerAddress { get; set; } = string.Empty;

    public Block() { }

    public Block(int index, List<Transaction> transactions, string previousHash, string minerAddress)
    {
        Index = index;
        Timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        Transactions = transactions;
        PreviousHash = previousHash;
        MinerAddress = minerAddress;
    }
}