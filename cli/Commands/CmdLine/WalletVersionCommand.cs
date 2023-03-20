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
using McMaster.Extensions.CommandLineUtils;

namespace Cli.Commands.CmdLine
{
    [CommandDescriptor("version", "Running version")]
    public class WalletVersionCommand : Command
    {
        private readonly IConsole _console;

        public WalletVersionCommand(IServiceProvider serviceProvider, IConsole console)
            : base(typeof(WalletVersionCommand), serviceProvider)
        {
            _console = console;
        }

        public override Task Execute(Session activeSession = null)
        {
            try
            {
                _console.WriteLine($"{BAMWallet.Helper.Util.GetAssemblyVersion()}");
                return Task.CompletedTask;
            }
            catch (Exception ex)
            {
                _console.Error.WriteLine($"An error occurred: {ex.Message}");
                return Task.FromException(ex);
            }
        }
    }
}
