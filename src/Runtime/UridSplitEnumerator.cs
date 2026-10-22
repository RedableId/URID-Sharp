using System;

namespace URID
{
	/// <summary>
	/// Enumerates the tokens of one level of a Urid without allocating. Returned by <c>Split</c> of every Urid type.
	/// </summary>
	/// <remarks>
	/// <para>
	/// The level is set by the longest run of separators: the identifier is split at every run of exactly that length,
	/// so shorter runs stay inside the tokens. <c>abc_def__xyz</c> splits into <c>abc_def</c> and <c>xyz</c>, and splitting
	/// <c>abc_def</c> again gives the next level, <c>abc</c> and <c>def</c>. An identifier without separators splits into
	/// single symbols.
	/// </para>
	/// <para>
	/// Empty tokens are kept unless <see cref="StringSplitOptions.RemoveEmptyEntries"/> is given: <c>__abc</c> splits into
	/// an empty token and <c>abc</c>, and the empty identifier gives one empty token. With empty tokens kept, joining the
	/// tokens with the splitting run restores the identifier. Other options have no meaning for a Urid and are ignored.
	/// </para>
	/// <para>
	/// Tokens are returned as <see cref="Urid256"/>, which holds a token of any Urid. Declare the loop variable with the
	/// size you need: <c>foreach (Urid64 token in path.Split())</c> applies the explicit conversion, which keeps as many
	/// leading symbols as fit.
	/// </para>
	/// <para>
	/// The struct is its own enumerable: <see cref="GetEnumerator"/> returns a copy, so the same split can be enumerated
	/// more than once.
	/// </para>
	/// </remarks>
	public unsafe struct UridSplitEnumerator
	{
		/// <summary>Digit values (1–37) of the symbols, first symbol first.</summary>
		private fixed byte codes[Urid256.MaxLength];

		private int length;

		/// <summary>Length of the separator runs that split the identifier; 0 splits into single symbols.</summary>
		private int level;

		private bool removeEmpty;

		/// <summary>Start of the next token; past <see cref="length"/> once the last token was returned.</summary>
		private int position;

		private Urid256 current;

		/// <summary>The token at the current position.</summary>
		public Urid256 Current => current;

		/// <summary>Returns a copy positioned before the first token.</summary>
		public UridSplitEnumerator GetEnumerator() => this;

		/// <summary>Advances to the next token.</summary>
		public bool MoveNext()
		{
			while (position <= length)
			{
				int start = position;
				int end;
				if (level == 0)
				{
					end = Math.Min(start + 1, length);
					position = end < length ? end : length + 1;
				}
				else
				{
					end = FindRun(start);
					position = end < length ? end + level : length + 1;
				}

				if (removeEmpty && start == end) continue;

				current = Build(start, end);
				return true;
			}
			return false;
		}

		/// <summary>Decodes <paramref name="v"/> into a new enumerator.</summary>
		internal static UridSplitEnumerator Create<TVec>(TVec v, StringSplitOptions options)
			where TVec : struct, IVec
		{
			var e = default(UridSplitEnumerator);

			int n = 0;
			while (!v.IsZero)
			{
				v.SubOne();
				e.codes[n++] = (byte)(v.Div37() + 1);
			}

			// Division yields the last symbol first.
			for (int i = 0, j = n - 1; i < j; i++, j--)
			{
				byte t = e.codes[i];
				e.codes[i] = e.codes[j];
				e.codes[j] = t;
			}

			int run = 0;
			int longest = 0;
			for (int i = 0; i < n; i++)
			{
				run = e.codes[i] == Base37.SeparatorCode ? run + 1 : 0;
				if (run > longest) longest = run;
			}

			e.length = n;
			e.level = longest;
			e.removeEmpty = (options & StringSplitOptions.RemoveEmptyEntries) != 0;
			return e;
		}

		/// <summary>Returns the start of the next splitting run at or after <paramref name="start"/>, or the length if there is none.</summary>
		/// <remarks>No run is longer than <see cref="level"/>, so the first <see cref="level"/> separators in a row are a whole run.</remarks>
		private int FindRun(int start)
		{
			int run = 0;
			for (int i = start; i < length; i++)
			{
				run = codes[i] == Base37.SeparatorCode ? run + 1 : 0;
				if (run == level) return i - level + 1;
			}
			return length;
		}

		/// <summary>Encodes the symbols from <paramref name="start"/> up to <paramref name="end"/>.</summary>
		private Urid256 Build(int start, int end)
		{
			var v = default(Vec256);
			for (int i = start; i < end; i++)
				v.Mul37Add(codes[i]);
			return new Urid256(v);
		}
	}
}
