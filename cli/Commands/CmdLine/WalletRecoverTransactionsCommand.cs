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
using Kurukuru;
using Cli.Commands.Common;
using BAMWallet.HD;
using McMaster.Extensions.CommandLineUtils;
using Microsoft.Extensions.Logging;

namespace Cli.Commands.CmdLine
{
    [CommandDescriptor("recover", "Recover wallet transactions")]
    public class WalletRecoverTransactionsCommand : Command
    {
        private readonly ILogger<WalletRecoverTransactionsCommand> _logger;
        private readonly ICommandReceiver _commandReceiver;

        public WalletRecoverTransactionsCommand(ILogger<WalletRecoverTransactionsCommand> logger, ICommandReceiver commandReceiver)
            : base(typeof(WalletRecoverTransactionsCommand), serviceProvider, true)
        {
            _logger = logger;
            _commandReceiver = commandReceiver;
        }

        public override async Task Execute(Session activeSession = null)
        {
            if (activeSession == null) return;
            var yesno = await Prompt.GetYesNoAsync("Restoring your wallet is an expensive operation that requires downloading large amounts of data.\n" +
                                        "Please be specific when entering the block height where you know when the first transaction was received.\n" +
                                        "If you don't know the specific block height then please ask for instructions in general: https://discord.gg/6DT3yFhXCB \n" +
                                        "Backup your wallet before starting or if restoring to a clean wallet then no backup is required.\n" +
                                        "To continue, enter (y) or (n) to exit.", false, ConsoleColor.Yellow);
            if (yesno)
            {
                var start = 0;

                var ynRecoverCompletely = await Prompt.GetYesNoAsync("All existing transactions will be dropped. Recover all (y) continue (n)", false, ConsoleColor.Red);
                start = ynRecoverCompletely ? 0 : await Prompt.GetIntAsync("Recover from specific blockchain height or leave it blank to recover from your last transaction height:", 0, ConsoleColor.Magenta);

                await Spinner.StartAsync("Recovering transactions ...", async spinner =>
                {
                    var (recovered, message) = await _commandReceiver.RecoverTransactions(activeSession, start, ynRecoverCompletely);
                    if (recovered is null)
                    {
                        _logger.LogError(message);
                        spinner.Fail(message);
                    }
                }, Patterns.Pong);
            }

        }
    }
}
