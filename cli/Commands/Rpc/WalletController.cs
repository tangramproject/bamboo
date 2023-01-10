// Bamboo (c) by Tangram
//
// Bamboo is licensed under a
// Creative Commons Attribution-NonCommercial-NoDerivatives 4.0 International License.
//
// You should have received a copy of the license along with this
// work. If not, see <http://creativecommons.org/licenses/by-nc-nd/4.0/>.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Microsoft.AspNetCore.Mvc;
using BAMWallet.Extensions;
using BAMWallet.HD;
using BAMWallet.Model;
using Cli.Commands.Common;
using Cli.Commands.Rpc;
using CLi.Commands.Rpc;
using Dawn;
using MessagePack;
using NBitcoin;

namespace BAMWallet.Rpc.Controllers
{
    [ApiController]
    [Route("api/wallet")]
    [Produces("application/json")]
    public class WalletController
    {
        private readonly ICommandService _commandService;
        private readonly IServiceProvider _serviceProvider;

        public WalletController(ICommandService commandService, IServiceProvider serviceProvider)
        {
            _commandService = commandService;
            _serviceProvider = serviceProvider;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="credentials"></param>
        /// <returns></returns>
        private Session GetSessionFromCredentials(Credentials credentials)
        {
            Guard.Argument(credentials, nameof(credentials)).NotNull();
            var identifier = credentials.Username.ToSecureString();
            var pass = credentials.Passphrase.ToSecureString();
            return Session.AreCredentialsValid(identifier, pass) ? new Session(identifier, pass) : null;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="cmd"></param>
        private void SendCommandAndAwaitResponse(RpcBaseCommand cmd)
        {
            _commandService.EnqueueCommand(cmd);
            cmd.Wait();
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="session"></param>
        /// <param name="last"></param>
        /// <returns></returns>
        private IActionResult GetHistory(Session session, bool last = false)
        {
            Guard.Argument(session, nameof(session)).NotNull();
            AutoResetEvent cmdFinishedEvent = new AutoResetEvent(false);
            RcpWalletTxHistoryCommand cmd =
                new RcpWalletTxHistoryCommand(_serviceProvider, ref cmdFinishedEvent, session);
            SendCommandAndAwaitResponse(cmd);
            var history = cmd.Result;
            if (history.Item1 is null) return new BadRequestObjectResult(history.Item2);
            var balance = history.Item1 as IList<BalanceSheet>;
            return last ? new OkObjectResult($"{balance.Last()}") : new OkObjectResult(balance);
        }

        /// <summary>
        /// Find out your address
        /// </summary>
        /// <param name="credentials"></param>
        /// <returns></returns>
        [HttpPost("address", Name = "Address")]
        public IActionResult Address([FromBody] Credentials credentials)
        {
            var session = GetSessionFromCredentials(credentials);
            if (null == session)
            {
                return new BadRequestObjectResult("Invalid identifier or password!");
            }

            var cmdFinishedEvent = new AutoResetEvent(false);
            var cmd = new RpcWalletAddressCommand(_serviceProvider, ref cmdFinishedEvent, session);
            SendCommandAndAwaitResponse(cmd);
            var result = cmd.Result;
            return result.Item1 is null
                ? new BadRequestObjectResult(result.Item2)
                : new OkObjectResult(new { address = result.Item1 as string });
        }

        /// <summary>
        /// Get your wallet balance
        /// </summary>
        /// <param name="credentials"></param>
        /// <returns></returns>
        [HttpPost("balance", Name = "Balance")]
        public IActionResult Balance([FromBody] Credentials credentials)
        {
            var session = GetSessionFromCredentials(credentials);
            if (session == null) return new BadRequestObjectResult("Invalid identifier or password!");
            var cmdFinishedEvent = new AutoResetEvent(false);
            var cmd = new RcpWalletTxHistoryCommand(_serviceProvider, ref cmdFinishedEvent, session);
            SendCommandAndAwaitResponse(cmd);
            var balanceSheet = cmd.Result;
            if (balanceSheet.Item1 is null) return new BadRequestObjectResult("Nothing to see");
            var balanceSheets = balanceSheet.Item1 as IReadOnlyCollection<BalanceSheet>;
            return new OkObjectResult(new { balance = balanceSheets.Last().Balance });
        }

        /// <summary>
        /// Create new wallet
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        [HttpPost("create", Name = "Create")]
        public IActionResult Create(string name)
        {
            AutoResetEvent cmdFinishedEvent = new AutoResetEvent(false);
            RpcCreateWalletCommand cmd = new RpcCreateWalletCommand(name, _serviceProvider, ref cmdFinishedEvent);
            SendCommandAndAwaitResponse(cmd);

            return cmd.Result.Item1 is null
                ? new BadRequestObjectResult(cmd.Result.Item2)
                : new OkObjectResult(cmd.Result.Item1);
        }

        /// <summary>
        /// Restore wallet from seed and passphrase
        /// </summary>
        /// <param name="name"></param>
        /// <param name="seed"></param>
        /// <param name="passphrase"></param>
        /// <returns></returns>
        [HttpPost("restore", Name = "Restore")]
        public IActionResult Restore(string name, string seed = null, string passphrase = null)
        {
            var cmdFinishedEvent = new AutoResetEvent(false);
            var cmd = new RpcWalletRestoreCommand(name, seed, passphrase, _serviceProvider, ref cmdFinishedEvent);
            SendCommandAndAwaitResponse(cmd);
            return cmd.Result.Item1 is null
                ? new BadRequestObjectResult(cmd.Result.Item2)
                : new OkObjectResult(cmd.Result.Item1);
        }

        /// <summary>
        /// Create new seed and passphrase
        /// </summary>
        /// <param name="mnemonicWordCount"></param>
        /// <param name="passphraseWordCount"></param>
        /// <returns></returns>
        [HttpGet("newseed", Name = "CreateSeed")]
        public IActionResult CreateSeed(WordCount mnemonicWordCount = WordCount.TwentyFour,
            WordCount passphraseWordCount = WordCount.Twelve)
        {
            var cmdFinishedEvent = new AutoResetEvent(false);
            var cmd = new RpcCreateSeedCommand(mnemonicWordCount, passphraseWordCount,
                _serviceProvider, ref cmdFinishedEvent);
            SendCommandAndAwaitResponse(cmd);
            return cmd.Result.Item1 is null
                ? new BadRequestObjectResult(cmd.Result.Item2)
                : new ObjectResult(cmd.Result.Item1);
        }

        /// <summary>
        /// Available wallets
        /// </summary>
        /// <returns></returns>
        [HttpGet("list", Name = "List")]
        public IActionResult List()
        {
            var cmdFinishedEvent = new AutoResetEvent(false);
            var cmd = new RpcWalletListommand(_serviceProvider, ref cmdFinishedEvent);
            SendCommandAndAwaitResponse(cmd);
            return cmd.Result.Item1 is null
                ? new BadRequestObjectResult(cmd.Result.Item2)
                : new OkObjectResult(cmd.Result.Item1 as List<string>);
        }

        /// <summary>
        /// Show my transactions
        /// </summary>
        /// <param name="credentials"></param>
        /// <returns></returns>
        [HttpPost("history", Name = "History")]
        public IActionResult History([FromBody] Credentials credentials)
        {
            var session = GetSessionFromCredentials(credentials);
            return null == session
                ? new BadRequestObjectResult("Invalid identifier or password!")
                : GetHistory(session);
        }

        /// <summary>
        /// Receive a payment
        /// </summary>
        /// <param name="receive"></param>
        /// <returns></returns>
        [HttpPost("receive", Name = "Receive")]
        public IActionResult Receive([FromBody] Receive receive)
        {
            Guard.Argument(receive.Username, nameof(receive.Username)).NotNull().NotEmpty().NotWhiteSpace();
            Guard.Argument(receive.Passphrase, nameof(receive.Passphrase)).NotNull().NotEmpty().NotWhiteSpace();
            Guard.Argument(receive.TransactionId, nameof(receive.TransactionId)).NotNull().NotEmpty().NotWhiteSpace();
            var session =
                GetSessionFromCredentials(new Credentials
                {
                    Username = receive.Username,
                    Passphrase = receive.Passphrase
                });
            if (null == session)
            {
                return new BadRequestObjectResult("Invalid identifier or password!");
            }

            var cmdFinishedEvent = new AutoResetEvent(false);
            var cmd =
                new RpcWalletReceiveCommand(receive.TransactionId, _serviceProvider, ref cmdFinishedEvent, session);
            SendCommandAndAwaitResponse(cmd);
            return cmd.Result.Item1 is null
                ? new BadRequestObjectResult(cmd.Result.Item2)
                : new OkObjectResult(cmd.Result.Item1);
        }

        /// <summary>
        /// Spend some crypto
        /// </summary>
        /// <param name="spend"></param>
        /// <returns></returns>
        [HttpPost("spend", Name = "Spend")]
        public IActionResult Spend([FromBody] Spend spend)
        {
            Guard.Argument(spend.Username, nameof(spend.Username)).NotNull().NotEmpty().NotWhiteSpace();
            Guard.Argument(spend.Passphrase, nameof(spend.Passphrase)).NotNull().NotEmpty().NotWhiteSpace();
            Guard.Argument(spend.Address, nameof(spend.Address)).NotNull().NotEmpty().NotWhiteSpace();
            Guard.Argument(spend.Amount, nameof(spend.Amount)).Positive();
            var session =
                GetSessionFromCredentials(
                    new Credentials { Username = spend.Username, Passphrase = spend.Passphrase });
            if (null == session)
            {
                return new BadRequestObjectResult("Invalid identifier or password!");
            }

            var senderAddress = session.KeySet.StealthAddress;
            session.SessionType = SessionType.Coin;
            var transaction = new WalletTransaction
            {
                Memo = spend.Memo,
                Payment = spend.Amount.ConvertToUInt64(),
                RecipientAddress = spend.Address,
                WalletType = WalletType.Send,
                SenderAddress = senderAddress,
                IsVerified = false,
                Delay = 5
            };
            var cmdFinishedEvent = new AutoResetEvent(false);
            var cmd = new RpcSpendCommand(ref transaction, _serviceProvider, ref cmdFinishedEvent, session);
            SendCommandAndAwaitResponse(cmd);
            return cmd.Result.Item1 is null
                ? new BadRequestObjectResult(cmd.Result.Item2)
                : new OkObjectResult(cmd.Result.Item1);
        }

        /// <summary>
        /// Setup staking on your node
        /// </summary>
        /// <param name="stakeCredentials"></param>
        /// <returns></returns>
        [HttpPost("stake", Name = "Stake")]
        public IActionResult Stake([FromBody] StakeCredentials stakeCredentials)
        {
            var session =
                GetSessionFromCredentials(new Credentials
                {
                    Username = stakeCredentials.Name,
                    Passphrase = stakeCredentials.Passphrase
                });
            if (null == session)
            {
                return new BadRequestObjectResult("Invalid identifier or password!");
            }
            var cmdFinishedEvent = new AutoResetEvent(false);
            var cmd =
                new RcpWalletStakeCommand(stakeCredentials, _serviceProvider, ref cmdFinishedEvent,
                    session);
            SendCommandAndAwaitResponse(cmd);
            return cmd.Result.Item1 == null
                ? new BadRequestObjectResult(cmd.Result.Item2)
                : new OkObjectResult(cmd.Result.Item2);
        }

        /// <summary>
        /// Get wallet version
        /// </summary>
        /// <returns></returns>
        [HttpGet("version", Name = "Version")]
        public IActionResult Version()
        {
            var cmdFinishedEvent = new AutoResetEvent(false);
            var cmd = new RcpVersionCommand(_serviceProvider, ref cmdFinishedEvent);
            SendCommandAndAwaitResponse(cmd);
            return cmd.Result.Item1 is null
                ? new BadRequestObjectResult(cmd.Result.Item2)
                : new ObjectResult(cmd.Result.Item1);
        }

        /// <summary>
        /// Raw transactions outputs
        /// </summary>
        /// <param name="credentials"></param>
        /// <returns></returns>
        [HttpPost("rawoutputs", Name = "RawOutputs")]
        public IActionResult RawOutputs([FromBody] Credentials credentials)
        {
            var session = GetSessionFromCredentials(credentials);
            return null == session
                ? new BadRequestObjectResult("Invalid identifier or password!")
                : GetRwaOutputs(session);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="session"></param>
        /// <returns></returns>
        private IActionResult GetRwaOutputs(Session session)
        {
            Guard.Argument(session, nameof(session)).NotNull();
            AutoResetEvent cmdFinishedEvent = new AutoResetEvent(false);
            var cmd =
                new RcpWalletRawOutputsCommand(_serviceProvider, ref cmdFinishedEvent, session);
            SendCommandAndAwaitResponse(cmd);
            var outputs = cmd.Result;
            if (outputs.Item1 is null) return new BadRequestObjectResult(outputs.Item2);
            return new OkObjectResult(outputs.Item1);
        }
    }
}