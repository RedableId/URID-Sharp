using System;
using System.Runtime.CompilerServices;

namespace URID
{
	/// <summary>
	/// Builds an identifier symbol by symbol without allocating, for example from a prefix, a separator and a number.
	/// </summary>
	/// <remarks>
	/// <para>
	/// Holds up to <see cref="Urid256.MaxLength"/> symbols; further appends are ignored.
	/// Each <c>ToXxx</c> method keeps only as many leading symbols as the target type can hold.
	/// </para>
	/// <para>
	/// Converting is cheap while the symbols fit the target: the low words are copied as is.
	/// Only truncation decodes the symbols and encodes the kept prefix again.
	/// </para>
	/// </remarks>
	public ref struct UridBuilder
	{
		private Vec256 _v;
		private int _length;

		/// <summary>Number of symbols appended so far.</summary>
		public int Length => _length;

		/// <summary>Whether the builder holds <see cref="Urid256.MaxLength"/> symbols and ignores further appends.</summary>
		public bool IsFull => _length >= Urid256.MaxLength;

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private void AppendCode(uint code)
		{
			if (_length < Urid256.MaxLength)
			{
				_v.Mul37Add(code);
				_length++;
			}
		}

		/// <summary>Appends one symbol, mapped the same way as when parsing a <see cref="Urid64"/>.</summary>
		public void Append(char symbol) => AppendCode(Base37.Encode(symbol));

		/// <summary>Appends every character of <paramref name="token"/> as a symbol.</summary>
		public void Append(ReadOnlySpan<char> token)
		{
			int lim = Math.Min(token.Length, Urid256.MaxLength - _length);
			for (int i = 0; i < lim; i++)
				AppendCode(Base37.Encode(token[i]));
		}

		/// <summary>Appends the decimal digits of <paramref name="number"/>.</summary>
		/// <remarks>Negative numbers are appended without the minus sign.</remarks>
		public void Append(int number)
		{
			uint n = number >= 0 ? (uint)number : (uint)-number;
			if (n == 0)
			{
				AppendCode(Base37.Encode('0'));
				return;
			}

			Span<char> buf = stackalloc char[10];
			int len = 0;
			while (n > 0)
			{
				buf[len++] = (char)('0' + n % 10);
				n /= 10;
			}

			// Not via Append(ReadOnlySpan<char>): C# 9 has no `scoped` parameters, so a stack-allocated
			// span cannot be passed to it from inside this ref struct.
			for (int i = len - 1; i >= 0; i--)
				AppendCode(Base37.Encode(buf[i]));
		}

		/// <summary>Appends the separator symbol.</summary>
		public void AppendSeparator() => AppendCode(Base37.SeparatorCode);

		/// <summary>Returns the first <see cref="Urid64.MaxLength"/> symbols as a <see cref="Urid64"/>.</summary>
		public Urid64 To64()
		{
			const int cap = Urid64.MaxLength;
			if (_length <= cap) return new Urid64(_v.V0);
			Span<char> buf = stackalloc char[Urid256.MaxLength];
			_v.WriteChars(buf);
			return new Urid64(buf.Slice(0, cap));
		}

		/// <summary>Returns the first <see cref="Urid128.MaxLength"/> symbols as a <see cref="Urid128"/>.</summary>
		public Urid128 To128()
		{
			const int cap = Urid128.MaxLength;
			if (_length <= cap) return new Urid128(_v.V0, _v.V1);
			Span<char> buf = stackalloc char[Urid256.MaxLength];
			_v.WriteChars(buf);
			return new Urid128(buf.Slice(0, cap));
		}

		/// <summary>Returns the first <see cref="Urid192.MaxLength"/> symbols as a <see cref="Urid192"/>.</summary>
		public Urid192 To192()
		{
			const int cap = Urid192.MaxLength;
			if (_length <= cap) return new Urid192(_v.V0, _v.V1, _v.V2);
			Span<char> buf = stackalloc char[Urid256.MaxLength];
			_v.WriteChars(buf);
			return new Urid192(buf.Slice(0, cap));
		}

		/// <summary>Returns all symbols as a <see cref="Urid256"/>, which always has room for them.</summary>
		public Urid256 To256() => new Urid256(_v);
	}
}
