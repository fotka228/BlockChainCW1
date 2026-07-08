using SecureBlockChainApp.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;

namespace SecureBlockChainApp.Services
{
    public class FileStorageService
    {
        private readonly string _BlockChainFilePath = "blockchain.json";
        private readonly string _WalletsFilePath = "wallets.json";
        private readonly string _BackupFilePath = "blockchain_backup.json";
        private readonly string _CorruptedFilePath = "blockchain_corrupted.json";

        private readonly JsonSerializerOptions _jsonOptions = new JsonSerializerOptions
        {
            WriteIndented = true,
        };

        public void SaveBlockChain(List<Block> chain)
        {
            if (File.Exists(_BlockChainFilePath))
            {
                File.Copy(_BlockChainFilePath, _BackupFilePath, overwrite: true);
            }

            var json = JsonSerializer.Serialize(chain, _jsonOptions);
            File.WriteAllText(_BlockChainFilePath, json);
        }

        public List<Block> LoadBlockChain()
        {
            if (!File.Exists(_BlockChainFilePath))
            {
                return new List<Block>();
            }


            try
            {
                var json = File.ReadAllText(_BlockChainFilePath);
                return JsonSerializer.Deserialize<List<Block>>(json, _jsonOptions) ?? new List<Block>();
            }
            catch (Exception)
            {
                var oldColor = Console.ForegroundColor;
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("critical problem: blockchian file is corrupted!");
                Console.ForegroundColor = oldColor;


                try
                {
                    if (File.Exists(_CorruptedFilePath)) File.Delete(_CorruptedFilePath);
                    File.Move(_BlockChainFilePath, _CorruptedFilePath);
                }
                catch { }

                Console.WriteLine(" trying  to restore data from the backup...");
                if (File.Exists(_BackupFilePath))
                {
                    try
                    {
                        var backupJson = File.ReadAllText(_BackupFilePath);
                        var backupChain = JsonSerializer.Deserialize<List<Block>>(backupJson, _jsonOptions);
                        if (backupChain != null && backupChain.Count > 0)
                        {
                            Console.ForegroundColor = ConsoleColor.Green;
                            Console.WriteLine("data successfully restored from the backup!");
                            Console.ForegroundColor = oldColor;
                            return backupChain;
                        }
                    }
                    catch
                    {
                        Console.ForegroundColor = ConsoleColor.Red;
                        Console.WriteLine(" critical error: backup file is also corrupted!");
                        Console.ForegroundColor = oldColor;
                    }
                }
                else
                {
                    Console.WriteLine(" Backup file is missing.");
                }

                return null;
            }
        }

        public void SaveWallets(List<Wallet> wallets)
        {
            var json = JsonSerializer.Serialize(wallets, _jsonOptions);
            File.WriteAllText(_WalletsFilePath, json);
        }

        public List<Wallet> LoadWallets()
        {
            if (!File.Exists(_WalletsFilePath)) return new List<Wallet>();
            var json = File.ReadAllText(_WalletsFilePath);
            return JsonSerializer.Deserialize<List<Wallet>>(json, _jsonOptions) ?? new List<Wallet>();
        }
    }
}