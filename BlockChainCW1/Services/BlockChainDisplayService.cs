using SecureBlockChainApp.Models;
using System;
using System.Collections.Generic;

namespace SecureBlockChainApp.Services
{
    public class BlockChainDisplayService
    {
        public void PrintBlockChain(List<Block> chain)
        {
            foreach (var block in chain)
            {
                Console.WriteLine($"Index: {block.Index}");
                Console.WriteLine($"TimeStamp: {block.TimeStamp}");
                Console.WriteLine($"Previous Hash: {block.PreviousHash}");
                Console.WriteLine($"Hash: {block.Hash}");
                Console.WriteLine($"Nonce: {block.Nonce}");
                Console.WriteLine($"Mining duration: {block.MiningDuration:F2} sec");
                Console.WriteLine("Transactions:");
                foreach (var tx in block.Transactions)
                {
                    PrintTransactions(tx);
                }
                Console.WriteLine(new string('-', 50));
            }
        }

        public void PrintTransactions(Transaction transaction)
        {
            Console.WriteLine($"  Id: {transaction.Id}");
            Console.WriteLine($"  From: {transaction.From}");
            Console.WriteLine($"  To: {transaction.To}");
            Console.WriteLine($"  Amount: {transaction.Amount}");
            Console.WriteLine($"  TimeStamp: {transaction.TimeStamp}");
            Console.WriteLine("  " + new string('.', 30));
        }

        public void PrintValidationResult(bool isValid)
        {
            var oldColor = Console.ForegroundColor;
            if (isValid)
            {
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine("The blockchain is valid");
            }
            else
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("The blockchain is invalid");
            }
            Console.ForegroundColor = oldColor;
        }
    }
}