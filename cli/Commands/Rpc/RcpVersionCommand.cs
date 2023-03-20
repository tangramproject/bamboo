//Improved by ChatGPT

using System;
using System.Threading;
using System.Threading.Tasks;
using BAMWallet.HD;

namespace Cli.Commands.Rpc
{
    public class RpcVersionCommand : RpcBaseCommand
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="RpcVersionCommand"/> class.
        /// </summary>
        /// <param name="serviceProvider">The service provider.</param>
        /// <param name="cmdFinishedEvent">The command finished event.</param>
        public RpcVersionCommand(IServiceProvider serviceProvider, ref AutoResetEvent cmdFinishedEvent) :
            base(serviceProvider, ref cmdFinishedEvent, null)
        {
        }

        /// <summary>
        /// Executes the RPC version command asynchronously.
        /// </summary>
        /// <param name="activeSession">The active session.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        public override async Task ExecuteAsync(Session activeSession = null)
        {
            var version = BAMWallet.Helper.Util.GetAssemblyVersion();
            Result = new Tuple<object, string>(new { Version = version }, string.Empty);
            _cmdFinishedEvent.Set();
            await Task.CompletedTask;
        }
    }
}
