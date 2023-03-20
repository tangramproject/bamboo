// Improved by ChatGPT

using System;
using System.Linq;

namespace BAMWallet.Extensions
{
    public static class AttributeExtensions
    {
        public static TValue GetAttributeValue<TAttribute, TValue>(
            this Type type,
            Func<TAttribute, TValue> valueSelector)
            where TAttribute : Attribute
        {
            if (type == null)
            {
                throw new ArgumentNullException(nameof(type));
            }

            var att = type.GetCustomAttributes(typeof(TAttribute), true)
                          .FirstOrDefault(a => a is TAttribute) as TAttribute;
            return att != null ? valueSelector(att) : default;
        }
    }
}
