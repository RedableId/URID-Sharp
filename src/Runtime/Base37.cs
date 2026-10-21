using System.Runtime.CompilerServices;

namespace URID
{
	/// <summary>
	/// Symbol alphabet of the bijective base-37 numeral system that every Urid is stored in.
	/// </summary>
	/// <remarks>
	/// <para>
	/// Symbols map to digit values 1–37: <c>a</c>–<c>z</c> → 1–26, <c>0</c>–<c>9</c> → 27–36, separator → 37.
	/// There is no digit 0, which makes the system bijective: every symbol string maps to exactly one number
	/// and back, and the empty string is the number 0. In ordinary base 37, "a" and "0a" would collide.
	/// </para>
	/// <para>
	/// Letters are case-insensitive, and every character that is not an ASCII letter or digit encodes as the separator.
	/// </para>
	/// </remarks>
	internal static class Base37
	{
		private const uint LettersCount = 26;
		private const uint DigitsCount = 10;
		private const uint DigitsFirst = LettersCount + 1;
		private const uint DigitsLast = LettersCount + DigitsCount;

		/// <summary>Digit value of the separator symbol.</summary>
		internal const uint SeparatorCode = DigitsLast + 1;

		/// <summary>Character that the separator symbol decodes to.</summary>
		internal const char Separator = '_';

		/// <summary>Returns <see langword="true"/> for every character that encodes as the separator.</summary>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		internal static bool IsSeparator(char c) => Encode(c) == SeparatorCode;

		/// <summary>Returns the digit value (1–37) of a character.</summary>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		internal static uint Encode(char c)
		{
			// Setting bit 0x20 lowercases an ASCII letter and moves no other character into the a–z range.
			uint u = (uint)((c | 0x20u) - 'a');
			if (u < LettersCount) return u + 1;
			u = (uint)(c - '0');
			if (u < DigitsCount) return u + DigitsFirst;
			return SeparatorCode;
		}

		/// <summary>Returns the character for a digit value (1–37). Letters are always lowercase.</summary>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		internal static char Decode(uint v)
		{
			if (v <= LettersCount) return (char)('a' + v - 1);
			if (v <= DigitsLast) return (char)('0' + v - DigitsFirst);
			return Separator;
		}
	}
}
