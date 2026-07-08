using SecureBlockChainApp.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;

namespace SecureBlockChainApp.Services
{
    public class WalletService
    {
        private readonly List<Block> _chain;
        private readonly List<Wallet> _wallets;
        private readonly FileStorageService _fileStorageService;

        public WalletService(List<Block> chain)
        {
            _chain = chain;
            _fileStorageService = new FileStorageService();
            _wallets = _fileStorageService.LoadWallets() ?? new List<Wallet>();
        }

        public Wallet CreateWallet(string name)
        {
            var existing = _wallets.FirstOrDefault(w => w.Name.Equals(name, StringComparison.OrdinalIgnoreCase));
            if (existing != null) return existing;

            using var ecdsa = ECDsa.Create();
            byte[] privateKey = ecdsa.ExportECPrivateKey();
            byte[] publicKey = ecdsa.ExportSubjectPublicKeyInfo();
            string address = Convert.ToHexString(SHA256.HashData(publicKey)).Substring(0, 20);

            var wallet = new Wallet(name, address, publicKey, privateKey);
            _wallets.Add(wallet);
            _fileStorageService.SaveWallets(_wallets);
            return wallet;
        }

        public decimal GetBalance(string address)
        {
            decimal balance = 0;
            foreach (var block in _chain)
            {
                foreach (var tx in block.Transactions)
                {
                    if (tx.From == address) balance -= (tx.Amount + tx.Fee);
                    if (tx.To == address) balance += tx.Amount;
                }
            }
            return balance;
        }

        public bool VerifySignature(string address, byte[] data, byte[] signature)
        {
            if (signature == null || signature.Length == 0) return false;

            byte[] publicKey = null;
            var wallet = _wallets.FirstOrDefault(w => w.Address == address);
            if (wallet != null)
            {
                publicKey = wallet.PublicKey;
            }
            else
            {
                foreach (var block in _chain)
                {
                    var tx = block.Transactions.FirstOrDefault(t => t.From == address && t.SenderPublicKey != null);
                    if (tx != null)
                    {
                        publicKey = tx.SenderPublicKey;
                        break;
                    }
                }
            }

            if (publicKey == null) return false;

            try
            {
                using var ecdsa = ECDsa.Create();
                ecdsa.ImportSubjectPublicKeyInfo(publicKey, out _);
                return ecdsa.VerifyData(data, signature, HashAlgorithmName.SHA256);
            }
            catch
            {
                return false;
            }
        }
    }
}