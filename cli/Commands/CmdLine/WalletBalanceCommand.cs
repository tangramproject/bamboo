// Bamboo (c) by Tangram
//
// Bamboo is licensed under a
// Creative Commons Attribution-NonCommercial-NoDerivatives 4.0 International License.
//
// You should have received a copy of the license along with this
// work. If not, see <http://creativecommons.org/licenses/by-nc-nd/4.0/>.
//Improved by ChatGPT

using System;
using System.Threading.Tasks;
using BAMWallet.HD;
using Kurukuru;
using McMaster.Extensions.CommandLineUtils;
using Cli.Commands.Common;

namespace Cli.Commands.CmdLine
{
    [CommandDescriptor("balance", "Get your wallet balance")]
    public class WalletBalanceCommand : Command
    {
        private readonly ICommandReceiver _commandReceiver;
        private readonly IConsole _console;

        public WalletBalanceCommand(IServiceProvider serviceProvider, ICommandReceiver commandReceiver, IConsole console)
            : base(typeof(WalletBalanceCommand), serviceProvider, true)
        {
            _commandReceiver = commandReceiver ?? throw new ArgumentNullException(nameof(commandReceiver));
            _console = console ?? throw new ArgumentNullException(nameof(console));
        }

        public override async Task Execute(Session activeSession = null)
        {
            if (activeSession == null)
            {
                return;
            }

            await Spinner.StartAsync("Checking confirmed balance(s) ...", async spinner =>
            {
                var balanceProfile = _commandReceiver.GetBalanceProfile(activeSession);
                if (balanceProfile == null)
                {
                    spinner.Fail("Nothing to see.");
                    return;
                }

                var table = new ConsoleTable("Payments", "Coinstake", "Coinbase", "Change", "Balance");
                table.AddRow($"{balanceProfile.Payment:F9}", $"{balanceProfile.Coinstake:F9}",
                    $"{balanceProfile.Coinbase:F9}", $"{balanceProfile.Change:F9}",
                    $"{balanceProfile.Balance:F9}");
                table.Configure(o => o.NumberAlignment = Alignment.Right);

                _console.WriteLine($"\n{table}");
                _console.WriteLine("\n");

                await Task.Delay(100);
            });
        }
    }
}
