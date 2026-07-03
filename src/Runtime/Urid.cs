using System;

namespace URID
{
    // ─────────────────────────────────────────────────────────────────────────
    //  Urid64   –  64-bit storage, up to 12 symbols
    // ─────────────────────────────────────────────────────────────────────────
    public readonly struct Urid64 : IEquatable<Urid64>
    {
        public const int MaxLength = 12;
        private readonly ulong _v;

        internal Urid64(ulong raw) => _v = raw;

        public Urid64(ReadOnlySpan<char> src)
        {
            var v = new Vec64();
            Base37.EncodeString(src, ref v, MaxLength);
            _v = v.V0;
        }

        internal ulong RawWord => _v;
        private Vec64 ToVec() => new Vec64(_v);

        public int Length => Base37.SymbolCount(ToVec());

        public int WriteChars(Span<char> dst) => Base37.DecodeChars(ToVec(), dst);

        public char this[int index]
        {
            get
            {
                Span<char> buf = stackalloc char[MaxLength];
                int        len = WriteChars(buf);
                return (uint)index < (uint)len ? buf[index] : '\0';
            }
        }

        public override string ToString()
        {
            Span<char> buf = stackalloc char[MaxLength];
            int        len = WriteChars(buf);
            return len == 0 ? string.Empty : new string(buf.Slice(0, len));
        }

        public static bool operator ==(Urid64 a, Urid64 b) => a._v == b._v;
        public static bool operator !=(Urid64 a, Urid64 b) => a._v != b._v;
        public bool Equals(Urid64 other) => _v == other._v;
        public override bool Equals(object? obj) => obj is Urid64 o && Equals(o);
        public override int GetHashCode() => _v.GetHashCode();
        public static implicit operator Urid64(string s) => new Urid64(s.AsSpan());
    }

    // ─────────────────────────────────────────────────────────────────────────
    //  Urid128  –  128-bit storage, up to 24 symbols
    // ─────────────────────────────────────────────────────────────────────────
    public readonly struct Urid128 : IEquatable<Urid128>
    {
        public const int MaxLength = 24;
        private readonly ulong _v0, _v1;

        internal Urid128(ulong v0, ulong v1) => (_v0, _v1) = (v0, v1);

        public Urid128(ReadOnlySpan<char> src)
        {
            var v = new Vec128();
            Base37.EncodeString(src, ref v, MaxLength);
            (_v0, _v1) = (v.V0, v.V1);
        }

        internal Vec128 ToVec() => new Vec128(_v0, _v1);

        public int Length => Base37.SymbolCount(ToVec());

        public int WriteChars(Span<char> dst) => Base37.DecodeChars(ToVec(), dst);

        public char this[int index]
        {
            get
            {
                Span<char> buf = stackalloc char[MaxLength];
                int        len = WriteChars(buf);
                return (uint)index < (uint)len ? buf[index] : '\0';
            }
        }

        public override string ToString()
        {
            Span<char> buf = stackalloc char[MaxLength];
            int        len = WriteChars(buf);
            return len == 0 ? string.Empty : new string(buf.Slice(0, len));
        }

        public static bool operator ==(Urid128 a, Urid128 b) =>
            a._v0 == b._v0 && a._v1 == b._v1;
        public static bool operator !=(Urid128 a, Urid128 b) => !(a == b);
        public bool Equals(Urid128 other) => this == other;
        public override bool Equals(object? obj) => obj is Urid128 o && Equals(o);
        public override int GetHashCode() => HashCode.Combine(_v0, _v1);

        public static implicit operator Urid128(string s)      => new Urid128(s.AsSpan());
        public static implicit operator Urid128(Urid64 src)    => new Urid128(src.RawWord, 0UL);
    }

    // ─────────────────────────────────────────────────────────────────────────
    //  Urid192  –  192-bit storage, up to 36 symbols
    // ─────────────────────────────────────────────────────────────────────────
    public readonly struct Urid192 : IEquatable<Urid192>
    {
        public const int MaxLength = 36;
        private readonly ulong _v0, _v1, _v2;

        internal Urid192(ulong v0, ulong v1, ulong v2) => (_v0, _v1, _v2) = (v0, v1, v2);

        public Urid192(ReadOnlySpan<char> src)
        {
            var v = new Vec192();
            Base37.EncodeString(src, ref v, MaxLength);
            (_v0, _v1, _v2) = (v.V0, v.V1, v.V2);
        }

        internal Vec192 ToVec() => new Vec192(_v0, _v1, _v2);

        public int Length => Base37.SymbolCount(ToVec());

        public int WriteChars(Span<char> dst) => Base37.DecodeChars(ToVec(), dst);

        public char this[int index]
        {
            get
            {
                Span<char> buf = stackalloc char[MaxLength];
                int        len = WriteChars(buf);
                return (uint)index < (uint)len ? buf[index] : '\0';
            }
        }

        public override string ToString()
        {
            Span<char> buf = stackalloc char[MaxLength];
            int        len = WriteChars(buf);
            return len == 0 ? string.Empty : new string(buf.Slice(0, len));
        }

        public static bool operator ==(Urid192 a, Urid192 b) =>
            a._v0 == b._v0 && a._v1 == b._v1 && a._v2 == b._v2;
        public static bool operator !=(Urid192 a, Urid192 b) => !(a == b);
        public bool Equals(Urid192 other) => this == other;
        public override bool Equals(object? obj) => obj is Urid192 o && Equals(o);
        public override int GetHashCode() => HashCode.Combine(_v0, _v1, _v2);

        public static implicit operator Urid192(string s)      => new Urid192(s.AsSpan());
        public static implicit operator Urid192(Urid64 src)    => new Urid192(src.RawWord, 0UL, 0UL);
        public static implicit operator Urid192(Urid128 src)
        {
            var v = src.ToVec();
            return new Urid192(v.V0, v.V1, 0UL);
        }
    }

    // ─────────────────────────────────────────────────────────────────────────
    //  Urid256  –  256-bit storage, up to 49 symbols
    // ─────────────────────────────────────────────────────────────────────────
    public readonly struct Urid256 : IEquatable<Urid256>
    {
        public const int MaxLength = 49;
        private readonly ulong _v0, _v1, _v2, _v3;

        internal Urid256(ulong v0, ulong v1, ulong v2, ulong v3) =>
            (_v0, _v1, _v2, _v3) = (v0, v1, v2, v3);

        public Urid256(ReadOnlySpan<char> src)
        {
            var v = new Vec256();
            Base37.EncodeString(src, ref v, MaxLength);
            (_v0, _v1, _v2, _v3) = (v.V0, v.V1, v.V2, v.V3);
        }

        private Vec256 ToVec() => new Vec256(_v0, _v1, _v2, _v3);

        public int Length => Base37.SymbolCount(ToVec());

        public int WriteChars(Span<char> dst) => Base37.DecodeChars(ToVec(), dst);

        public char this[int index]
        {
            get
            {
                Span<char> buf = stackalloc char[MaxLength];
                int        len = WriteChars(buf);
                return (uint)index < (uint)len ? buf[index] : '\0';
            }
        }

        public override string ToString()
        {
            Span<char> buf = stackalloc char[MaxLength];
            int        len = WriteChars(buf);
            return len == 0 ? string.Empty : new string(buf.Slice(0, len));
        }

        public static bool operator ==(Urid256 a, Urid256 b) =>
            a._v0 == b._v0 && a._v1 == b._v1 && a._v2 == b._v2 && a._v3 == b._v3;
        public static bool operator !=(Urid256 a, Urid256 b) => !(a == b);
        public bool Equals(Urid256 other) => this == other;
        public override bool Equals(object? obj) => obj is Urid256 o && Equals(o);
        public override int GetHashCode() => HashCode.Combine(_v0, _v1, _v2, _v3);

        public static implicit operator Urid256(string s)      => new Urid256(s.AsSpan());
        public static implicit operator Urid256(Urid64 src)    => new Urid256(src.RawWord, 0UL, 0UL, 0UL);
        public static implicit operator Urid256(Urid128 src)
        {
            var v = src.ToVec();
            return new Urid256(v.V0, v.V1, 0UL, 0UL);
        }
        public static implicit operator Urid256(Urid192 src)
        {
            var v = src.ToVec();
            return new Urid256(v.V0, v.V1, v.V2, 0UL);
        }
    }
}
