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
using McMaster.Extensions.CommandLineUtils;
using NBitcoin;

namespace Cli.Commands.CmdLine
{
    [CommandDescriptor("seed", "Create new seed and passphrase")]
    class WalletCreateMnemonicCommand : Command
    {
        private readonly ICommandReceiver _commandReceiver;
        private readonly IConsole _console;

        public WalletCreateMnemonicCommand(ICommandReceiver commandReceiver, IConsole console)
        {
            _commandReceiver = commandReceiver;
            _console = console;
        }

        public override async Task Execute(Session activeSession = null)
        {
            _console.ForegroundColor = ConsoleColor.Magenta;
            _console.WriteLine("\nSeed phrase\n");

            var wCount = await Options(3);

            var mnemonic = new Mnemonic(Wordlist.English, wCount);
            var seed = mnemonic.DeriveSeed();
            var passphrase = await Options(1);

            _console.WriteLine("Seed phrase: " + mnemonic);
            _console.WriteLine("Passphrase:  " + passphrase);

            _console.ForegroundColor = ConsoleColor.White;
        }

        private async Task<WordCount> Options(int defaultAnswer)
        {
            _console.ForegroundColor = ConsoleColor.Magenta;

            _console.WriteLine("\nWord Count:\n");
            _console.WriteLine("12    [1]");
            _console.WriteLine("18    [2]");
            _console.WriteLine("24    [3]\n");

            var wordCount = await Prompt.GetIntAsync("Select word count:", defaultAnswer, ConsoleColor.Yellow);

            _console.WriteLine("");

            return (WordCount)wordCount;
        }
    }
}
