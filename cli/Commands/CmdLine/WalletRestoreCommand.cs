// Bamboo (c) by Tangram
//
// Bamboo is licensed under a
// Creative Commons Attribution-NonCommercial-NoDerivatives 4.0 International License.
//
// You should have received a copy of the license along with this
// work. If not, see <http://creativecommons.org/licenses/by-nc-nd/4.0/>.
// Improved by ChatGPT

using System;
using System.Threading.Tasks;
using BAMWallet.HD;
using BAMWallet.Helper;
using Cli.Commands.Common;
using McMaster.Extensions.CommandLineUtils;
using Microsoft.Extensions.Logging;

namespace Cli.Commands.CmdLine
{
    [CommandDescriptor("restore", "Restore wallet from seed and passphrase")]
    class WalletRestoreCommand : Command
    {
        private readonly ICommandReceiver _commandReceiver;
        private readonly ILogger<WalletRestoreCommand> _logger;

        public WalletRestoreCommand(ICommandReceiver commandReceiver, ILogger<WalletRestoreCommand> logger)
            : base(typeof(WalletRestoreCommand), null)
        {
            _commandReceiver = commandReceiver;
            _logger = logger;
        }

        public override async Task Execute(Session activeSession = null)
        {
            string walletName = Prompt.GetString("Specify new wallet name (e.g., MyWallet):", null, ConsoleColor.Red);
            string seedString = await Prompt.GetStringAsync("Seed:", null, ConsoleColor.Yellow);
            string passphraseString = await Prompt.GetStringAsync("Passphrase/pin:", null, ConsoleColor.Yellow);

            using SecureString seed = seedString.ToSecureString();
            using SecureString passphrase = passphraseString.ToSecureString();

            try
            {
                string walletId = await _commandReceiver.CreateWallet(seed, passphrase, walletName);
                string walletPath = Util.WalletPath(walletId);

                _logger.LogInformation("Your wallet has been generated!");
                _logger.LogInformation("To start synchronizing with the network, login with your wallet name and passphrase or PIN");
                _logger.LogInformation($"Wallet Name: {walletId}");
                _logger.LogInformation($"Wallet Path: {walletPath}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to create wallet");
            }
        }
    }
}

