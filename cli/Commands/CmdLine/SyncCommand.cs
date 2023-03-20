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
using BAMWallet.Model;
using Cli.Commands.Common;
using Kurukuru;
using McMaster.Extensions.CommandLineUtils;
using Microsoft.Extensions.DependencyInjection;

namespace Cli.Commands.CmdLine
{
    [CommandDescriptor("sync", "Syncs wallet with chain")]
    public class SyncCommand : Command
    {
        public SyncCommand(IServiceProvider serviceProvider)
            : base(typeof(SyncCommand), serviceProvider, false)
        {
        }

        public override async Task Execute(Session activeSession = null)
        {
            if (activeSession != null)
            {
                await _commandReceiver.SyncWallet(activeSession);
            }

            
            await Spinner.StartAsync("Syncing wallet...", async spinner =>
            {
                await _commandReceiver.SyncWallet(activeSession);
                spinner.Succeed("Wallet synced successfully.");
            });
        }
    }
}
