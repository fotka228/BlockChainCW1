
using BlockChainP411NEW.Models;
using BlockChainP411NEW.Services;

namespace BlockChainP411NEW.Services;

public class TransactionService
{
    private readonly WalletService _walletService = new();
    public decimal IcoCost { get; set; } = 100m;

    public bool ValidateTransaction(Transaction tx, Dictionary<string, Wallet> wallets, HashSet<string> registeredTokens, out string error)
    {
        error = string.Empty;

        if (tx.Type == TransactionType.ICO)
        {
            if (!wallets.TryGetValue(tx.From, out var creatorWallet))
            {
                error = "Creator of ICO does not exist!";
                return false;
            }
            decimal currentBaseBalance = _walletService.GetBalance(creatorWallet, "BASE");
            if (currentBaseBalance < IcoCost)
            {
                error = $"Insufficient BASE for ICO. Required: {IcoCost}, Available: {currentBaseBalance}";
                return false;
            }
            if (registeredTokens.Contains(tx.Ticker))
            {
                error = $"Token {tx.Ticker} already exists!";
                return false;
            }

            if (!tx.Emission.HasValue || tx.Emission.Value <= 0)
            {
                error = "Emission must be a positive value!";
                return false;
            }

            return true;
        }
        else if (tx.Type == TransactionType.Transfer)
        {
            if (!registeredTokens.Contains(tx.Ticker))
            {
                error = $"Token {tx.Ticker} does not exist in the network!";
                return false;
            }

            if (!wallets.TryGetValue(tx.From, out var sender))
            {
                error = "Sender does not exist!";
                return false;
            }

            if (!wallets.TryGetValue(tx.To, out var receiver))
            {
                error = "Receiver does not exist!";
                return false;
            }
            decimal senderTokenBalance = _walletService.GetBalance(sender, tx.Ticker);
            if (senderTokenBalance < tx.Amount)
            {
                error = $"Insufficient {tx.Ticker}. Required: {tx.Amount}, Available: {senderTokenBalance}";
                return false;
            }
            decimal senderBaseBalance = _walletService.GetBalance(sender, "BASE");
            if (senderBaseBalance < tx.Fee)
            {
                error = $"Insufficient BASE for transaction fee. Required: {tx.Fee}, Available: {senderBaseBalance}";
                return false;
            }

            return true;
        }

        error = "Unknown transaction type";
        return false;
    }

    public void ApplyTransaction(Transaction tx, Dictionary<string, Wallet> wallets, HashSet<string> registeredTokens)
    {
        if (tx.Type == TransactionType.ICO)
        {
            var creator = wallets[tx.From];
            _walletService.AddBalance(creator, tx.Ticker, tx.Emission!.Value);
            _walletService.AddBalance(creator, "BASE", -IcoCost);
            registeredTokens.Add(tx.Ticker);
        }
        else if (tx.Type == TransactionType.Transfer)
        {
            var sender = wallets[tx.From];
            var receiver = wallets[tx.To];
            _walletService.AddBalance(sender, tx.Ticker, -tx.Amount);
            _walletService.AddBalance(receiver, tx.Ticker, tx.Amount);
            _walletService.AddBalance(sender, "BASE", -tx.Fee);
        }
    }
}