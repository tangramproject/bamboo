// Bamboo (c) by Tangram
//
// Bamboo is licensed under a
// Creative Commons Attribution-NonCommercial-NoDerivatives 4.0 International License.
//
// You should have received a copy of the license along with this
// work. If not, see <http://creativecommons.org/licenses/by-nc-nd/4.0/>.

using System;
using System.Linq;
using System.Text.RegularExpressions;

namespace Cli.UI
{
    public class TerminalUserInterface : UserInterfaceBase
    {
        private const int IndentSize = 4;

        public override UserInterfaceChoice Do(UserInterfaceSection section)
        {
            while (true)
            {
                Console.Clear();
                PrintHeader(section.Title);
                Print(section.Description, IndentSize);

                Console.WriteLine();
                if (section.Choices == null) return null;

                for (var choiceIndex = 0; choiceIndex < section.Choices.Length; ++choiceIndex)
                {
                    Print($"{choiceIndex + 1}: {section.Choices[choiceIndex].Text}", IndentSize);
                }
                Print($"{section.Choices.Length + 1}: Cancel", IndentSize);

                Console.WriteLine();

                var choiceStr = Console.ReadLine();
                if (int.TryParse(choiceStr, out var choiceInt))
                {
                    if (choiceInt > 0 && choiceInt <= section.Choices.Length)
                    {
                        return section.Choices[choiceInt - 1];
                    }

                    return new UserInterfaceChoice(string.Empty);
                }
            }
        }

        public override bool Do<T>(IUserInterfaceInput<T> input, out T output)
        {
            output = default;
            var validInput = false;
            while (!validInput)
            {
                Console.WriteLine();
                Console.Write($"{GetIndentString(IndentSize)}{input.Prompt}: ");

                var inputString = Console.ReadLine();
                if (input.IsValid(inputString))
                {
                    validInput = input.Cast(inputString, out output);
                }
            }

            return true;
        }

        private void PrintHeader(string header)
        {
            Console.WriteLine($"{_topic} | {header}\n");
        }

        private static void Print(string text, int indent = 0)
        {
            var lineWidth = Console.WindowWidth - indent;
            var pattern = $@"(?<line>.{{1,{lineWidth}}})(?<!\s)(\s+|$)|(?<line>.+?)(\s+|$)";
            var lines = Regex.Matches(text, pattern).Select(m => m.Groups["line"].Value);

            foreach (var line in lines)
            {
                Console.WriteLine($"{GetIndentString(indent)}{line}");
            }
        }

        private static string GetIndentString(int indent)
        {
            return new string(' ', indent);
        }
    }
}
