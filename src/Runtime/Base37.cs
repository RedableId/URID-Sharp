using System;
using System.Runtime.CompilerServices;

namespace URID
{
    /// <summary>
    /// Shared helpers for bijective base-37 encoding.
    /// Digits are 1–37 (a-z = 1-26, 0-9 = 27-36, separator = 37).
    /// V = 0 represents the empty identifier; no leading-zero ambiguity.
    /// All multi-word integers are little-endian (V0 is least significant).
    /// </summary>
    internal static class Base37
    {
        // ── alphabet layout ──────────────────────────────────────────────────
        private const uint LettersCount = 26;
        private const uint DigitsCount  = 10;
        private const uint DigitsFirst  = LettersCount + 1;
        private const uint DigitsLast   = LettersCount + DigitsCount;
        private const uint Base = 37;

        // ── alphabet ─────────────────────────────────────────────────────────

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static uint Encode(char c)
        {
            uint u = (uint)((c | 0x20u) - 'a');
            if (u < LettersCount) return u + 1;           // 'a'=1 … 'z'=26
            u = (uint)(c - '0');
            if (u < DigitsCount)  return u + DigitsFirst; // '0'=27 … '9'=36
            return Base;                                  // '_' (or anything else) → 37
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static char Decode(uint v)
        {
            if (v <= LettersCount) return (char)('a' + v - 1);
            if (v <= DigitsLast)   return (char)('0' + v - DigitsFirst);
            return '_';
        }

        // ── generic helpers ──────────────────────────────────────────────────
        // The JIT creates a per-T specialization for each concrete VecN type.
        // No boxing; interface methods resolve to direct calls via constrained callvirt.

        // Encode src into v (caller must pass a zeroed VecN), truncating to maxSyms.
        internal static void EncodeString<TVec>(ReadOnlySpan<char> src, ref TVec v, int maxSyms)
            where TVec : struct, IVec
        {
            int limit = src.Length < maxSyms ? src.Length : maxSyms;
            for (int i = 0; i < limit; i++)
                v.Mul37Add(Encode(src[i]));
        }

        // Decode v (passed by value — caller's copy is unchanged) into dst.
        // Returns the number of chars written.
        internal static int DecodeChars<T>(T v, Span<char> dst)
            where T : struct, IVec
        {
            int len = 0;
            while (!v.IsZero) { v.SubOne(); dst[len++] = Decode(v.Div37() + 1); }
            dst.Slice(0, len).Reverse();
            return len;
        }

        // Count encoded symbols without modifying the original (v is passed by value).
        internal static int SymbolCount<T>(T v)
            where T : struct, IVec
        {
            int len = 0;
            while (!v.IsZero) { v.SubOne(); v.Div37(); len++; }
            return len;
        }
    }
}
