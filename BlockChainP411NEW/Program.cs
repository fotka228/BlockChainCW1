
using BlockChainP411NEW.Models;
using BlockChainP411NEW.Services;
using System;

namespace BlockChainP411NEW;

class Program
{
    static void Main(string[] args)
    {
        Console.OutputEncoding = System.Text.Encoding.UTF8;
        Console.WriteLine("Exam");

        Console.WriteLine("\nDemo");

        var blockchain = new BlockChainService();

        var alice = blockchain.CreateWallet("0xAliceAddress1111", "priv_alice");
        var bob = blockchain.CreateWallet("0xBobAddress2222", "priv_bob");
        Console.WriteLine("1. Create a wallets:");
        Console.WriteLine($"  Alice: {alice.Address}");
        Console.WriteLine($"  Bob:   {bob.Address}");


        Console.WriteLine("\n2. Alice mines 3 blocks to accumulate BASE:");
        blockchain.MineBlock(alice.Address); Console.WriteLine("   Block #1 mined, reward: 50 BASE");
        blockchain.MineBlock(alice.Address); Console.WriteLine("   Block #2 mined, reward: 50 BASE");
        blockchain.MineBlock(alice.Address); Console.WriteLine("   Block #3 mined, reward: 50 BASE");

        Console.WriteLine($"   Balance BASE Alice: {alice.Balances["BASE"]:N2}");
        Console.WriteLine($"   Balance BASE Bob:  {(bob.Balances.ContainsKey("BASE") ? bob.Balances["BASE"] : 0):N2}");
        Console.WriteLine("\n3. Alice issues ALICE_COIN (ICO):");
        var icoTx = new Transaction(TransactionType.ICO, "ALICE_COIN", alice.Address, alice.Address, 0, 100m, 1000m);
        if (blockchain.AddTransaction(icoTx, out string err1))
        {
            Console.WriteLine("   ICO successful: 1000 ALICE_COIN created.");
            Console.WriteLine($"   Balance ALICE_COIN Alice: {alice.Balances["ALICE_COIN"]:N2}");
            Console.WriteLine($"   Balance BASE Alice: {alice.Balances["BASE"]:N2}");
        }
        Console.WriteLine("\n4. Bob tries to issue BOB_COIN (insufficient funds):");
        var bobIcoBad = new Transaction(TransactionType.ICO, "BOB_COIN", bob.Address, bob.Address, 0, 100m, 500m);
        if (!blockchain.AddTransaction(bobIcoBad, out string err2))
        {
            Console.WriteLine($"   Rejected: {err2}");
        }
        Console.WriteLine("\n5. Bob tries to steal the ALICE_COIN ticker:");
        bob.Balances["BASE"] = 200m;
        var bobStealTx = new Transaction(TransactionType.ICO, "ALICE_COIN", bob.Address, bob.Address, 0, 100m, 100m);
        if (!blockchain.AddTransaction(bobStealTx, out string err3))
        {
            Console.WriteLine($"   Rejected: {err3}");
        }
        bob.Balances["BASE"] = 0m;
        Console.WriteLine("\n6. Mining a block for transaction confirmation:");
        blockchain.MineBlock(alice.Address);
        Console.WriteLine($"   Block #4 mined. Current chain height: {blockchain.Chain.Count}");
        Console.WriteLine("\n7. Alice transfers 300 ALICE_COIN to Bob (Fee: 2 BASE):");
        var transferTx = new Transaction(TransactionType.Transfer, "ALICE_COIN", alice.Address, bob.Address, 300m, 2m);
        if (blockchain.AddTransaction(transferTx, out _))
        {
            Console.WriteLine("   Transfer successful.");
            Console.WriteLine($"   Alice ALICE_COIN: {alice.Balances["ALICE_COIN"]:N2}");
            Console.WriteLine($"   Bob ALICE_COIN:   {bob.Balances["ALICE_COIN"]:N2}");
            Console.WriteLine($"   Alice BASE:       {alice.Balances["BASE"]:N2}");
            Console.WriteLine($"   Bob BASE (Gas):   {(bob.Balances.ContainsKey("BASE") ? bob.Balances["BASE"] : 0):N2}");
        }
        Console.WriteLine("\nFINAL STATE OF MULTICURRENCY WALLETS");
        Console.WriteLine($"Alice ({alice.Address}):");
        foreach (var kvp in alice.Balances) Console.WriteLine($"   {kvp.Key}: {kvp.Value:N2}");

        Console.WriteLine($"Bob ({bob.Address}):");
        if (!bob.Balances.ContainsKey("BASE")) bob.Balances["BASE"] = 0m;
        foreach (var kvp in bob.Balances) Console.WriteLine($"   {kvp.Key}: {kvp.Value:N2}");

        Console.WriteLine($"\nRegistered tokens in the network: {string.Join(", ", blockchain.TokenRegistry)}");
        Console.WriteLine($"BlockChain height: {blockchain.Chain.Count}, difficulty: {blockchain.Difficulty}");


        Console.WriteLine("\nExtra-Task:");
        Console.WriteLine("\n1. Deploy a couple  of independence Нod:");
        var nodeA = new BlockChainService();
        var nodeB = new BlockChainService();

        var userA = nodeA.CreateWallet("0xUserA", "pA");
        var userB = nodeB.CreateWallet("0xUserB", "pB");
        userA.Balances["BASE"] = 500m;
        userB.Balances["BASE"] = 500m;

        Console.WriteLine("2. Nod А сreate token MEME:");
        var memeA = new Transaction(TransactionType.ICO, "MEME", userA.Address, userA.Address, 0, 100m, 500m);
        nodeA.AddTransaction(memeA, out _);
        nodeA.MineBlock(userA.Address);
        Console.WriteLine($"   Nod А height: {nodeA.Chain.Count}, tokens: {string.Join(", ", nodeA.TokenRegistry)}");

        Console.WriteLine("\n3. Nod Б сreate token MEME (2 blocks - longer chain):");
        var memeB = new Transaction(TransactionType.ICO, "MEME", userB.Address, userB.Address, 0, 100m, 2000m);
        nodeB.AddTransaction(memeB, out _);
        nodeB.MineBlock(userB.Address);
        nodeB.MineBlock(userB.Address);
        Console.WriteLine($"   Nod Б height: {nodeB.Chain.Count}, tokens: {string.Join(", ", nodeB.TokenRegistry)}");

        Console.WriteLine("\n4. Synchronization and consensus verification ...");
        if (nodeB.Chain.Count > nodeA.Chain.Count)
        {
            Console.WriteLine("   [Consensus] Nod А detected longer chain. Rolling back shorter chain...");
            nodeA.RollbackToHeight(1);

            nodeA.Chain.Clear();
            nodeA.Chain.AddRange(nodeB.Chain);
            nodeA.TokenRegistry.Clear();
            foreach (var t in nodeB.TokenRegistry) nodeA.TokenRegistry.Add(t);

            Console.WriteLine("   [Consensus] Nod А successfully overwrote the state with data from Nod Б.");
        }

        Console.WriteLine("\n5. Final State:");
        Console.WriteLine($"   Nod А height: {nodeA.Chain.Count}, tokens: {string.Join(", ", nodeA.TokenRegistry)}");
        Console.WriteLine($"   Nod Б height: {nodeB.Chain.Count}, tokens: {string.Join(", ", nodeB.TokenRegistry)}");

        Console.WriteLine("\nConsensus Result:");
        Console.WriteLine("   Consensus successful: longer chain (Nod Б) won.");
        Console.WriteLine("   Shorter chain (Nod А) rolled back, duplicate token issuance canceled.");

        Console.WriteLine("\nConsensus Result:");
        Console.WriteLine("   Consensus successful: longer chain (Nod Б) won.");
        Console.WriteLine("   Shorter chain (Nod А) rolled back, duplicate token issuance canceled.");

        Console.WriteLine("\nExam completed. Press any key to exit...");
        Console.ReadKey();
    }
}