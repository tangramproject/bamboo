﻿// Bamboo (c) by Tangram 
// 
// Bamboo is licensed under a
// Creative Commons Attribution-NonCommercial-NoDerivatives 4.0 International License.
// 
// You should have received a copy of the license along with this
// work. If not, see <http://creativecommons.org/licenses/by-nc-nd/4.0/>.

using System;
using System.Threading.Tasks;

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;

using Newtonsoft.Json;

using McMaster.Extensions.CommandLineUtils;

using Kurukuru;

using BAMWallet.HD;
using BAMWallet.Model;
using BAMWallet.Extentions;

namespace CLi.ApplicationLayer.Commands.Wallet
{
    [CommandDescriptor(new string[] { "spend" }, "Spend some coins")]
    public class WalletTransferCommand : Command
    {
        private readonly IWalletService _walletService;
        private readonly IConsole _console;
        private readonly ILogger _logger;

        private Spinner spinner;

        public WalletTransferCommand(IServiceProvider serviceProvider)
        {
            _walletService = serviceProvider.GetService<IWalletService>();
            _console = serviceProvider.GetService<IConsole>();
            _logger = serviceProvider.GetService<ILogger>();
        }

        public override async Task Execute()
        {
            using var identifier = Prompt.GetPasswordAsSecureString("Identifier:", ConsoleColor.Yellow);
            using var passphrase = Prompt.GetPasswordAsSecureString("Passphrase:", ConsoleColor.Yellow);

            var address = Prompt.GetString("Address:", null, ConsoleColor.Red);
            var amount = Prompt.GetString("Amount:", null, ConsoleColor.Red);
            var memo = Prompt.GetString("Memo:", null, ConsoleColor.Green);

            if (double.TryParse(amount, out double t))
            {
                await Spinner.StartAsync("Processing payment ...", async spinner =>
                {
                    this.spinner = spinner;

                    try
                    {
                        var session = _walletService.SessionAddOrUpdate(new Session(identifier, passphrase)
                        {
                            Amount = t.ConvertToUInt64(),
                            Memo = memo,
                            RecipientAddress = address,
                            SessionType = SessionType.Coin
                        });

                        await _walletService.TransferPayment(session.SessionId);

                        if (session.LastError != null)
                        {
                            spinner.Fail(JsonConvert.SerializeObject(session.LastError.GetValue("message")));
                            return;
                        }

                        var balance = _walletService.AvailableBalance(session.SessionId);

                        spinner.Text = $"Available Balance: {balance.Result.DivWithNaT():F9}";
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex.StackTrace);
                        throw;
                    }
                }, Patterns.Toggle3);
            }
        }
    }
}
