// CypherNetwork BAMWallet by Matthew Hellyer is licensed under CC BY-NC-ND 4.0.
// To view a copy of this license, visit https://creativecommons.org/licenses/by-nc-nd/4.0

using System;
using System.IO;
using System.Security;
using BAMWallet.Extensions;
using BAMWallet.Helper;
using BAMWallet.Model;
using LiteDB;

namespace BAMWallet.HD
{
    public class Session
    {
        public SecureString Identifier { get; }
        public SecureString Passphrase { get; }
        public Guid SessionId { get; }
        public SessionType SessionType { get; }
        public LiteRepository Database { get; }
        public bool Syncing { get; }

        public KeySet KeySet => Database.Query<KeySet>().First();

        public bool IsValid => IsIdentifierValid(Identifier);

        public static bool AreCredentialsValid(SecureString identifier, SecureString passphrase)
        {
            return IsIdentifierValid(identifier) && IsPassPhraseValid(identifier, passphrase);
        }

        public Session(SecureString identifier, SecureString passphrase)
        {
            Identifier = identifier;
            Passphrase = passphrase;
            SessionId = Guid.NewGuid();
            if (!IsValid)
            {
                throw new FileNotFoundException($"Wallet with ID: {identifier.FromSecureString()} not found!");
            }
            Database = Util.LiteRepositoryFactory(identifier.FromSecureString(), passphrase);
        }

        private static bool IsIdentifierValid(SecureString identifier)
        {
            return File.Exists(Util.WalletPath(identifier.FromSecureString()));
        }

        private static async Task<bool> IsPassPhraseValidAsync(SecureString walletIdentifier, SecureString walletPassphrase)
        {
            var connectionString = new ConnectionString
            {
                Filename = Util.WalletPath(walletIdentifier.FromSecureString()),
                Password = walletPassphrase.FromSecureString(),
                Connection = ConnectionType.Shared
            };

            try
            {
                using var db = new LiteDatabase(connectionString);
                var collection = db.GetCollection<KeySet>();
                return await collection.CountAsync() == 1;
            }
            catch (LiteException)
            {
                return false;
            }
        }

        public static async Task<bool> AreCredentialsValidAsync(SecureString walletIdentifier, SecureString walletPassphrase)
        {
            return await IsIdentifierValidAsync(walletIdentifier) && await IsPassPhraseValidAsync(walletIdentifier, walletPassphrase);
        }

        private static async Task<bool> IsIdentifierValidAsync(SecureString identifier)
        {
            return await File.ExistsAsync(Util.WalletPath(identifier.FromSecureString()));
        }
    }
