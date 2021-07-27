﻿// Bamboo (c) by Tangram
//
// Bamboo is licensed under a
// Creative Commons Attribution-NonCommercial-NoDerivatives 4.0 International License.
//
// You should have received a copy of the license along with this
// work. If not, see <http://creativecommons.org/licenses/by-nc-nd/4.0/>.

using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using McMaster.Extensions.CommandLineUtils;
using BAMWallet.Extensions;
using BAMWallet.HD;
using Kurukuru;
namespace CLi.ApplicationLayer.Commands.Wallet
{
    [CommandDescriptor("balance", "Get your wallet balance")]
    public class WalletBalanceCommand : Command
    {
        private readonly IWalletService _walletService;
        private readonly IConsole _console;

        private Spinner _spinner;

        public WalletBalanceCommand(IServiceProvider serviceProvider): base(typeof(WalletBalanceCommand).GetAttributeValue((CommandDescriptorAttribute attr) => attr.Name),
            typeof(WalletBalanceCommand).GetAttributeValue((CommandDescriptorAttribute attr) => attr.Description))
        {
            _walletService = serviceProvider.GetService<IWalletService>();
            _console = serviceProvider.GetService<IConsole>();
        }

        public override async Task Execute()
        {
            try
            {
                Login();
                await Spinner.StartAsync("Checking balance ...", spinner =>
                {
                    _spinner = spinner;
                    var session = ActiveSession;
                    var balance = _walletService.History(session);
                    if (balance.Success)
                    {
                        _console.ForegroundColor = ConsoleColor.Green;
                        _console.WriteLine($"\n Balance: {balance.Result.Last().Balance}");
                        _console.ForegroundColor = ConsoleColor.White;
                    }
                    else
                    {
                        _console.ForegroundColor = ConsoleColor.Red;
                        _console.WriteLine($"\n {balance.NonSuccessMessage}");
                        _console.ForegroundColor = ConsoleColor.White;
                        spinner.Fail();
                    }
                    return Task.CompletedTask;
                });
            }
            catch (Exception)
            {
                throw;
            }
        }
    }
}
