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
using Cli.Commands.Common;
using ConsoleTables;
using McMaster.Extensions.CommandLineUtils;

namespace Cli.Commands.CmdLine
{
    [CommandDescriptor("address", "Find out your address")]
    class WalletAddressCommand : Command
    {
        public WalletAddressCommand(IServiceProvider serviceProvider)
            : base(typeof(WalletAddressCommand), serviceProvider, true)
        {
        }

        public override async Task Execute(Session activeSession = null)
        {
            if (activeSession != null)
            {
                var request = await _commandReceiver.Address(activeSession);

                if (request.Item1?.ToString() is null)
                {
                    _console.ForegroundColor = ConsoleColor.Red;
                    _console.WriteLine($"Address request failed: {request.Item2}");
                    _console.ForegroundColor = ConsoleColor.White;
                    return;
                }

                var table = new ConsoleTable("Address");
                table.AddRow(request.Item1);
                _console.WriteLine($"\n{table}");
            }
        }
    }
}
