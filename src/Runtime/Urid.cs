using System;
#if UNITY_5_3_OR_NEWER
using UnityEngine;
#endif

// Every Urid is a thin typed wrapper over a VecN; all logic lives in UridUtility.
//
// Unity's serializer ignores readonly fields, so in Unity the structs are mutable
// [Serializable] structs with a [SerializeField] storage field; everywhere else they
// are readonly structs. Members are marked readonly explicitly so the Unity variant
// gets the same no-defensive-copy guarantees as a readonly struct.
//
// The storage field name (_v) is part of Unity's serialized data format.

namespace URID
{
    // ─────────────────────────────────────────────────────────────────────────
    //  Urid64   –  64-bit storage, up to 12 symbols
    // ─────────────────────────────────────────────────────────────────────────
#if UNITY_5_3_OR_NEWER
    [Serializable]
    public struct Urid64 : IEquatable<Urid64>
#else
    public readonly struct Urid64 : IEquatable<Urid64>
#endif
    {
        public const int MaxLength = 12;

#if UNITY_5_3_OR_NEWER
        [SerializeField] private Vec64 _v;
#else
        private readonly Vec64 _v;
#endif

        internal Urid64(Vec64 v) => _v = v;
        public Urid64(ReadOnlySpan<char> src) => UridUtility.Parse(src, MaxLength, out _v);

        internal readonly Vec64 Vec => _v;

        public readonly int  Length                    => _v.GetLength();
        public readonly char this[int index]           => _v.GetChar(index);
        public readonly int  WriteChars(Span<char> dst) => _v.WriteChars(dst);
        public readonly override string ToString()     => UridUtility.Format(_v);

        public readonly bool Equals(Urid64 other)          => _v.Equals(other._v);
        public readonly override bool Equals(object? obj)  => obj is Urid64 o && Equals(o);
        public readonly override int GetHashCode()         => _v.GetHashCode();
        public static bool operator ==(Urid64 a, Urid64 b) => a._v.Equals(b._v);
        public static bool operator !=(Urid64 a, Urid64 b) => !a._v.Equals(b._v);

        public static implicit operator Urid64(string s) => new Urid64(s.AsSpan());
    }

    // ─────────────────────────────────────────────────────────────────────────
    //  Urid128  –  128-bit storage, up to 24 symbols
    // ─────────────────────────────────────────────────────────────────────────
#if UNITY_5_3_OR_NEWER
    [Serializable]
    public struct Urid128 : IEquatable<Urid128>
#else
    public readonly struct Urid128 : IEquatable<Urid128>
#endif
    {
        public const int MaxLength = 24;

#if UNITY_5_3_OR_NEWER
        [SerializeField] private Vec128 _v;
#else
        private readonly Vec128 _v;
#endif

        internal Urid128(Vec128 v) => _v = v;
        public Urid128(ReadOnlySpan<char> src) => UridUtility.Parse(src, MaxLength, out _v);

        internal readonly Vec128 Vec => _v;

        public readonly int  Length                    => _v.GetLength();
        public readonly char this[int index]           => _v.GetChar(index);
        public readonly int  WriteChars(Span<char> dst) => _v.WriteChars(dst);
        public readonly override string ToString()     => UridUtility.Format(_v);

        public readonly bool Equals(Urid128 other)           => _v.Equals(other._v);
        public readonly override bool Equals(object? obj)    => obj is Urid128 o && Equals(o);
        public readonly override int GetHashCode()           => _v.GetHashCode();
        public static bool operator ==(Urid128 a, Urid128 b) => a._v.Equals(b._v);
        public static bool operator !=(Urid128 a, Urid128 b) => !a._v.Equals(b._v);

        public static implicit operator Urid128(string s)   => new Urid128(s.AsSpan());
        public static implicit operator Urid128(Urid64 src) => new Urid128(new Vec128(src.Vec));
    }

    // ─────────────────────────────────────────────────────────────────────────
    //  Urid192  –  192-bit storage, up to 36 symbols
    // ─────────────────────────────────────────────────────────────────────────
#if UNITY_5_3_OR_NEWER
    [Serializable]
    public struct Urid192 : IEquatable<Urid192>
#else
    public readonly struct Urid192 : IEquatable<Urid192>
#endif
    {
        public const int MaxLength = 36;

#if UNITY_5_3_OR_NEWER
        [SerializeField] private Vec192 _v;
#else
        private readonly Vec192 _v;
#endif

        internal Urid192(Vec192 v) => _v = v;
        public Urid192(ReadOnlySpan<char> src) => UridUtility.Parse(src, MaxLength, out _v);

        internal readonly Vec192 Vec => _v;

        public readonly int  Length                    => _v.GetLength();
        public readonly char this[int index]           => _v.GetChar(index);
        public readonly int  WriteChars(Span<char> dst) => _v.WriteChars(dst);
        public readonly override string ToString()     => UridUtility.Format(_v);

        public readonly bool Equals(Urid192 other)           => _v.Equals(other._v);
        public readonly override bool Equals(object? obj)    => obj is Urid192 o && Equals(o);
        public readonly override int GetHashCode()           => _v.GetHashCode();
        public static bool operator ==(Urid192 a, Urid192 b) => a._v.Equals(b._v);
        public static bool operator !=(Urid192 a, Urid192 b) => !a._v.Equals(b._v);

        public static implicit operator Urid192(string s)    => new Urid192(s.AsSpan());
        public static implicit operator Urid192(Urid64 src)  => new Urid192(new Vec192(src.Vec));
        public static implicit operator Urid192(Urid128 src) => new Urid192(new Vec192(src.Vec));
    }

    // ─────────────────────────────────────────────────────────────────────────
    //  Urid256  –  256-bit storage, up to 49 symbols
    // ─────────────────────────────────────────────────────────────────────────
#if UNITY_5_3_OR_NEWER
    [Serializable]
    public struct Urid256 : IEquatable<Urid256>
#else
    public readonly struct Urid256 : IEquatable<Urid256>
#endif
    {
        public const int MaxLength = 49;

#if UNITY_5_3_OR_NEWER
        [SerializeField] private Vec256 _v;
#else
        private readonly Vec256 _v;
#endif

        internal Urid256(Vec256 v) => _v = v;
        public Urid256(ReadOnlySpan<char> src) => UridUtility.Parse(src, MaxLength, out _v);

        internal readonly Vec256 Vec => _v;

        public readonly int  Length                    => _v.GetLength();
        public readonly char this[int index]           => _v.GetChar(index);
        public readonly int  WriteChars(Span<char> dst) => _v.WriteChars(dst);
        public readonly override string ToString()     => UridUtility.Format(_v);

        public readonly bool Equals(Urid256 other)           => _v.Equals(other._v);
        public readonly override bool Equals(object? obj)    => obj is Urid256 o && Equals(o);
        public readonly override int GetHashCode()           => _v.GetHashCode();
        public static bool operator ==(Urid256 a, Urid256 b) => a._v.Equals(b._v);
        public static bool operator !=(Urid256 a, Urid256 b) => !a._v.Equals(b._v);

        public static implicit operator Urid256(string s)    => new Urid256(s.AsSpan());
        public static implicit operator Urid256(Urid64 src)  => new Urid256(new Vec256(src.Vec));
        public static implicit operator Urid256(Urid128 src) => new Urid256(new Vec256(src.Vec));
        public static implicit operator Urid256(Urid192 src) => new Urid256(new Vec256(src.Vec));
    }
}
