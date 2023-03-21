// Bamboo (c) by Tangram
//
// Bamboo is licensed under a
// Creative Commons Attribution-NonCommercial-NoDerivatives 4.0 International License.
//
// You should have received a copy of the license along with this
// work. If not, see <http://creativecommons.org/licenses/by-nc-nd/4.0/>.

using System;

namespace Cli.UI
{
    public class TextInput<T> : IUserInterfaceInput<T>
    {
        public string Prompt { get; }
        private readonly Func<string, bool>? _validation;
        private readonly Func<string, T>? _cast;

        public TextInput(string prompt, Func<string, bool>? validation = null, Func<string, T>? cast = null)
        {
            Prompt = prompt;
            _validation = validation;
            _cast = cast;
        }

        public bool IsValid(string value) => _validation?.Invoke(value) ?? true;

        public bool Cast(string input, out T output)
        {
            output = default!;

            if (_cast != null && _cast(input) is T castedValue)
            {
                output = castedValue;
                return true;
            }

            return false;
        }
    }
}
