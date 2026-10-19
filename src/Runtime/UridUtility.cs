using System;
using System.Runtime.CompilerServices;

namespace URID
{
    /// <summary>
    /// The whole Urid implementation, as extension methods over the VecN storage types.
    /// The JIT (and Burst) emits a separate specialization per concrete VecN: no boxing,
    /// interface calls become direct calls through the struct constraint.
    /// </summary>
    /// <remarks>
    /// Vectors are taken by value on purpose: decoding consumes a mutable working copy,
    /// so the caller's value is never modified. After inlining, the copy lives in registers.
    /// </remarks>
    internal static class UridUtility
    {
        /// Encodes up to <paramref name="maxLength"/> symbols of <paramref name="src"/>.
        internal static void Parse<TVec>(ReadOnlySpan<char> src, int maxLength, out TVec v)
            where TVec : struct, IVec
        {
            v = default;
            int limit = src.Length < maxLength ? src.Length : maxLength;
            for (int i = 0; i < limit; i++)
                v.Mul37Add(Base37.Encode(src[i]));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static int GetLength<TVec>(this TVec v)
            where TVec : struct, IVec
        {
            int len = 0;
            while (!v.IsZero) { v.SubOne(); v.Div37(); len++; }
            return len;
        }

        /// Writes the symbols into <paramref name="dst"/>; returns the number written.
        internal static int WriteChars<TVec>(this TVec v, Span<char> dst)
            where TVec : struct, IVec
        {
            int len = 0;
            while (!v.IsZero) { v.SubOne(); dst[len++] = Base37.Decode(v.Div37() + 1); }
            dst.Slice(0, len).Reverse();
            return len;
        }

        /// Returns the symbol at <paramref name="index"/>, or '\0' when out of range.
        internal static char GetChar<TVec>(this TVec v, int index)
            where TVec : struct, IVec
        {
            int len = v.GetLength();
            if ((uint)index >= (uint)len) return '\0';
            // Symbols come out last-first: drop the ones after index, then decode one.
            for (int skip = len - 1 - index; skip > 0; skip--) { v.SubOne(); v.Div37(); }
            v.SubOne();
            return Base37.Decode(v.Div37() + 1);
        }

        internal static string Format<TVec>(this TVec v)
            where TVec : struct, IVec
        {
            Span<char> buf = stackalloc char[Urid256.MaxLength];
            int len = v.WriteChars(buf);
            return len == 0 ? string.Empty : new string(buf[..len]);
        }
    }
}
