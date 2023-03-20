// Bamboo (c) by Tangram
//
// Bamboo is licensed under a
// Creative Commons Attribution-NonCommercial-NoDerivatives 4.0 International License.
//
// You should have received a copy of the license along with this
// work. If not, see <http://creativecommons.org/licenses/by-nc-nd/4.0/>.
// Improved by ChatGPT

using System;
using System.Linq;
using System.Threading.Tasks;
using BAMWallet.Extensions;
using BAMWallet.HD;
using BAMWallet.Model;
using Cli.Commands.Common;
using Kurukuru;
using McMaster.Extensions.CommandLineUtils;
using Microsoft.Extensions.Logging;

namespace Cli.Commands.CmdLine
{
    [CommandDescriptor("login", "Provide your wallet name and passphrase")]
    class LoginCommand : Command
    {
        private readonly ILogger<LoginCommand> _logger;
        private Session _session;

        public LoginCommand(IServiceProvider serviceProvider, ILogger<LoginCommand> logger)
            : base(typeof(LoginCommand), serviceProvider, true)
        {
            _logger = logger;
        }

        public override async Task Execute(Session activeSession = null)
        {
            var identifier = Prompt.GetString("Wallet Name:", null, ConsoleColor.Yellow);
            var passphrase = Prompt.GetPasswordAsSecureString("Passphrase:", ConsoleColor.Yellow);

            if (await Session.AreCredentialsValidAsync(identifier.ToSecureString(), passphrase))
            {
                ActiveSession = new Session(identifier.ToSecureString(), passphrase);

                var networkSettings = BAMWallet.Helper.Util.LiteRepositoryAppSettingsFactory().Query<NetworkSettings>().FirstOrDefault();

                if (networkSettings is not null)
                {
                    var networkEnvironment = ActiveSession.KeySet.StealthAddress.StartsWith('v') ? Constant.Mainnet : Constant.Testnet;

                    if (networkSettings.Environment != networkEnvironment)
                    {
                        _logger.LogError("Please create a separate wallet for {Environment}. Or change the network environment back to {Environment}.", networkEnvironment);
                        Environment.Exit(0);
                        return;
                    }
                }

                await Spinner.StartAsync("Syncing wallet ...", async spinner =>
                {
                    await _commandReceiver.SyncWallet(ActiveSession);
                });

                await Spinner.StartAsync("Scanning for new transactions ...", async spinner =>
                {
                    await _commandReceiver.RecoverTransactionsAsync(ActiveSession, 0);
                });
            }
            else
            {
                _logger.LogError("Access denied. Cannot find a wallet with the given identifier and passphrase.");
            }
        }

        public Session ActiveSession
        {
            get => _session;
            private set => _session = value;
        }
    }
}
