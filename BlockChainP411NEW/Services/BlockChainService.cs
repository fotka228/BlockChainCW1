using BlockChainP411NEW.Models;
using BlockChainP411NEW.Services;

namespace BlockChainP411NEW.Services;

public class BlockChainService
{
    private readonly List<Block> _chain = new();
    private readonly List<Transaction> _pendingTransactions = new();
    private readonly Dictionary<string, Wallet> _wallets = new();
    private readonly HashSet<string> _tokenRegistry = new HashSet<string> { "BASE" };
    private readonly object _lock = new();

    private readonly TransactionService _transactionService = new();

    public int Difficulty { get; set; } = 2;
    public decimal BlockReward { get; set; } = 50m;

    public List<Block> Chain => _chain;
    public List<Transaction> PendingTransactions => _pendingTransactions;
    public Dictionary<string, Wallet> Wallets => _wallets;
    public HashSet<string> TokenRegistry => _tokenRegistry;

    public BlockChainService()
    {
        var genesisTx = new Transaction(TransactionType.ICO, "BASE", "system", "system", 0, 0, 0);
        var genesisBlock = new Block(0, new List<Transaction> { genesisTx }, "0", "system")
        {
            Hash = "0000_GENESIS_HASH_0000"
        };
        _chain.Add(genesisBlock);
    }

    public Wallet CreateWallet(string address, string privateKey)
    {
        lock (_lock)
        {
            var wallet = new Wallet(address, privateKey);
            _wallets[address] = wallet;
            return wallet;
        }
    }

    public bool AddTransaction(Transaction tx, out string error)
    {
        lock (_lock)
        {
            if (!_transactionService.ValidateTransaction(tx, _wallets, _tokenRegistry, out error))
                return false;

            _transactionService.ApplyTransaction(tx, _wallets, _tokenRegistry);
            _pendingTransactions.Add(tx);
            return true;
        }
    }

    public Block? MineBlock(string minerAddress)
    {
        lock (_lock)
        {
            if (!_wallets.ContainsKey(minerAddress)) return null;

            var rewardTx = new Transaction(TransactionType.Transfer, "BASE", "system", minerAddress, BlockReward, 0);
            var blockTxs = new List<Transaction>(_pendingTransactions) { rewardTx };

            var lastBlock = _chain.Last();
            var block = new Block(_chain.Count, blockTxs, lastBlock.Hash, minerAddress);

            int nonce = 0;
            string prefix = new string('0', Difficulty);
            while (true)
            {
                block.Nonce = nonce;
                string hash = Convert.ToBase64String(System.Security.Cryptography.SHA256.HashData(
                    System.Text.Encoding.UTF8.GetBytes(block.Index + block.PreviousHash + block.Nonce + minerAddress)));
                if (hash.StartsWith(prefix))
                {
                    block.Hash = hash;
                    break;
                }
                nonce++;
            }

            var walletService = new WalletService();
            walletService.AddBalance(_wallets[minerAddress], "BASE", BlockReward);

            _chain.Add(block);
            _pendingTransactions.Clear();
            return block;
        }
    }

    public void RollbackToHeight(int height)
    {
        lock (_lock)
        {
            var walletService = new WalletService();
            while (_chain.Count > height + 1)
            {
                var removedBlock = _chain.Last();
                foreach (var tx in removedBlock.Transactions)
                {
                    if (tx.Type == TransactionType.Transfer && tx.From != "system")
                    {
                        walletService.AddBalance(_wallets[tx.From], tx.Ticker, tx.Amount);
                        walletService.AddBalance(_wallets[tx.From], "BASE", tx.Fee);
                        walletService.AddBalance(_wallets[tx.To], tx.Ticker, -tx.Amount);
                    }
                    else if (tx.Type == TransactionType.ICO && tx.From != "system")
                    {
                        walletService.AddBalance(_wallets[tx.From], tx.Ticker, -tx.Emission!.Value);
                        walletService.AddBalance(_wallets[tx.From], "BASE", _transactionService.IcoCost);
                        _tokenRegistry.Remove(tx.Ticker);
                    }
                }
                _chain.RemoveAt(_chain.Count - 1);
            }
            _pendingTransactions.Clear();
        }
    }
}