﻿using System;
using System.Collections.Concurrent;
using System.Globalization;
using System.IO;
using System.Runtime.CompilerServices;

namespace Discord
{
    internal static class InternalExtensions
    {
		internal static readonly IFormatProvider _format = CultureInfo.InvariantCulture;
		
		public static ulong ToId(this string value)
			=> ulong.Parse(value, NumberStyles.None, _format);
		public static ulong? ToNullableId(this string value)
			=> value == null ? (ulong?)null : ulong.Parse(value, NumberStyles.None, _format);
        public static bool TryToId(this string value, out ulong result)
            => ulong.TryParse(value, NumberStyles.None, _format, out result);

        public static string ToIdString(this ulong value)
			=> value.ToString(_format);
		public static string ToIdString(this ulong? value)
			=> value?.ToString(_format);
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool HasBit(this uint rawValue, byte bit) => ((rawValue >> bit) & 1U) == 1;

        public static bool TryGetOrAdd<TKey, TValue>(this ConcurrentDictionary<TKey, TValue> d, 
            TKey key, Func<TKey, TValue> factory, out TValue result)
        {
            bool created = false;
            TValue newValue = default(TValue);
            while (true)
            {
                if (d.TryGetValue(key, out result))
                    return false;
                if (!created)
                {
                    newValue = factory(key);
                    created = true;
                }
                if (d.TryAdd(key, newValue))
                {
                    result = newValue;
                    return true;
                }
            }
        }
        public static bool TryGetOrAdd<TKey, TValue>(this ConcurrentDictionary<TKey, TValue> d,
            TKey key, TValue value, out TValue result)
        {
            while (true)
            {
                if (d.TryGetValue(key, out result))
                    return false;
                if (d.TryAdd(key, value))
                {
                    result = value;
                    return true;
                }
            }
        }

        public static string Base64(this Stream stream, ImageType type, string existingId)
        {
            if (type == ImageType.None)
                return null;
            else if (stream != null)
            {
                byte[] bytes = new byte[stream.Length - stream.Position];
                stream.Read(bytes, 0, bytes.Length);

                string base64 = Convert.ToBase64String(bytes);
                string imageType = type == ImageType.Jpeg ? "image/jpeg;base64" : "image/png;base64";
                return $"data:{imageType},{base64}";
            }
            return existingId;
        }
    }
}
