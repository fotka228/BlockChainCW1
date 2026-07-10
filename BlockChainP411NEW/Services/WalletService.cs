using BlockChainP411NEW.Models;

namespace BlockChainP411NEW.Services;

public class WalletService
{
    public decimal GetBalance(Wallet wallet, string ticker)
    {
        if (wallet.Balances.TryGetValue(ticker, out var balance))
            return balance;
        return 0m;
    }

    public void AddBalance(Wallet wallet, string ticker, decimal amount)
    {
        if (!wallet.Balances.ContainsKey(ticker))
            wallet.Balances[ticker] = 0m;

        wallet.Balances[ticker] += amount;
    }
}