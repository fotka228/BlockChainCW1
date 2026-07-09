using System;
using System.Text;

namespace BlockChainP411NEW.Models
{
    public enum TransactionType
    {
        Transfer,
        ICO
    }
    public class Transaction
    {
        public string Id { get; set; }
        public string From { get; set; }
        public string To { get; set; }
        public decimal Amount { get; set; }
        public DateTime TimeStamp { get; set; }
        public decimal Fee { get; set; }
        public byte[] SenderPublicKey { get; set; }
        public byte[] Signature { get; set; }
        public TransactionType Type { get; set; } = TransactionType.Transfer;
        public string TokenSymbol { get; set; } = "BASE";
        public decimal? TotalSupply { get; set; }

        public Transaction(string from, string to, decimal amount, byte[] senderPublicKey)
        {
            Id = Guid.NewGuid().ToString();
            From = from;
            To = to;
            Amount = amount;
            TimeStamp = DateTime.UtcNow;
            SenderPublicKey = senderPublicKey;
            TokenSymbol = "BASE";
            Fee = 0;
            Type = TransactionType.Transfer;
        }
        public Transaction(string from, string tokenSymbol, decimal totalSupply, byte[] senderPublicKey, bool isIco)
        {
            Id = Guid.NewGuid().ToString();
            From = from;
            To = from;
            Amount = totalSupply;
            TimeStamp = DateTime.UtcNow;
            SenderPublicKey = senderPublicKey;
            Type = TransactionType.ICO;
            TokenSymbol = tokenSymbol;
            TotalSupply = totalSupply;
            Fee = 100;
        }

        public string ToHashString()
        {
            var sig = Signature != null ? Convert.ToHexString(Signature) : string.Empty;
            return $"{Id}|{From}->{To}|{Amount}|{TimeStamp:O}|{Fee}|{TokenSymbol}|{Type}|{TotalSupply}|{sig}";
        }
        public byte[] GetDataToSign()
        {
            string raw = $"{Id}{From}{To}{Amount}{TimeStamp:O}{TokenSymbol}{Type}{TotalSupply}";
            return Encoding.UTF8.GetBytes(raw);
        }
        public int GetSizeInBytes()
        {
            string payload = $"{Id}|{From}->{To}|{Amount}|{TimeStamp:O}|{Fee}|{TokenSymbol}|{Type}|{TotalSupply}";
            return Encoding.UTF8.GetByteCount(payload);
        }
    }
}