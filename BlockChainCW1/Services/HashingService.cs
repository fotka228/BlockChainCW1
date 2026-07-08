using SecureBlockChainApp.Models;
using System;
using System.Security.Cryptography;
using System.Text;

namespace SecureBlockChainApp.Services
{
    public class HashingService
    {
        public string ComputeHash(Block block)
        {
            var totalHash = "";
            foreach (var item in block.Transactions)
            {
                totalHash += ComputeHash(item.ToRowString());
            }
            var blockData = $"{block.Index}{block.TimeStamp:O}{totalHash}{block.PreviousHash}{block.Nonce}";
            return ComputeHash(blockData);
        }

        public string ComputeHash(string input)
        {
            byte[] inputBytes = Encoding.UTF8.GetBytes(input);
            byte[] hashBytes = SHA256.HashData(inputBytes);
            return Convert.ToHexString(hashBytes);
        }
    }
}