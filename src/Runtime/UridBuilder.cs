using System;
using System.Runtime.CompilerServices;

namespace URID
{
    /// <summary>
    /// Stack-only builder for bijective base-37 identifiers.
    /// Accumulates up to 49 symbols (the capacity of Urid256), then
    /// converts to any of the four vector sizes. Symbols beyond a target
    /// size's capacity are silently dropped in the corresponding ToXxx() call.
    /// </summary>
    public ref struct UridBuilder
    {
        private Vec256 _v;
        private int _length;

        public int  Length => _length;
        public bool IsFull => _length >= Urid256.MaxLength;

        // ── core append ──────────────────────────────────────────────────────

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void AppendCode(uint code)
        {
            if (_length < Urid256.MaxLength)
            {
                _v.Mul37Add(code);
                _length++;
            }
        }

        // ── public API ───────────────────────────────────────────────────────

        public void Append(char symbol) =>
            AppendCode(Base37.Encode(symbol));

        public void Append(ReadOnlySpan<char> token)
        {
            int lim = Math.Min(token.Length, Urid256.MaxLength - _length);
            for (int i = 0; i < lim; i++)
                AppendCode(Base37.Encode(token[i]));
        }

        /// Appends the decimal representation of <paramref name="number"/>.
        /// Negative values are treated as their absolute value (no minus sign).
        public void Append(int number)
        {
            uint n = number >= 0 ? (uint)number : (uint)-number;
            if (n == 0) { AppendCode(Base37.Encode('0')); return; }
            Span<char> buf = stackalloc char[10]; // max 10 decimal digits for int
            int len = 0;
            while (n > 0) { buf[len++] = (char)('0' + n % 10); n /= 10; }
            // C# 9 has no `scoped` parameters, so a stackalloc'd span can't be
            // passed to Append(ReadOnlySpan<char>) from inside this ref struct;
            // append the digits directly (most significant first).
            for (int i = len - 1; i >= 0; i--)
                AppendCode(Base37.Encode(buf[i]));
        }

        public void AppendSeparator() =>
            AppendCode(Base37.Encode('_'));

        // ── conversion ───────────────────────────────────────────────────────
        // Fast path: copy the low words directly (no decode/re-encode).
        // Slow path (truncation): decode to a char buffer and re-encode the first N symbols.

        public Urid64 To64()
        {
            const int cap = Urid64.MaxLength;
            if (_length <= cap) return new Urid64(_v.V0);
            Span<char> buf = stackalloc char[Urid256.MaxLength];
            Base37.DecodeChars(_v, buf);
            return new Urid64(buf.Slice(0, cap));
        }

        public Urid128 To128()
        {
            const int cap = Urid128.MaxLength;
            if (_length <= cap) return new Urid128(_v.V0, _v.V1);
            Span<char> buf = stackalloc char[Urid256.MaxLength];
            Base37.DecodeChars(_v, buf);
            return new Urid128(buf.Slice(0, cap));
        }

        public Urid192 To192()
        {
            const int cap = Urid192.MaxLength;
            if (_length <= cap) return new Urid192(_v.V0, _v.V1, _v.V2);
            Span<char> buf = stackalloc char[Urid256.MaxLength];
            Base37.DecodeChars(_v, buf);
            return new Urid192(buf.Slice(0, cap));
        }

        // No truncation ever needed: builder capacity == Urid256.MaxLength.
        public Urid256 To256() => new Urid256(_v.V0, _v.V1, _v.V2, _v.V3);
    }
}
