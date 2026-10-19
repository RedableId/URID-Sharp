using System;
using System.Runtime.CompilerServices;

namespace URID
{
    // ── internal interface ────────────────────────────────────────────────────
    // All methods mutate the receiver in place. Callers pass VecN by value to
    // generic helpers (UridUtility) so the originals are never modified.
    // ──────────────────────────────────────────────────────────────────────────

    // Constant-multiplier helpers for ×37 / ÷37 on individual 64-bit words.
    // Both methods expose a constant 37u to the JIT so it can emit a magic-multiply
    // instead of a hardware divide instruction.
    internal static class Magic37
    {
        // floor(2^64 / 37); 2^64 mod 37 = 12.
        internal const ulong Q64 = 498560650640798692UL;

        // ── Division column ──────────────────────────────────────────────────
        // DivFirst: most-significant word only (rem carry-in is always 0).
        // Returns rem; equivalent to DivStep with rem=0 but without dead operations.
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static ulong DivFirst(ref ulong word)
        {
            ulong rem = word % 37u;
            word /= 37u;
            return rem;
        }

        // DivStep: each subsequent word, carrying rem from the previous word.
        // Identity: rem * 2^64 + word = rem*Q64*37 + (rem*12 + word)
        //   → new_word = rem*Q64 + floor(t/37),  new_rem = t % 37
        //   where t = rem*12 + word  (65-bit; carry handled with a 0/1 flag).
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static void DivStep(ref ulong word, ref ulong rem)
        {
            ulong rmul = rem * 12u;
            ulong t    = rmul + word;
            ulong ov   = t < rmul ? 1u : 0u;   // 1 iff t wrapped past 2^64
            ulong t2   = t + ov * 12u;          // ov=1: t2 = (t − 2^64) + 12 ≤ 443
            ulong q    = t2 / 37u;              // JIT → magic multiply + shift
            word = (rem + ov) * Q64 + q;
            rem  = t2 - q * 37u;               // reuse q; cheaper than t2 % 37u
        }

        // ── Multiplication column ────────────────────────────────────────────
        // Computes word*37 + carry → (new_word, new_carry).
        // Requires carry ≤ 37 (symbol code 1-37 or propagated carry from prior word).
        // Uses a 32-bit split to avoid UInt128:
        //   word*37 + carry = P_hi*2^32 + P_lo
        //   mid = P_hi + (P_lo >> 32)       →  low 64 bits = (mid << 32) | P_lo_lo
        //                                      carry_out    = mid >> 32  (≤ 37)
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static void MulStep(ref ulong word, ref ulong carry)
        {
            ulong lo   = word & 0xFFFFFFFFUL;
            ulong hi   = word >> 32;
            ulong p_lo = lo * 37u + carry;        // ≤ (2^32−1)×37 + 37 = 37×2^32 < 2^38
            ulong p_hi = hi * 37u;                // ≤ (2^32−1)×37             < 2^38
            ulong mid  = p_hi + (p_lo >> 32);    // ≤ 37×2^32                  < 2^38
            word  = (mid << 32) | (p_lo & 0xFFFFFFFFUL); // ulong truncation clips mid_hi
            carry = mid >> 32;                    // ≤ 37
        }
    }

    // Deterministic hash mixing. Deliberately not System.HashCode: its per-process
    // random seed is not Burst-compatible, and Urid keys must hash inside jobs.
    internal static class HashMix
    {
        private const ulong K = 0x9E3779B97F4A7C15UL;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static ulong Add(ulong h, ulong word) => (h ^ word) * K;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static int Fold(ulong h) => (int)h ^ (int)(h >> 32);
    }

    internal interface IVec
    {
        bool IsZero { get; }
        void SubOne();           // subtract 1; caller must ensure !IsZero
        uint Div37();            // divide by 37 in place, return remainder
        void Mul37Add(ulong addend); // multiply by 37 and add addend in place
    }

    // Fields are public on these internal types so Unity's serializer picks them
    // up without [SerializeField]; renaming them breaks serialized assets.

    // ──────────────────────────────────────────────────────────────────────────
    //  Vec64  –  1 word, up to 12 symbols
    //  No UInt128 needed: 37^12 ≈ 6.58×10^18 < 2^64, so all intermediate
    //  values fit in a single ulong.
    // ──────────────────────────────────────────────────────────────────────────
    [Serializable]
    internal struct Vec64 : IVec, IEquatable<Vec64>
    {
        public ulong V0;
        public readonly bool IsZero => V0 == 0;

        internal Vec64(ulong v0) => V0 = v0;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void SubOne() => V0--;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public uint Div37() => (uint)Magic37.DivFirst(ref V0);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Mul37Add(ulong addend) => V0 = V0 * 37u + addend;

        public readonly bool Equals(Vec64 o) => V0 == o.V0;
        public readonly override bool Equals(object? obj) => obj is Vec64 o && Equals(o);
        public readonly override int GetHashCode() => HashMix.Fold(V0);
    }

    // ──────────────────────────────────────────────────────────────────────────
    //  Vec128  –  2 words, up to 24 symbols
    // ──────────────────────────────────────────────────────────────────────────
    [Serializable]
    internal struct Vec128 : IVec, IEquatable<Vec128>
    {
        public ulong V0, V1;
        public readonly bool IsZero => V0 == 0 && V1 == 0;

        internal Vec128(ulong v0, ulong v1) => (V0, V1) = (v0, v1);
        internal Vec128(Vec64 v) => (V0, V1) = (v.V0, 0UL);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void SubOne()
        {
            if (V0 != 0) { V0--; return; }
            V0 = ulong.MaxValue; V1--;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public uint Div37()
        {
            ulong rem = Magic37.DivFirst(ref V1);
            Magic37.DivStep(ref V0, ref rem);
            return (uint)rem;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Mul37Add(ulong addend)
        {
            ulong carry = addend;
            Magic37.MulStep(ref V0, ref carry);
            if (carry == 0) return;
            Magic37.MulStep(ref V1, ref carry);
        }

        public readonly bool Equals(Vec128 o) => V0 == o.V0 && V1 == o.V1;
        public readonly override bool Equals(object? obj) => obj is Vec128 o && Equals(o);
        public readonly override int GetHashCode() =>
            HashMix.Fold(HashMix.Add(HashMix.Add(0, V0), V1));
    }

    // ──────────────────────────────────────────────────────────────────────────
    //  Vec192  –  3 words, up to 36 symbols
    // ──────────────────────────────────────────────────────────────────────────
    [Serializable]
    internal struct Vec192 : IVec, IEquatable<Vec192>
    {
        public ulong V0, V1, V2;
        public readonly bool IsZero => V0 == 0 && V1 == 0 && V2 == 0;

        internal Vec192(ulong v0, ulong v1, ulong v2) => (V0, V1, V2) = (v0, v1, v2);
        internal Vec192(Vec64 v)  => (V0, V1, V2) = (v.V0, 0UL, 0UL);
        internal Vec192(Vec128 v) => (V0, V1, V2) = (v.V0, v.V1, 0UL);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void SubOne()
        {
            if (V0 != 0) { V0--; return; }
            V0 = ulong.MaxValue;
            if (V1 != 0) { V1--; return; }
            V1 = ulong.MaxValue; V2--;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public uint Div37()
        {
            ulong rem = Magic37.DivFirst(ref V2);
            Magic37.DivStep(ref V1, ref rem);
            Magic37.DivStep(ref V0, ref rem);
            return (uint)rem;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Mul37Add(ulong addend)
        {
            ulong carry = addend;
            Magic37.MulStep(ref V0, ref carry);
            if (carry == 0) return;
            Magic37.MulStep(ref V1, ref carry);
            if (carry == 0) return;
            Magic37.MulStep(ref V2, ref carry);
        }

        public readonly bool Equals(Vec192 o) => V0 == o.V0 && V1 == o.V1 && V2 == o.V2;
        public readonly override bool Equals(object? obj) => obj is Vec192 o && Equals(o);
        public readonly override int GetHashCode() =>
            HashMix.Fold(HashMix.Add(HashMix.Add(HashMix.Add(0, V0), V1), V2));
    }

    // ──────────────────────────────────────────────────────────────────────────
    //  Vec256  –  4 words, up to 49 symbols
    // ──────────────────────────────────────────────────────────────────────────
    [Serializable]
    internal struct Vec256 : IVec, IEquatable<Vec256>
    {
        public ulong V0, V1, V2, V3;
        public readonly bool IsZero => V0 == 0 && V1 == 0 && V2 == 0 && V3 == 0;

        internal Vec256(ulong v0, ulong v1, ulong v2, ulong v3) => (V0, V1, V2, V3) = (v0, v1, v2, v3);
        internal Vec256(Vec64 v)  => (V0, V1, V2, V3) = (v.V0, 0UL, 0UL, 0UL);
        internal Vec256(Vec128 v) => (V0, V1, V2, V3) = (v.V0, v.V1, 0UL, 0UL);
        internal Vec256(Vec192 v) => (V0, V1, V2, V3) = (v.V0, v.V1, v.V2, 0UL);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void SubOne()
        {
            if (V0 != 0) { V0--; return; }
            V0 = ulong.MaxValue;
            if (V1 != 0) { V1--; return; }
            V1 = ulong.MaxValue;
            if (V2 != 0) { V2--; return; }
            V2 = ulong.MaxValue; V3--;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public uint Div37()
        {
            ulong rem = Magic37.DivFirst(ref V3);
            Magic37.DivStep(ref V2, ref rem);
            Magic37.DivStep(ref V1, ref rem);
            Magic37.DivStep(ref V0, ref rem);
            return (uint)rem;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Mul37Add(ulong addend)
        {
            ulong carry = addend;
            Magic37.MulStep(ref V0, ref carry);
            if (carry == 0) return;
            Magic37.MulStep(ref V1, ref carry);
            if (carry == 0) return;
            Magic37.MulStep(ref V2, ref carry);
            if (carry == 0) return;
            Magic37.MulStep(ref V3, ref carry);
        }

        public readonly bool Equals(Vec256 o) => V0 == o.V0 && V1 == o.V1 && V2 == o.V2 && V3 == o.V3;
        public readonly override bool Equals(object? obj) => obj is Vec256 o && Equals(o);
        public readonly override int GetHashCode() =>  HashMix.Fold(HashMix.Add(HashMix.Add(HashMix.Add(HashMix.Add(0, V0), V1), V2), V3));
    }
}
