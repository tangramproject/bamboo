// Bamboo (c) by Tangram
//
// Bamboo is licensed under a
// Creative Commons Attribution-NonCommercial-NoDerivatives 4.0 International License.
//
// You should have received a copy of the license along with this
// work. If not, see <http://creativecommons.org/licenses/by-nc-nd/4.0/>.
// Improved by ChatGPT

using System;
using System.IO;
using System.Linq;
using System.Security;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using McMaster.Extensions.CommandLineUtils;
using Constants = BAMWallet.HD.Constant;
using BAMWallet.HD;
using Cli.Commands.Common;
using Cli.Helper;

namespace Cli.Commands.CmdLine
{
    [CommandDescriptor("remove", "Remove a wallet")]
    class WalletRemoveCommand : Command
    {
        private string _idToDelete;
        private readonly ILogger _logger;

        private static bool IsLoggedInWithWallet(SecureString identifier, Session activeSession)
        {
            return activeSession != null &&
                   string.Equals(identifier.FromSecureString(), activeSession.Identifier.FromSecureString());
        }

        private void DeleteWallet()
        {
            if (string.IsNullOrWhiteSpace(_idToDelete))
            {
                _console.WriteLine("Wallet name cannot be empty.");
                return;
            }

            var baseDir = Path.GetDirectoryName(AppDomain.CurrentDomain.BaseDirectory);
            try
            {
                if (Directory.Exists(baseDir))
                {
                    var walletsDir = Path.Combine(baseDir, Constants.WALLET_DIR_SUFFIX);
                    if (Directory.Exists(walletsDir))
                    {
                        using var files = new DisposableEnumerable<string>(Directory.EnumerateFiles(walletsDir, Constants.WALLET_FILE_EXTENSION));

                        if (files.Any())
                        {
                            if (files.TryGetValue(Path.GetFileNameWithoutExtension(_idToDelete), out var walletFile))
                            {
                                File.Delete(walletFile);

                                _console.ForegroundColor = ConsoleColor.Green;
                                _console.WriteLine($"Wallet with name: {_idToDelete} permanently deleted.");
                                _console.ForegroundColor = ConsoleColor.White;
                            }
                            else
                            {
                                _console.ForegroundColor = ConsoleColor.Red;
                                _console.WriteLine($"Wallet with name: {_idToDelete} does not exist. Command failed.");
                                _console.ForegroundColor = ConsoleColor.White;
                            }
                        }
                        else
                        {
                            _console.ForegroundColor = ConsoleColor.Red;
                            _console.WriteLine($"Wallet with name: {_idToDelete} does not exist. Command failed.");
                            _console.ForegroundColor = ConsoleColor.White;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.LogException(_console, _logger, ex);
            }

            _idToDelete = string.Empty;
        }

        public WalletRemoveCommand(IServiceProvider serviceProvider, ILogger logger)
            : base(typeof(WalletRemoveCommand), serviceProvider)
        {
            _logger = logger;
            Logout = false;
            _idToDelete = string.Empty;
        }

        public override async Task Execute(Session activeSession = null)
        {
            var identifier = await Prompt.GetStringAsync("Wallet Name:", null, ConsoleColor.Yellow).ConfigureAwait(false);
            var isDeletionConfirmed = Prompt.GetYesNo(
                $"Are you sure you want to delete wallet with Identifier: {identifier}? (This action cannot be undone!)", false, ConsoleColor.Red);
            if (!isDeletionConfirmed) return;

            _idToDelete = identifier;

            if (IsLoggedInWithWallet(identifier.ToSecureString(), activeSession))
            {
                Logout = true;
            }

            DeleteWallet();
        }

        public bool Logout { get; private set; }
    }
}
