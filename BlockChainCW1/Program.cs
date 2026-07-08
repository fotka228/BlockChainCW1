using SecureBlockChainApp.Models;
using SecureBlockChainApp.Services;
using System;
using System.Text;
using System.Threading.Tasks;

namespace SecureBlockChainApp
{
    internal class Program
    {
        static async Task Main(string[] args)
        {
            Console.OutputEncoding = Encoding.UTF8;

            var displayService = new BlockChainDisplayService();
            var blockChain1 = new BlockChainService(initialDifficulty: 4);
            var walletService = new WalletService(blockChain1.Chain);
            var transactionService = new TransactionService(blockChain1.Chain);

            Console.WriteLine("First Validation:");
            bool isBlockchainValidOnStart = blockChain1.IsValid();
            displayService.PrintValidationResult(isBlockchainValidOnStart);

            var walletAlice = walletService.CreateWallet("Alice");
            var walletBob = walletService.CreateWallet("Denys");

            while (true)
            {
                Console.WriteLine("\nBlockChain Menu");
                Console.WriteLine("1. Mine Block");
                Console.WriteLine("2. Create Transaction ");
                Console.WriteLine("3. Show Alice Balance");
                Console.WriteLine("4. Show Denys Balance");
                Console.WriteLine("5. Validate Blockchain");
                Console.WriteLine("6. Print Blockchain");
                Console.WriteLine("7. Change Blockchain");
                Console.WriteLine("8. Exit");
                Console.Write("Your choice: ");

                var choice = Console.ReadLine();
                switch (choice)
                {
                    case "1":

                        await blockChain1.AddBlockAsync(walletAlice.Address);
                        break;

                    case "2":
                        try
                        {
                            var tx = transactionService.CreateTransaction(walletAlice, walletBob.Address, 10m, walletAlice.PublicKey);
                            blockChain1.AddTransactionToMemPool(tx);
                            Console.WriteLine("Transaction successfully added to MemPool!");
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine($"Error creating transaction: {ex.Message}");
                        }
                        break;

                    case "3":
                        Console.WriteLine($"Alice's balance: {walletService.GetBalance(walletAlice.Address)} coins.");
                        break;

                    case "4":
                        Console.WriteLine($"Denys's balance: {walletService.GetBalance(walletBob.Address)} coins.");
                        break;

                    case "5":
                        bool isValid = blockChain1.IsValid();
                        displayService.PrintValidationResult(isValid);
                        break;

                    case "6":
                        displayService.PrintBlockChain(blockChain1.Chain);
                        break;

                    case "7":
                        if (blockChain1.Chain.Count > 1 && blockChain1.Chain[1].Transactions.Count > 0)
                        {
                            blockChain1.Chain[1].Transactions[0].Amount = 9999;
                            Console.WriteLine("Data in memory tampered! Try to execute validation (Option 5).");
                        }
                        else
                        {
                            Console.WriteLine("Hasnt block with transactions for modification. First, execute options 2 and 1.");
                        }
                        break;

                    case "8":
                        return;

                    default:
                        Console.WriteLine("incorrect choice. Try again.");
                        break;
                }
            }
        }
    }
}