namespace BlockChainP411NEW.Models;

public enum TransactionType
{
    Transfer,
    ICO
}

public class Transaction
{
    public TransactionType Type { get; set; } = TransactionType.Transfer;
    public string Ticker { get; set; } = "BASE";
    public string From { get; set; } = string.Empty;
    public string To { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public decimal Fee { get; set; }
    public long Timestamp { get; set; }
    public decimal? Emission { get; set; } // Заповнюється тільки для ICO

    public Transaction() { }

    public Transaction(TransactionType type, string ticker, string from, string to, decimal amount, decimal fee, decimal? emission = null)
    {
        Type = type;
        Ticker = ticker;
        From = from;
        To = to;
        Amount = amount;
        Fee = fee;
        Timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        Emission = emission;
    }
}