using BlockChainP411NEW.Models;
using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace BlockChainP411NEW.Services
{
    public class TransactionService
    {
        private readonly WalletService _walletService;
        private readonly List<Block> _chain;
        private static readonly Regex AddressPattern = new Regex(@"^0x[0-9a-fA-F]{40}$", RegexOptions.Compiled);
        public TransactionService(List<Block> chain)
        {
            _chain = chain;
            _walletService = new WalletService(chain);
        }
        public Transaction CreateTransaction(Wallet sender, string to, decimal amount, string tokenSymbol = "BASE")
        {
            var balance = _walletService.GetBalance(sender.Address, tokenSymbol);
            if (balance < amount)
                throw new ArgumentException($"Insufficient balance of {tokenSymbol}.");

            if (tokenSymbol != "BASE" && !_walletService.TokenExists(tokenSymbol))
                throw new ArgumentException($"Token {tokenSymbol} does not exist.");

            var baseBalance = _walletService.GetBalance(sender.Address, "BASE");
            decimal fee = CalculateFee(amount);
            if (baseBalance < fee)
                throw new ArgumentException($"Insufficient BASE for fee. Need {fee}, have {baseBalance}.");

            var tx = new Transaction(sender.Address, to, amount, sender.PublicKey)
            {
                TokenSymbol = tokenSymbol,
                Fee = fee
            };
            tx.Signature = sender.Sign(tx.GetDataToSign());

            var (isValid, error) = ValidateTransaction(tx);
            if (!isValid)
                throw new ArgumentException(error);

            return tx;
        }

        public Transaction CreateICOToken(Wallet sender, string tokenSymbol, decimal totalSupply)
        {
            var baseBalance = _walletService.GetBalance(sender.Address, "BASE");
            if (baseBalance < 50)
                throw new ArgumentException($"ICO costs 50 BASE. You have only {baseBalance} BASE.");

            if (_walletService.TokenExists(tokenSymbol))
                throw new ArgumentException($"Token {tokenSymbol} already exists!");

            if (tokenSymbol.Equals("BASE", StringComparison.OrdinalIgnoreCase))
                throw new ArgumentException("Cannot create token with symbol BASE (reserved).");

            var tx = new Transaction(sender.Address, tokenSymbol, totalSupply, sender.PublicKey, true);
            tx.Signature = sender.Sign(tx.GetDataToSign());

            var (isValid, error) = ValidateTransaction(tx);
            if (!isValid)
                throw new ArgumentException(error);

            return tx;
        }


        private decimal CalculateFee(decimal amount)
        {
            decimal fee = amount * 0.01m;
            return Math.Max(fee, 0.01m);
        }
        public (bool IsValid, string ErrorMessage) ValidateTransaction(Transaction tx)
        {
            if (tx == null)
                return (false, "Transaction cannot be null.");
            if (string.IsNullOrEmpty(tx.From))
                return (false, "Sender cannot be empty.");
            if (string.IsNullOrEmpty(tx.To))
                return (false, "Recipient cannot be empty.");

            if (tx.From != "COINBASE" && !IsValidAddress(tx.From))
                return (false, $"Invalid sender address.");

            if (!IsValidAddress(tx.To))
                return (false, $"Invalid recipient address.");

            if (tx.Amount <= 0)
                return (false, "Amount must be greater than zero.");

            if (tx.Type == TransactionType.ICO)
            {
                var baseBalance = _walletService.GetBalance(tx.From, "BASE");
                if (baseBalance < 50)
                    return (false, $"ICO costs 50 BASE. Sender has only {baseBalance} BASE.");

                if (_walletService.TokenExists(tx.TokenSymbol))
                    return (false, $"Token {tx.TokenSymbol} already exists!");

                if (tx.TokenSymbol.Equals("BASE", StringComparison.OrdinalIgnoreCase))
                    return (false, "Cannot create token with symbol BASE (reserved).");

                if (tx.From != tx.To)
                    return (false, "ICO creator must receive the tokens.");
            }
            else
            {
                if (tx.TokenSymbol != "BASE" && !_walletService.TokenExists(tx.TokenSymbol))
                    return (false, $"Token {tx.TokenSymbol} does not exist.");

                var tokenBalance = _walletService.GetBalance(tx.From, tx.TokenSymbol);
                if (tokenBalance < tx.Amount)
                    return (false, $"Insufficient {tx.TokenSymbol} balance.");

                var baseBalance = _walletService.GetBalance(tx.From, "BASE");
                if (baseBalance < tx.Fee)
                    return (false, $"Insufficient BASE for fee. Need {tx.Fee}, have {baseBalance}.");
            }

            if (tx.From == "COINBASE")
                return (true, string.Empty);

            string derivedAddress;
            try
            {
                derivedAddress = WalletService.DeriveAddress(tx.SenderPublicKey);
            }
            catch (ArgumentException)
            {
                return (false, "Sender public key is missing or invalid.");
            }

            if (!string.Equals(derivedAddress, tx.From, StringComparison.OrdinalIgnoreCase))
                return (false, "Sender address does not match public key.");

            bool signatureValid = _walletService.VerifySignature(tx.SenderPublicKey, tx.GetDataToSign(), tx.Signature);
            if (!signatureValid)
                return (false, "Invalid transaction signature.");

            return (true, string.Empty);
        }
        public static bool IsValidAddress(string address)
        {
            return !string.IsNullOrEmpty(address) && AddressPattern.IsMatch(address);
        }
    }
}