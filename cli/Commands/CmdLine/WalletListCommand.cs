// Bamboo (c) by Tangram
//
// Bamboo is licensed under a
// Creative Commons Attribution-NonCommercial-NoDerivatives 4.0 International License.
//
// You should have received a copy of the license along with this
// work. If not, see <http://creativecommons.org/licenses/by-nc-nd/4.0/>.
// Improved by ChatGPT

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using ConsoleTables;
using McMaster.Extensions.CommandLineUtils;
using BAMWallet.HD;
using Cli.Commands.Common;

namespace Cli.Commands.CmdLine
{
    [CommandDescriptor("list", "Available wallets")]
    class WalletListCommand : Command
    {
        public WalletListCommand(IServiceProvider serviceProvider)
            : base(typeof(WalletListCommand), serviceProvider, true)
        {
        }

        public override async Task Execute(Session activeSession = null)
        {
            var (wallets, message) = await _commandReceiver.WalletList();

            if (wallets is not null)
            {
                using var table = new ConsoleTable("Path");
                foreach (var balance in wallets as List<string>)
                {
                    table.AddRow(balance);
                }

                _console.WriteLine($"\n{table}");
            }
            else
            {
                _console.ForegroundColor = ConsoleColor.Red;
                _console.WriteLine($"Wallet list request failed: {message}!");
                _console.ForegroundColor = ConsoleColor.White;
            }
        }
    }
}
