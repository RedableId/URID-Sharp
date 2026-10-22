using System;

namespace URID
{
	/// <summary>
	/// Identifier of up to 12 symbols packed into 64 bits.
	/// </summary>
	/// <remarks>
	/// <para>
	/// A Urid stores a short name made of lowercase letters, digits and the separator <c>_</c> as a number
	/// in bijective base 37. It is compared, hashed and copied like an integer and allocates nothing until
	/// it is converted to a <see cref="string"/>.
	/// </para>
	/// <para>
	/// Parsing is lossy by design: letters are lowercased, every character that is not an ASCII letter or digit
	/// becomes <c>_</c>, and characters beyond <c>MaxLength</c> are dropped. <see langword="default"/> is the empty identifier.
	/// A wider Urid accepts a narrower one through an implicit conversion that keeps the identifier unchanged.
	/// </para>
	/// <para>
	/// Each Urid is declared in parts: this file holds all members, while the storage fields come from
	/// <c>Urid.Sharp.cs</c> (a readonly struct, plain .NET) or <c>Urid.Unity.cs</c> (a serializable struct, Unity).
	/// All members are marked <see langword="readonly"/>, so both variants behave the same and need no defensive copies.
	/// </para>
	/// </remarks>
	public partial struct Urid64 : IEquatable<Urid64>
	{
		/// <summary>Maximum number of symbols. Longer input is truncated.</summary>
		public const int MaxLength = 12;

		/// <summary>Creates an identifier from its raw words, least significant first.</summary>
		internal Urid64(ulong v0) => this.v0 = v0;

		/// <summary>Creates an identifier from a working register.</summary>
		internal Urid64(Vec64 v) : this(v.V0) { }

		/// <summary>Parses the first <see cref="MaxLength"/> characters of <paramref name="src"/>.</summary>
		public Urid64(ReadOnlySpan<char> src) : this(UridUtility.Parse<Vec64>(src, MaxLength)) { }

		/// <summary>Copy of the value as a mutable working register for <see cref="UridUtility"/>.</summary>
		internal readonly Vec64 Vec => new Vec64(v0);

		/// <summary>Number of symbols.</summary>
		/// <remarks>Not stored: it is computed by decoding the value, so the cost grows with the length.</remarks>
		public readonly int Length => Vec.GetLength();

		/// <summary>
		/// Returns the symbol at <paramref name="index"/>, or <c>'\0'</c> when <paramref name="index"/> is out of range.
		/// </summary>
		/// <remarks>Decodes the value on every call; to read all symbols at once, use <see cref="WriteChars"/>.</remarks>
		public readonly char this[int index] => Vec.GetChar(index);

		/// <summary>Writes the symbols into <paramref name="dst"/> and returns how many were written.</summary>
		/// <remarks>A buffer of <see cref="MaxLength"/> characters is always large enough.</remarks>
		public readonly int WriteChars(Span<char> dst) => Vec.WriteChars(dst);

		/// <summary>Returns the identifier in snake_case, the same as <c>ToString(singleCase: true, upperCase: false)</c>.</summary>
		public readonly override string ToString() => Vec.Format(singleCase: true, upperCase: false);

		/// <summary>
		/// Formats the identifier so that, for the same arguments, two results are equal (ordinal comparison)
		/// if and only if the identifiers are equal.
		/// </summary>
		/// <param name="singleCase">
		/// <para>
		/// <see langword="true"/>: snake_case or SCREAM_CASE, every separator written as <paramref name="separator"/>.
		/// </para>
		/// <para>
		/// <see langword="false"/>: camelCase or PascalCase. A separator directly followed by a letter is dropped and
		/// the letter is capitalized; every other separator (before a digit, before another separator or at the end)
		/// is written as <paramref name="separator"/>. A capital letter thus stands for exactly one separator:
		/// <c>a__b</c> becomes <c>a_B</c>. In PascalCase, leading separators are written as is and the first letter
		/// after them is capitalized: <c>__init__</c> becomes <c>__Init__</c> (camelCase: <c>_Init__</c>).
		/// </para>
		/// </param>
		/// <param name="upperCase">SCREAM_CASE or PascalCase instead of snake_case or camelCase.</param>
		/// <param name="separator">Character written for a separator.</param>
		/// <exception cref="ArgumentException"><paramref name="separator"/> is an ASCII letter or digit.</exception>
		public readonly string ToString(bool singleCase, bool upperCase, char separator = Base37.Separator) => Vec.Format(singleCase, upperCase, separator);

		/// <summary>Splits the identifier into the tokens of its top level; see <see cref="UridSplitEnumerator"/>.</summary>
		/// <param name="options"><see cref="StringSplitOptions.RemoveEmptyEntries"/> skips empty tokens; other options are ignored.</param>
		public readonly UridSplitEnumerator Split(StringSplitOptions options = StringSplitOptions.None) => UridSplitEnumerator.Create(Vec, options);

		/// <inheritdoc/>
		public readonly bool Equals(Urid64 other) => v0 == other.v0;

		/// <inheritdoc/>
		public readonly override bool Equals(object? obj) => obj is Urid64 o && Equals(o);

		/// <inheritdoc/>
		public readonly override int GetHashCode() => HashMix.Fold(v0);

		/// <summary>Whether two identifiers are equal.</summary>
		public static bool operator ==(Urid64 a, Urid64 b) => a.Equals(b);

		/// <summary>Whether two identifiers differ.</summary>
		public static bool operator !=(Urid64 a, Urid64 b) => !a.Equals(b);

		/// <summary>Parses <paramref name="s"/> like the <see cref="Urid64(ReadOnlySpan{char})"/> constructor.</summary>
		public static implicit operator Urid64(string s) => new Urid64(s.AsSpan());

		/// <summary>Narrows the identifier, keeping as many leading symbols as fit.</summary>
		public static explicit operator Urid64(Urid128 src) => new Urid64(src.Vec.KeepFirst(MaxLength).V0);

		/// <inheritdoc cref="explicit operator Urid64(Urid128)"/>
		public static explicit operator Urid64(Urid192 src) => new Urid64(src.Vec.KeepFirst(MaxLength).V0);

		/// <inheritdoc cref="explicit operator Urid64(Urid128)"/>
		public static explicit operator Urid64(Urid256 src) => new Urid64(src.Vec.KeepFirst(MaxLength).V0);
	}

	/// <summary>
	/// Identifier of up to 24 symbols packed into 128 bits.
	/// </summary>
	/// <inheritdoc cref="Urid64" path="/remarks"/>
	public partial struct Urid128 : IEquatable<Urid128>
	{
		/// <inheritdoc cref="Urid64.MaxLength"/>
		public const int MaxLength = 24;

		/// <inheritdoc cref="Urid64.Urid64(ulong)"/>
		internal Urid128(ulong v0, ulong v1) => (this.v0, this.v1) = (v0, v1);

		/// <inheritdoc cref="Urid64.Urid64(Vec64)"/>
		internal Urid128(Vec128 v) : this(v.V0, v.V1) { }

		/// <inheritdoc cref="Urid64.Urid64(ReadOnlySpan{char})"/>
		public Urid128(ReadOnlySpan<char> src) : this(UridUtility.Parse<Vec128>(src, MaxLength)) { }

		/// <inheritdoc cref="Urid64.Vec"/>
		internal readonly Vec128 Vec => new Vec128(v0, v1);

		/// <inheritdoc cref="Urid64.Length"/>
		public readonly int Length => Vec.GetLength();

		/// <inheritdoc cref="Urid64.this[int]"/>
		public readonly char this[int index] => Vec.GetChar(index);

		/// <inheritdoc cref="Urid64.WriteChars"/>
		public readonly int WriteChars(Span<char> dst) => Vec.WriteChars(dst);

		/// <inheritdoc cref="Urid64.ToString()"/>
		public readonly override string ToString() => Vec.Format(singleCase: true, upperCase: false);

		/// <inheritdoc cref="Urid64.ToString(bool, bool, char)"/>
		public readonly string ToString(bool singleCase, bool upperCase, char separator = Base37.Separator) => Vec.Format(singleCase, upperCase, separator);

		/// <inheritdoc cref="Urid64.Split"/>
		public readonly UridSplitEnumerator Split(StringSplitOptions options = StringSplitOptions.None) => UridSplitEnumerator.Create(Vec, options);

		/// <inheritdoc/>
		public readonly bool Equals(Urid128 other) => v0 == other.v0 && v1 == other.v1;

		/// <inheritdoc/>
		public readonly override bool Equals(object? obj) => obj is Urid128 o && Equals(o);

		/// <inheritdoc/>
		public readonly override int GetHashCode() => HashMix.Fold(HashMix.Add(HashMix.Add(0, v0), v1));

		/// <inheritdoc cref="Urid64.operator ==(Urid64, Urid64)"/>
		public static bool operator ==(Urid128 a, Urid128 b) => a.Equals(b);

		/// <inheritdoc cref="Urid64.operator !=(Urid64, Urid64)"/>
		public static bool operator !=(Urid128 a, Urid128 b) => !a.Equals(b);

		/// <inheritdoc cref="Urid64.implicit operator Urid64(string)"/>
		public static implicit operator Urid128(string s) => new Urid128(s.AsSpan());

		/// <summary>Widens the identifier without changing it.</summary>
		public static implicit operator Urid128(Urid64 src) => new Urid128(src.Vec.V0, 0UL);

		/// <inheritdoc cref="Urid64.explicit operator Urid64(Urid128)"/>
		public static explicit operator Urid128(Urid192 src)
		{
			var v = src.Vec.KeepFirst(MaxLength);
			return new Urid128(v.V0, v.V1);
		}

		/// <inheritdoc cref="Urid64.explicit operator Urid64(Urid128)"/>
		public static explicit operator Urid128(Urid256 src)
		{
			var v = src.Vec.KeepFirst(MaxLength);
			return new Urid128(v.V0, v.V1);
		}
	}

	/// <summary>
	/// Identifier of up to 36 symbols packed into 192 bits.
	/// </summary>
	/// <inheritdoc cref="Urid64" path="/remarks"/>
	public partial struct Urid192 : IEquatable<Urid192>
	{
		/// <inheritdoc cref="Urid64.MaxLength"/>
		public const int MaxLength = 36;

		/// <inheritdoc cref="Urid64.Urid64(ulong)"/>
		internal Urid192(ulong v0, ulong v1, ulong v2) => (this.v0, this.v1, this.v2) = (v0, v1, v2);

		/// <inheritdoc cref="Urid64.Urid64(Vec64)"/>
		internal Urid192(Vec192 v) : this(v.V0, v.V1, v.V2) { }

		/// <inheritdoc cref="Urid64.Urid64(ReadOnlySpan{char})"/>
		public Urid192(ReadOnlySpan<char> src) : this(UridUtility.Parse<Vec192>(src, MaxLength)) { }

		/// <inheritdoc cref="Urid64.Vec"/>
		internal readonly Vec192 Vec => new Vec192(v0, v1, v2);

		/// <inheritdoc cref="Urid64.Length"/>
		public readonly int Length => Vec.GetLength();

		/// <inheritdoc cref="Urid64.this[int]"/>
		public readonly char this[int index] => Vec.GetChar(index);

		/// <inheritdoc cref="Urid64.WriteChars"/>
		public readonly int WriteChars(Span<char> dst) => Vec.WriteChars(dst);

		/// <inheritdoc cref="Urid64.ToString()"/>
		public readonly override string ToString() => Vec.Format(singleCase: true, upperCase: false);

		/// <inheritdoc cref="Urid64.ToString(bool, bool, char)"/>
		public readonly string ToString(bool singleCase, bool upperCase, char separator = Base37.Separator) => Vec.Format(singleCase, upperCase, separator);

		/// <inheritdoc cref="Urid64.Split"/>
		public readonly UridSplitEnumerator Split(StringSplitOptions options = StringSplitOptions.None) => UridSplitEnumerator.Create(Vec, options);

		/// <inheritdoc/>
		public readonly bool Equals(Urid192 other) => v0 == other.v0 && v1 == other.v1 && v2 == other.v2;

		/// <inheritdoc/>
		public readonly override bool Equals(object? obj) => obj is Urid192 o && Equals(o);

		/// <inheritdoc/>
		public readonly override int GetHashCode() => HashMix.Fold(HashMix.Add(HashMix.Add(HashMix.Add(0, v0), v1), v2));

		/// <inheritdoc cref="Urid64.operator ==(Urid64, Urid64)"/>
		public static bool operator ==(Urid192 a, Urid192 b) => a.Equals(b);

		/// <inheritdoc cref="Urid64.operator !=(Urid64, Urid64)"/>
		public static bool operator !=(Urid192 a, Urid192 b) => !a.Equals(b);

		/// <inheritdoc cref="Urid64.implicit operator Urid64(string)"/>
		public static implicit operator Urid192(string s) => new Urid192(s.AsSpan());

		/// <inheritdoc cref="Urid128.implicit operator Urid128(Urid64)"/>
		public static implicit operator Urid192(Urid64 src) => new Urid192(src.Vec.V0, 0UL, 0UL);

		/// <inheritdoc cref="Urid128.implicit operator Urid128(Urid64)"/>
		public static implicit operator Urid192(Urid128 src)
		{
			var v = src.Vec;
			return new Urid192(v.V0, v.V1, 0UL);
		}

		/// <inheritdoc cref="Urid64.explicit operator Urid64(Urid128)"/>
		public static explicit operator Urid192(Urid256 src)
		{
			var v = src.Vec.KeepFirst(MaxLength);
			return new Urid192(v.V0, v.V1, v.V2);
		}
	}

	/// <summary>
	/// Identifier of up to 49 symbols packed into 256 bits.
	/// </summary>
	/// <inheritdoc cref="Urid64" path="/remarks"/>
	public partial struct Urid256 : IEquatable<Urid256>
	{
		/// <inheritdoc cref="Urid64.MaxLength"/>
		public const int MaxLength = 49;

		/// <inheritdoc cref="Urid64.Urid64(ulong)"/>
		internal Urid256(ulong v0, ulong v1, ulong v2, ulong v3) => (this.v0, this.v1, this.v2, this.v3) = (v0, v1, v2, v3);

		/// <inheritdoc cref="Urid64.Urid64(Vec64)"/>
		internal Urid256(Vec256 v) : this(v.V0, v.V1, v.V2, v.V3) { }

		/// <inheritdoc cref="Urid64.Urid64(ReadOnlySpan{char})"/>
		public Urid256(ReadOnlySpan<char> src) : this(UridUtility.Parse<Vec256>(src, MaxLength)) { }

		/// <inheritdoc cref="Urid64.Vec"/>
		internal readonly Vec256 Vec => new Vec256(v0, v1, v2, v3);

		/// <inheritdoc cref="Urid64.Length"/>
		public readonly int Length => Vec.GetLength();

		/// <inheritdoc cref="Urid64.this[int]"/>
		public readonly char this[int index] => Vec.GetChar(index);

		/// <inheritdoc cref="Urid64.WriteChars"/>
		public readonly int WriteChars(Span<char> dst) => Vec.WriteChars(dst);

		/// <inheritdoc cref="Urid64.ToString()"/>
		public readonly override string ToString() => Vec.Format(singleCase: true, upperCase: false);

		/// <inheritdoc cref="Urid64.ToString(bool, bool, char)"/>
		public readonly string ToString(bool singleCase, bool upperCase, char separator = Base37.Separator) => Vec.Format(singleCase, upperCase, separator);

		/// <inheritdoc cref="Urid64.Split"/>
		public readonly UridSplitEnumerator Split(StringSplitOptions options = StringSplitOptions.None) => UridSplitEnumerator.Create(Vec, options);

		/// <inheritdoc/>
		public readonly bool Equals(Urid256 other) => v0 == other.v0 && v1 == other.v1 && v2 == other.v2 && v3 == other.v3;

		/// <inheritdoc/>
		public readonly override bool Equals(object? obj) => obj is Urid256 o && Equals(o);

		/// <inheritdoc/>
		public readonly override int GetHashCode() => HashMix.Fold(HashMix.Add(HashMix.Add(HashMix.Add(HashMix.Add(0, v0), v1), v2), v3));

		/// <inheritdoc cref="Urid64.operator ==(Urid64, Urid64)"/>
		public static bool operator ==(Urid256 a, Urid256 b) => a.Equals(b);

		/// <inheritdoc cref="Urid64.operator !=(Urid64, Urid64)"/>
		public static bool operator !=(Urid256 a, Urid256 b) => !a.Equals(b);

		/// <inheritdoc cref="Urid64.implicit operator Urid64(string)"/>
		public static implicit operator Urid256(string s) => new Urid256(s.AsSpan());

		/// <inheritdoc cref="Urid128.implicit operator Urid128(Urid64)"/>
		public static implicit operator Urid256(Urid64 src) => new Urid256(src.Vec.V0, 0UL, 0UL, 0UL);

		/// <inheritdoc cref="Urid128.implicit operator Urid128(Urid64)"/>
		public static implicit operator Urid256(Urid128 src)
		{
			var v = src.Vec;
			return new Urid256(v.V0, v.V1, 0UL, 0UL);
		}

		/// <inheritdoc cref="Urid128.implicit operator Urid128(Urid64)"/>
		public static implicit operator Urid256(Urid192 src)
		{
			var v = src.Vec;
			return new Urid256(v.V0, v.V1, v.V2, 0UL);
		}
	}
}
