// Improved by ChatGPT

using System.Runtime.CompilerServices;
using Serilog;

namespace BAMWallet.Extensions
{
    public static class LoggerExtensions
    {
        public static ILogger Here(this ILogger logger,
            [CallerMemberName] string memberName = null,
            [CallerLineNumber] int sourceLineNumber = 0)
        {
            if (logger == null)
            {
                throw new ArgumentNullException(nameof(logger));
            }

            return logger
                .ForContext("MemberName", memberName)
                .ForContext("LineNumber", sourceLineNumber);
        }
    }
}
