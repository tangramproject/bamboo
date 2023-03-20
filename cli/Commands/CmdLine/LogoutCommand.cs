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

namespace Cli.Commands.CmdLine
{
    [CommandDescriptor("logout", "Session ends and locks the wallet")]
    class LogoutCommand : Command
    {
        private readonly bool _automaticLogout;

        public LogoutCommand(IServiceProvider serviceProvider, bool automaticLogout = false)
            : base(typeof(LogoutCommand), serviceProvider)
        {
            _automaticLogout = automaticLogout;
        }

        public override async Task Execute(Session activeSession = null)
        {
            if (!_automaticLogout) return;

            _console.ForegroundColor = ConsoleColor.Red;
            await _console.WriteLineAsync("You have been logged out of the wallet due to inactivity. Please login again to use the wallet.");
            _console.ForegroundColor = ConsoleColor.Cyan;
            await _console.WriteAsync("bamboo$ ");
            _console.ResetColor();
        }

        public bool AutomaticLogout => _automaticLogout;
    }
}

