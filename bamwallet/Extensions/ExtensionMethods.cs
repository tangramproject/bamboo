﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Mime;
using System.Runtime.CompilerServices;
using System.Text;

using Newtonsoft.Json;

namespace BAMWallet.Extensions
{
    public static class ExtensionMethods
    {
        public static StringContent AsJson(this object o)
          => new(JsonConvert.SerializeObject(o), Encoding.UTF8, MediaTypeNames.Application.Json);

        public static string AsJsonString(this object o) => JsonConvert.SerializeObject(o);

        public static T Cast<T>(this object o) => JsonConvert.DeserializeObject<T>(o.AsJsonString());

        public static IEnumerable<IEnumerable<T>> Split<T>(this T[] array, int size)
        {
            for (var i = 0; i < (float)array.Length / size; i++)
            {
                yield return array.Skip(i * size).Take(size);
            }
        }
        public static void ExecuteInConstrainedRegion(this Action action)
        {
#pragma warning disable SYSLIB0004 // Type or member is obsolete
            RuntimeHelpers.PrepareConstrainedRegions();
#pragma warning restore SYSLIB0004 // Type or member is obsolete

            try
            {
            }
            finally
            {
                action();
            }
        }

        public static byte[] ToBytes<T>(this T arg) => Encoding.UTF8.GetBytes(arg.ToString());

        public static IEnumerable<(T item, int index)> WithIndex<T>(this IEnumerable<T> source)
        {
            return source.Select((item, index) => (item, index));
        }
    }
}
