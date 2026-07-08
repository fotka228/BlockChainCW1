using SecureBlockChainApp.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace SecureBlockChainApp.Services
{
    public class BlockChainService
    {
        private readonly HashingService _hashingService;
        private readonly MiningService _miningService;
        private readonly TransactionService _transactionService;
        private readonly WalletService _walletService;
        private readonly FileStorageService _fileStorageService;

        public List<Block> Chain { get; set; }
        public List<Transaction> PendingTransactions { get; set; } = new List<Transaction>();

        public int Difficulty { get; set; }
        public double _targetBlockTime = 10;
        private readonly int _adjustmentInterval = 10;
        private readonly decimal _rewardAmount = 50;
        private readonly int maxTransactionAmount = 2;

        public BlockChainService(int initialDifficulty = 4)
        {
            Difficulty = initialDifficulty;
            Chain = new List<Block>();
            _fileStorageService = new FileStorageService();
            _hashingService = new HashingService();
            _miningService = new MiningService(_hashingService);

            var loadedChain = _fileStorageService.LoadBlockChain();

            if (loadedChain != null && loadedChain.Count > 0)
            {
                Chain = loadedChain;
                Difficulty = Chain.Last().Difficulty;
            }
            else
            {
                CreateGenesisBlock();
                _fileStorageService.SaveBlockChain(Chain);
            }

            _walletService = new WalletService(Chain);
            _transactionService = new TransactionService(Chain);
        }

        private void CreateGenesisBlock()
        {
            Block genesisBlock = new Block(0, DateTime.UtcNow, new List<Transaction>(), "0", Difficulty);
            genesisBlock.Hash = _hashingService.ComputeHash(genesisBlock);
            Chain.Add(genesisBlock);
        }

        public async Task AddBlockAsync(string minerAddress)
        {
            foreach (var transaction in PendingTransactions)
            {
                if (!_transactionService.ValidateTransaction(transaction).IsValid)
                {
                    throw new InvalidOperationException($"Invalid transaction: {transaction.Id}");
                }
            }

            var sortedTransactions = PendingTransactions
                .OrderByDescending(t => t.Fee)
                .Take(maxTransactionAmount)
                .ToList();

            var totalReward = sortedTransactions.Sum(t => t.Fee) + _rewardAmount;
            var rewardTransaction = new Transaction("COINBASE", minerAddress, totalReward, null);
            sortedTransactions.Add(rewardTransaction);

            Block previousBlock = Chain.Last();
            Block newBlock = new Block(
                previousBlock.Index + 1,
                DateTime.UtcNow,
                sortedTransactions,
                previousBlock.Hash,
                Difficulty
            );

            Console.WriteLine("Mining block via Multi-threading...");
            await _miningService.MineBlockAsync(newBlock, Difficulty, CancellationToken.None);
            Console.WriteLine($"Block mined! Hash: {newBlock.Hash}");

            Chain.Add(newBlock);
            PendingTransactions.RemoveAll(t => sortedTransactions.Contains(t));

            if (newBlock.Index % _adjustmentInterval == 0)
            {
                AdjustDifficulty();
            }

            _fileStorageService.SaveBlockChain(Chain);
        }

        public void AddTransactionToMemPool(Transaction transaction)
        {
            var validation = _transactionService.ValidateTransaction(transaction);
            if (!validation.IsValid)
            {
                throw new ArgumentException(validation.ErrorMessage);
            }
            if (transaction.From != "COINBASE")
            {
                var senderBalance = _walletService.GetBalance(transaction.From);
                if (senderBalance < transaction.Amount + transaction.Fee)
                {
                    throw new InvalidOperationException($"Insufficient balance for transaction: {transaction.Id}");
                }
            }
            PendingTransactions.Add(transaction);
        }

        public void AdjustDifficulty()
        {
            if ((Chain.Count - 1) % _adjustmentInterval != 0 || Chain.Count <= 1) return;

            var recentBlocks = Chain.Skip(Chain.Count - _adjustmentInterval).ToList();
            double avgTime = recentBlocks.Average(b => b.MiningDuration);

            if (avgTime < _targetBlockTime) Difficulty++;
            else if (avgTime > _targetBlockTime) Difficulty = Math.Max(1, Difficulty - 1);
        }

        public bool IsValid()
        {
            for (int i = 1; i < Chain.Count; i++)
            {
                var currentBlock = Chain[i];
                var previousBlock = Chain[i - 1];

                if (currentBlock.Hash != _hashingService.ComputeHash(currentBlock)) return false;
                if (currentBlock.PreviousHash != previousBlock.Hash) return false;
                if (currentBlock.MiningDuration < 0) return false;
                if (currentBlock.TimeStamp <= previousBlock.TimeStamp) return false;

                double physicalTimeDiff = (currentBlock.TimeStamp - previousBlock.TimeStamp).TotalSeconds;
                double maxAllowedDuration = physicalTimeDiff + 2.0;
                if (currentBlock.MiningDuration > maxAllowedDuration) return false;

                foreach (var tx in currentBlock.Transactions)
                {
                    if (tx.From == "COINBASE") continue;

                    bool isSignatureValid = _walletService.VerifySignature(tx.From, tx.GetDataToSign(), tx.Signature);
                    if (!isSignatureValid)
                    {
                        var oldColor = Console.ForegroundColor;
                        Console.ForegroundColor = ConsoleColor.Red;
                        Console.WriteLine($"\n[critical error]: Found a forged transaction in block {currentBlock.Index}!");
                        Console.ForegroundColor = oldColor;
                        return false;
                    }
                }
            }
            return true;
        }

        public Block FindBlockByHash(string targetHash)
        {
            return Chain.FirstOrDefault(b => b.Hash.Equals(targetHash, StringComparison.OrdinalIgnoreCase));
        }
    }
}