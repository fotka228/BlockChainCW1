using BlockChainP411NEW.Models;
using BlockChainP411NEW.Services;
using System;
using System.Threading.Tasks;

namespace BlockChain_1
{
    internal class Program
    {
        static async Task Main(string[] args)
        {
            var displayService = new BlockChainDisplayService();
            var blockChain = new BlockChainService(initialDifficulty: 4);
            var walletService = new WalletService(blockChain.Chain);
            var transactionService = new TransactionService(blockChain.Chain);

            var alice = walletService.CreateWallet("Alice");
            var bob = walletService.CreateWallet("Bob");
            Console.WriteLine($"Alice: {alice.Address}");
            Console.WriteLine($"Bob:   {bob.Address}\n");

            await RunDemo(blockChain, transactionService, walletService, alice, bob);
            await MainMenu(blockChain, transactionService, walletService, displayService, alice, bob);
        }
        static async Task RunDemo(BlockChainService chain, TransactionService txService,
            WalletService walletService, Wallet alice, Wallet bob)
        {
            Console.WriteLine("\n Demo proof\n");
            Console.WriteLine("1. Alice mines BASE coins...");
            for (int i = 0; i < 7; i++)
            {
                await chain.MineBlock(alice.Address);
                Console.WriteLine($"   Block #{chain.Chain.Count - 1}. Alice BASE: {walletService.GetBalance(alice.Address, "BASE")}");
            }
            Console.WriteLine("\n2. Alice creates 1000 ALICE_COIN tokens...");
            try
            {
                var icoTx = txService.CreateICOToken(alice, "ALICE_COIN", 1000);
                chain.AddTransactionToMemPool(icoTx);
                await chain.MineBlock(alice.Address);
                Console.WriteLine("ALICE_COIN created successfully!");
                Console.WriteLine($"Alice ALICE_COIN: {walletService.GetBalance(alice.Address, "ALICE_COIN")}");
                Console.WriteLine($"Alice BASE: {walletService.GetBalance(alice.Address, "BASE")}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: {ex.Message}");
            }

            Console.WriteLine("\n3. Bob tries to create BOB_COIN (no BASE)...");
            try
            {
                var icoTx = txService.CreateICOToken(bob, "BOB_COIN", 1000);
                chain.AddTransactionToMemPool(icoTx);
                await chain.MineBlock(bob.Address);
                Console.WriteLine("Transaction passed (should fail!)");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Rejected: {ex.Message}");
            }

            Console.WriteLine("\n4. Bob tries to create ALICE_COIN (theft)...");
            try
            {
                var icoTx = txService.CreateICOToken(bob, "ALICE_COIN", 1000);
                chain.AddTransactionToMemPool(icoTx);
                await chain.MineBlock(bob.Address);
                Console.WriteLine("Transaction passed (should fail!)");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Rejected: {ex.Message}");
            }

            Console.WriteLine("\n5. Alice sends 200 ALICE_COIN to Bob...");
            try
            {
                var tx = txService.CreateTransaction(alice, bob.Address, 200, "ALICE_COIN");
                chain.AddTransactionToMemPool(tx);
                await chain.MineBlock(alice.Address);
                Console.WriteLine("Transfer successful!");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: {ex.Message}");
            }
            Console.WriteLine("\n6. Final balances:");
            PrintBalances(walletService, alice, "Alice");
            PrintBalances(walletService, bob, "Bob");
        }

        static void PrintBalances(WalletService walletService, Wallet wallet, string name)
        {
            var balances = walletService.GetAllBalances(wallet.Address);
            Console.WriteLine($"\n{name}:");
            foreach (var kvp in balances)
                if (kvp.Value != 0)
                    Console.WriteLine($"   {kvp.Key}: {kvp.Value:F4}");
        }

        static async Task MainMenu(BlockChainService chain, TransactionService txService,
            WalletService walletService, BlockChainDisplayService display,
            Wallet alice, Wallet bob)
        {
            while (true)
            {
                Console.WriteLine("\n BlockChain Menu");
                Console.WriteLine("1. Show balances");
                Console.WriteLine("2. Send BASE");
                Console.WriteLine("3. Send custom token");
                Console.WriteLine("4. Create token");
                Console.WriteLine("5. Mine block");
                Console.WriteLine("6. Validate chain");
                Console.WriteLine("7. Print chain");
                Console.WriteLine("8. Token info");
                Console.WriteLine("9. Exit");

                Console.Write("\nChoose: ");
                var choice = Console.ReadLine();

                switch (choice)
                {
                    case "1":
                        PrintBalances(walletService, alice, "Alice");
                        PrintBalances(walletService, bob, "Bob");
                        break;
                    case "2":
                        await SendToken(chain, txService, alice, bob.Address, "BASE");
                        break;
                    case "3":
                        Console.Write("Token symbol: ");
                        var token = Console.ReadLine()?.ToUpperInvariant();
                        if (!string.IsNullOrEmpty(token))
                            await SendToken(chain, txService, alice, bob.Address, token);
                        break;
                    case "4":
                        await CreateToken(chain, txService, alice);
                        break;
                    case "5":
                        await chain.MineBlock(alice.Address);
                        Console.WriteLine("Block mined.");
                        break;
                    case "6":
                        display.PrintChainValidity(chain.IsValid());
                        break;
                    case "7":
                        display.PrintChain(chain.Chain);
                        break;
                    case "8":
                        Console.Write("Token symbol: ");
                        var infoToken = Console.ReadLine()?.ToUpperInvariant();
                        if (!string.IsNullOrEmpty(infoToken))
                        {
                            var info = walletService.GetTokenInfo(infoToken);
                            if (info.HasValue)
                                Console.WriteLine($"Token: {infoToken}, Creator: {info.Value.Creator}, Supply: {info.Value.TotalSupply}");
                            else
                                Console.WriteLine($"Token {infoToken} not found.");
                        }
                        break;
                    case "9":
                        return;
                    default:
                        Console.WriteLine("Invalid option.");
                        break;
                }
            }
        }

        static async Task SendToken(BlockChainService chain, TransactionService txService,
            Wallet sender, string to, string symbol)
        {
            Console.Write($"Amount of {symbol}: ");
            if (!decimal.TryParse(Console.ReadLine(), out decimal amount) || amount <= 0)
            {
                Console.WriteLine("Invalid amount.");
                return;
            }

            try
            {
                var tx = txService.CreateTransaction(sender, to, amount, symbol);
                chain.AddTransactionToMemPool(tx);
                await chain.MineBlock(sender.Address);
                Console.WriteLine($"Sent {amount} {symbol}!");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: {ex.Message}");
            }
        }

        static async Task CreateToken(BlockChainService chain, TransactionService txService, Wallet sender)
        {
            Console.Write("Token symbol: ");
            var symbol = Console.ReadLine()?.ToUpperInvariant();
            if (string.IsNullOrEmpty(symbol)) return;

            Console.Write("Total supply: ");
            if (!decimal.TryParse(Console.ReadLine(), out decimal supply) || supply <= 0) return;

            try
            {
                var icoTx = txService.CreateICOToken(sender, symbol, supply);
                chain.AddTransactionToMemPool(icoTx);
                await chain.MineBlock(sender.Address);
                Console.WriteLine($"Token {symbol} created! Supply: {supply}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: {ex.Message}");
            }
        }
    }
}