// Bamboo (c) by Tangram
//
// Bamboo is licensed under a
// Creative Commons Attribution-NonCommercial-NoDerivatives 4.0 International License.
//
// You should have received a copy of the license along with this
// work. If not, see <http://creativecommons.org/licenses/by-nc-nd/4.0/>.
// Improved by ChatGPT

using System;
using System.Threading;
using BAMWallet.HD;

namespace Cli.Commands.Rpc
{
    public abstract class RpcCommandBase : Command
    {
        private readonly CancellationTokenSource _cancellationTokenSource;
        private readonly Session _session;

        public RpcCommandBase(IServiceProvider serviceProvider, Session session)
            : base(typeof(RpcCommandBase), serviceProvider)
        {
            _cancellationTokenSource = new CancellationTokenSource();
            _session = session;
            Result = new Tuple<object, string>(null, "Command not executed.");
        }

        public Tuple<object, string> Result { get; protected set; }

        public void Cancel()
        {
            _cancellationTokenSource.Cancel();
        }

        public Session Session
        {
            get { return _session; }
            set { /* Add any necessary validation here */ _session = value; }
        }

        public CancellationToken CancellationToken
        {
            get { return _cancellationTokenSource.Token; }
        }
    }
}
