// Bamboo (c) by Tangram
//
// Bamboo is licensed under a
// Creative Commons Attribution-NonCommercial-NoDerivatives 4.0 International License.
//
// You should have received a copy of the license along with this
// work. If not, see <http://creativecommons.org/licenses/by-nc-nd/4.0/>.
// Imprved by ChatGPT

using System;
using System.Threading.Tasks;
using BAMWallet.HD;
using Cli.Commands.Common;
using Kurukuru;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Cli.Commands.CmdLine
{
    [CommandDescriptor("sync", "Syncs wallet with chain")]
    public class WalletSyncCommand : Command
    {
        private readonly ILogger<WalletSyncCommand> _logger;

        public WalletSyncCommand(IServiceProvider serviceProvider)
            : base(typeof(WalletSyncCommand), serviceProvider, true)
        {
            _logger = serviceProvider.GetService<ILogger<WalletSyncCommand>>();
        }

        public override Task Execute(Session activeSession = null)
        {
            if (activeSession == null)
            {
                return Task.CompletedTask;
            }

            return Spinner.StartAsync("Syncing wallet ...", async spinner =>
            {
                try
                {
                    _commandReceiver.SyncWallet(activeSession);
                    spinner.Succeed();
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error syncing wallet.");
                    throw;
                }
            }, Patterns.Arc);
        }
    }
}
