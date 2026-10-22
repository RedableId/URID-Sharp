using System;
using System.Runtime.CompilerServices;

namespace URID
{
	/// <summary>
	/// Shared implementation of every Urid operation, written once as generic extension methods over <see cref="IVec"/>.
	/// </summary>
	/// <remarks>
	/// <para>
	/// Each Urid type only forwards to these methods with its own vector type. The JIT and Burst compile a separate
	/// copy per concrete vector, so the interface calls become direct calls without boxing.
	/// </para>
	/// <para>
	/// The vector is taken by value on purpose: the algorithms consume it as a mutable working copy and never change
	/// the caller's value. After inlining, that copy lives in registers.
	/// </para>
	/// </remarks>
	internal static class UridUtility
	{
		/// <summary>Encodes the first <paramref name="maxLength"/> characters of <paramref name="src"/>.</summary>
		internal static TVec Parse<TVec>(ReadOnlySpan<char> src, int maxLength)
			where TVec : struct, IVec
		{
			TVec v = default;
			int limit = src.Length < maxLength ? src.Length : maxLength;
			for (int i = 0; i < limit; i++)
				v.Mul37Add(Base37.Encode(src[i]));
			return v;
		}

		/// <inheritdoc cref="Urid64.Length"/>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		internal static int GetLength<TVec>(this TVec v)
			where TVec : struct, IVec
		{
			int len = 0;
			while (!v.IsZero)
			{
				v.SubOne();
				v.Div37();
				len++;
			}
			return len;
		}

		/// <inheritdoc cref="Urid64.WriteChars"/>
		internal static int WriteChars<TVec>(this TVec v, Span<char> dst)
			where TVec : struct, IVec
		{
			int len = 0;
			while (!v.IsZero)
			{
				v.SubOne();
				dst[len++] = Base37.Decode(v.Div37() + 1);
			}
			// Division yields the last symbol first.
			dst.Slice(0, len).Reverse();
			return len;
		}

		/// <inheritdoc cref="Urid64.this[int]"/>
		internal static char GetChar<TVec>(this TVec v, int index)
			where TVec : struct, IVec
		{
			int len = v.GetLength();
			if ((uint)index >= (uint)len) return '\0';

			// Division yields the last symbol first: drop the symbols after index, then decode one.
			for (int skip = len - 1 - index; skip > 0; skip--)
			{
				v.SubOne();
				v.Div37();
			}
			v.SubOne();
			return Base37.Decode(v.Div37() + 1);
		}

		/// <summary>Removes the last <paramref name="count"/> symbols.</summary>
		/// <remarks>Dropping one symbol of a bijective numeral is <c>(v − 1) / 37</c>; no symbol is decoded.</remarks>
		internal static TVec DropLast<TVec>(this TVec v, int count)
			where TVec : struct, IVec
		{
			for (int i = 0; i < count && !v.IsZero; i++)
			{
				v.SubOne();
				v.Div37();
			}
			return v;
		}

		/// <summary>Keeps the first <paramref name="count"/> symbols.</summary>
		internal static TVec KeepFirst<TVec>(this TVec v, int count)
			where TVec : struct, IVec
			=> v.DropLast(v.GetLength() - count);

		/// <inheritdoc cref="Urid64.ToString(bool, bool, char)"/>
		internal static string Format<TVec>(this TVec v, bool singleCase, bool upperCase, char separator = Base37.Separator)
			where TVec : struct, IVec
		{
			if (!Base37.IsSeparator(separator))
				throw new ArgumentException("Separator must not be an ASCII letter or digit.", nameof(separator));

			Span<char> buf = stackalloc char[Urid256.MaxLength];
			int len = v.WriteChars(buf);
			if (len == 0) return string.Empty;

			if (singleCase)
				FormatSingleCase(buf.Slice(0, len), upperCase, separator);
			else
				len = FormatMixedCase(buf.Slice(0, len), upperCase, separator);

			return new string(buf.Slice(0, len));
		}

		/// <summary>Rewrites decoded symbols in place as snake_case or SCREAM_CASE.</summary>
		private static void FormatSingleCase(Span<char> buf, bool upperCase, char separator)
		{
			for (int i = 0; i < buf.Length; i++)
			{
				char c = buf[i];
				if (c == Base37.Separator)
					buf[i] = separator;
				else if (upperCase && IsLower(c))
					buf[i] = ToUpper(c);
			}
		}

		/// <summary>Rewrites decoded symbols in place as camelCase or PascalCase.</summary>
		/// <returns>The new length, which is shorter by the number of separators absorbed into capital letters.</returns>
		private static int FormatMixedCase(Span<char> buf, bool pascalCase, char separator)
		{
			int w = 0;
			int r = 0;
			int len = buf.Length;

			if (pascalCase)
			{
				while (r < len && buf[r] == Base37.Separator)
				{
					buf[w++] = separator;
					r++;
				}
				if (r < len && IsLower(buf[r]))
					buf[w++] = ToUpper(buf[r++]);
			}

			for (; r < len; r++)
			{
				char c = buf[r];
				if (c == Base37.Separator && r + 1 < len && IsLower(buf[r + 1]))
					buf[w++] = ToUpper(buf[++r]);
				else
					buf[w++] = c == Base37.Separator ? separator : c;
			}
			return w;
		}

		/// <summary>Letter test for decoded symbols, which are only lowercase letters, digits and the separator.</summary>
		private static bool IsLower(char c) => (uint)(c - 'a') < 26u;

		/// <summary>Uppercases a decoded lowercase letter.</summary>
		private static char ToUpper(char c) => (char)(c - ('a' - 'A'));
	}
}
