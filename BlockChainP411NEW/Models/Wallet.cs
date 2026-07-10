namespace BlockChainP411NEW.Models;

public class Wallet
{
    public string Address { get; set; } = string.Empty;
    public string PrivateKey { get; set; } = string.Empty;

    public Dictionary<string, decimal> Balances { get; set; } = new();

    public Wallet() { }

    public Wallet(string address, string privateKey)
    {
        Address = address;
        PrivateKey = privateKey;
        Balances["BASE"] = 0m;
    }
}