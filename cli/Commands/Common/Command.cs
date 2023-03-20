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
using Microsoft.Extensions.DependencyInjection;
using BAMWallet.Extensions;
using BAMWallet.HD;
using McMaster.Extensions.CommandLineUtils;

namespace Cli.Commands.Common
{
    public abstract class Command
    {
        protected readonly ICommandReceiver CommandReceiver;
        protected readonly ICommandService Receiver;
        protected readonly IConsole Console;

        protected Command(Type commandType, IServiceProvider serviceProvider, bool refreshLogin = false)
        {
            Name = commandType.GetAttributeValue((CommandDescriptorAttribute attr) => attr.Name);
            Description = commandType.GetAttributeValue((CommandDescriptorAttribute attr) => attr.Description);
            CommandReceiver = serviceProvider.GetService<ICommandReceiver>();
            Receiver = serviceProvider.GetService<ICommandService>();
            Console = serviceProvider.GetService<IConsole>();
            RefreshLogin = refreshLogin;
        }

        public abstract Task Execute(Session activeSession = null);

        public string Name { get; }
        public string Description { get; }

        public bool RefreshLogin { get; }
    }
}
