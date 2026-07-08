using BlockChainP411NEW.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;

namespace BlockChainP411NEW.Services
{
    public class WalletService
    {
        private readonly List<Block> _chain;
        public WalletService(List<Block> chain)
        {
            _chain = chain;
        }
        public Wallet CreateWallet(string name)
        {
            using var ecdsa = ECDsa.Create();
            byte[] privateKey = ecdsa.ExportECPrivateKey();
            byte[] publicKey = ecdsa.ExportSubjectPublicKeyInfo();
            string address = DeriveAddress(publicKey);
            return new Wallet(name, address, publicKey, privateKey);
        }
        public static string DeriveAddress(byte[] publicKey)
        {
            if (publicKey == null || publicKey.Length == 0)
                throw new ArgumentException("Public key cannot be empty.", nameof(publicKey));

            byte[] hash = SHA256.HashData(publicKey);
            byte[] addressBytes = hash[^20..];
            return "0x" + Convert.ToHexString(addressBytes).ToLowerInvariant();
        }
        public bool VerifySignature(byte[] publicKey, byte[] data, byte[] signature)
        {
            if (publicKey == null || publicKey.Length == 0 || signature == null || signature.Length == 0)
                return false;

            try
            {
                using var ecdsa = ECDsa.Create();
                ecdsa.ImportSubjectPublicKeyInfo(publicKey, out _);
                return ecdsa.VerifyData(data, signature, HashAlgorithmName.SHA256);
            }
            catch (CryptographicException)
            {
                return false;
            }
        }
        public decimal GetBalance(string address, string tokenSymbol = "BASE")
        {
            decimal balance = 0;
            foreach (var block in _chain)
            {
                foreach (var tx in block.Transactions)
                {
                    if (tx.TokenSymbol != tokenSymbol) continue;

                    if (tx.From == address && tx.Type != TransactionType.ICO)
                        balance -= tx.Amount + (tokenSymbol == "BASE" ? tx.Fee : 0);
                    if (tx.To == address)
                        balance += tx.Amount;
                }
            }
            return balance;
        }
        public Dictionary<string, decimal> GetAllBalances(string address)
        {
            var balances = new Dictionary<string, decimal>();
            var allTokens = new HashSet<string> { "BASE" };

            foreach (var block in _chain)
            {
                foreach (var tx in block.Transactions)
                {
                    if (!string.IsNullOrEmpty(tx.TokenSymbol))
                        allTokens.Add(tx.TokenSymbol);
                }
            }

            foreach (var token in allTokens)
            {
                balances[token] = GetBalance(address, token);
            }

            return balances;
        }
        public bool TokenExists(string tokenSymbol)
        {
            if (tokenSymbol == "BASE") return true;

            foreach (var block in _chain)
            {
                foreach (var tx in block.Transactions)
                {
                    if (tx.Type == TransactionType.ICO &&
                        tx.TokenSymbol.Equals(tokenSymbol, StringComparison.OrdinalIgnoreCase))
                        return true;
                }
            }
            return false;
        }
        public (string Creator, decimal TotalSupply, DateTime CreatedAt)? GetTokenInfo(string tokenSymbol)
        {
            if (tokenSymbol == "BASE") return null;

            foreach (var block in _chain)
            {
                foreach (var tx in block.Transactions)
                {
                    if (tx.Type == TransactionType.ICO &&
                        tx.TokenSymbol.Equals(tokenSymbol, StringComparison.OrdinalIgnoreCase))
                    {
                        return (tx.From, tx.Amount, tx.TimeStamp);
                    }
                }
            }
            return null;
        }
    }
}