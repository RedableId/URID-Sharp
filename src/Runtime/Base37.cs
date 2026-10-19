using System.Runtime.CompilerServices;

namespace URID
{
    /// <summary>
    /// Alphabet of the bijective base-37 encoding.
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
    }
}
