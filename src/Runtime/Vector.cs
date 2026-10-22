using System.Runtime.CompilerServices;

namespace URID
{
	/// <summary>
	/// Single-word steps of multi-word multiplication and division by 37.
	/// </summary>
	/// <remarks>
	/// Multi-word values chain these steps through a carry or remainder, so no 128-bit arithmetic is needed
	/// (<c>System.UInt128</c> is unavailable in Unity and Burst). Every division is by the constant 37,
	/// which lets the JIT replace the hardware divide with a multiply and shift.
	/// </remarks>
	internal static class Magic37
	{
		/// <summary><c>floor(2^64 / 37)</c>; the matching remainder <c>2^64 mod 37</c> is 12.</summary>
		internal const ulong Q64 = 498560650640798692UL;

		/// <summary>Divides the most significant word by 37 and returns the remainder to carry into the next word.</summary>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		internal static ulong DivFirst(ref ulong word)
		{
			ulong rem = word % 37u;
			word /= 37u;
			return rem;
		}

		/// <summary>
		/// Divides the next lower word by 37, taking the remainder <paramref name="rem"/> from the word above.
		/// Both arguments are updated.
		/// </summary>
		/// <remarks>
		/// Uses <c>rem · 2^64 + word = rem · Q64 · 37 + (rem · 12 + word)</c>, so the quotient is
		/// <c>rem · Q64 + (rem · 12 + word) / 37</c>. The sum <c>rem · 12 + word</c> can exceed 64 bits;
		/// the overflow is detected and folded back in as <c>+12</c>.
		/// </remarks>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		internal static void DivStep(ref ulong word, ref ulong rem)
		{
			ulong rmul = rem * 12u;
			ulong t = rmul + word;
			ulong ov = t < rmul ? 1u : 0u;
			ulong t2 = t + ov * 12u;
			ulong q = t2 / 37u;
			word = (rem + ov) * Q64 + q;
			rem = t2 - q * 37u;
		}

		/// <summary>
		/// Computes <c>word · 37 + carry</c>: the low 64 bits stay in <paramref name="word"/>,
		/// the overflow goes to <paramref name="carry"/>.
		/// </summary>
		/// <remarks>
		/// Requires <c>carry ≤ 37</c>, which always holds for a symbol code or a carry from a lower word.
		/// The word is multiplied as two 32-bit halves, so every intermediate value stays below <c>2^38</c>.
		/// </remarks>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		internal static void MulStep(ref ulong word, ref ulong carry)
		{
			ulong lo = word & 0xFFFFFFFFUL;
			ulong hi = word >> 32;
			ulong pLo = lo * 37u + carry;
			ulong pHi = hi * 37u;
			ulong mid = pHi + (pLo >> 32);
			word = (mid << 32) | (pLo & 0xFFFFFFFFUL);
			carry = mid >> 32;
		}
	}

	/// <summary>
	/// Deterministic hash mixing for Urid hash codes.
	/// </summary>
	/// <remarks>
	/// <c>System.HashCode</c> is avoided on purpose: its per-process random seed cannot be used
	/// from Burst-compiled code, and Urids are meant to work as keys inside Burst jobs.
	/// </remarks>
	internal static class HashMix
	{
		private const ulong K = 0x9E3779B97F4A7C15UL;

		/// <summary>Mixes one more word into the running hash <paramref name="h"/>.</summary>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		internal static ulong Add(ulong h, ulong word) => (h ^ word) * K;

		/// <summary>Folds a 64-bit hash into an <see cref="int"/> hash code.</summary>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		internal static int Fold(ulong h) => (int)h ^ (int)(h >> 32);
	}

	/// <summary>
	/// Fixed-size unsigned integer that <see cref="UridUtility"/> uses as a mutable working register.
	/// </summary>
	/// <remarks>
	/// A vector never stores an identifier: each Urid keeps its own storage fields and creates a vector copy
	/// when an algorithm has to modify the value step by step. Words are little-endian: <c>V0</c> is the least significant.
	/// </remarks>
	internal interface IVec
	{
		/// <summary>Whether the value is 0, which is the empty identifier.</summary>
		bool IsZero { get; }

		/// <summary>Subtracts 1. The value must not be 0.</summary>
		void SubOne();

		/// <summary>Divides by 37 in place and returns the remainder.</summary>
		uint Div37();

		/// <summary>Multiplies by 37 and adds <paramref name="addend"/> (at most 37) in place.</summary>
		void Mul37Add(ulong addend);
	}

	/// <summary>One-word <see cref="IVec"/>, the working register of <see cref="Urid64"/>.</summary>
	/// <remarks>Plain <see cref="ulong"/> arithmetic is enough: 37^12 &lt; 2^64.</remarks>
	internal struct Vec64 : IVec
	{
		internal ulong V0;

		internal Vec64(ulong v0) => V0 = v0;

		/// <inheritdoc/>
		public readonly bool IsZero => V0 == 0;

		/// <inheritdoc/>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public void SubOne() => V0--;

		/// <inheritdoc/>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public uint Div37() => (uint)Magic37.DivFirst(ref V0);

		/// <inheritdoc/>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public void Mul37Add(ulong addend) => V0 = V0 * 37u + addend;
	}

	/// <summary>Two-word <see cref="IVec"/>, the working register of <see cref="Urid128"/>.</summary>
	internal struct Vec128 : IVec
	{
		internal ulong V0, V1;

		internal Vec128(ulong v0, ulong v1) => (V0, V1) = (v0, v1);

		/// <inheritdoc/>
		public readonly bool IsZero => V0 == 0 && V1 == 0;

		/// <inheritdoc/>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public void SubOne()
		{
			if (V0 != 0) { V0--; return; }
			V0 = ulong.MaxValue;
			V1--;
		}

		/// <inheritdoc/>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public uint Div37()
		{
			ulong rem = Magic37.DivFirst(ref V1);
			Magic37.DivStep(ref V0, ref rem);
			return (uint)rem;
		}

		/// <inheritdoc/>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public void Mul37Add(ulong addend)
		{
			ulong carry = addend;
			Magic37.MulStep(ref V0, ref carry);
			Magic37.MulStep(ref V1, ref carry);
		}
	}

	/// <summary>Three-word <see cref="IVec"/>, the working register of <see cref="Urid192"/>.</summary>
	internal struct Vec192 : IVec
	{
		internal ulong V0, V1, V2;

		internal Vec192(ulong v0, ulong v1, ulong v2) => (V0, V1, V2) = (v0, v1, v2);

		/// <inheritdoc/>
		public readonly bool IsZero => V0 == 0 && V1 == 0 && V2 == 0;

		/// <inheritdoc/>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public void SubOne()
		{
			if (V0 != 0) { V0--; return; }
			V0 = ulong.MaxValue;
			if (V1 != 0) { V1--; return; }
			V1 = ulong.MaxValue;
			V2--;
		}

		/// <inheritdoc/>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public uint Div37()
		{
			ulong rem = Magic37.DivFirst(ref V2);
			Magic37.DivStep(ref V1, ref rem);
			Magic37.DivStep(ref V0, ref rem);
			return (uint)rem;
		}

		/// <inheritdoc/>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public void Mul37Add(ulong addend)
		{
			ulong carry = addend;
			Magic37.MulStep(ref V0, ref carry);
			Magic37.MulStep(ref V1, ref carry);
			Magic37.MulStep(ref V2, ref carry);
		}
	}

	/// <summary>Four-word <see cref="IVec"/>, the working register of <see cref="Urid256"/> and <see cref="UridBuilder"/>.</summary>
	internal struct Vec256 : IVec
	{
		internal ulong V0, V1, V2, V3;

		internal Vec256(ulong v0, ulong v1, ulong v2, ulong v3) => (V0, V1, V2, V3) = (v0, v1, v2, v3);

		/// <inheritdoc/>
		public readonly bool IsZero => V0 == 0 && V1 == 0 && V2 == 0 && V3 == 0;

		/// <inheritdoc/>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public void SubOne()
		{
			if (V0 != 0) { V0--; return; }
			V0 = ulong.MaxValue;
			if (V1 != 0) { V1--; return; }
			V1 = ulong.MaxValue;
			if (V2 != 0) { V2--; return; }
			V2 = ulong.MaxValue;
			V3--;
		}

		/// <inheritdoc/>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public uint Div37()
		{
			ulong rem = Magic37.DivFirst(ref V3);
			Magic37.DivStep(ref V2, ref rem);
			Magic37.DivStep(ref V1, ref rem);
			Magic37.DivStep(ref V0, ref rem);
			return (uint)rem;
		}

		/// <inheritdoc/>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public void Mul37Add(ulong addend)
		{
			ulong carry = addend;
			Magic37.MulStep(ref V0, ref carry);
			Magic37.MulStep(ref V1, ref carry);
			Magic37.MulStep(ref V2, ref carry);
			Magic37.MulStep(ref V3, ref carry);
		}
	}
}
