using System;
using System.Collections.Generic;
using NUnit.Framework;

namespace URID.Tests
{
	[TestFixture]
	public class SplitTests
	{
		private static string[] Tokens(Urid128 id, StringSplitOptions options = StringSplitOptions.None)
		{
			var result = new List<string>();
			foreach (var token in id.Split(options))
				result.Add(token.ToString());
			return result.ToArray();
		}

		[TestCase("abc", ExpectedResult = new[] { "a", "b", "c" })]
		[TestCase("abc_def", ExpectedResult = new[] { "abc", "def" })]
		[TestCase("abc_def__xyz", ExpectedResult = new[] { "abc_def", "xyz" })]
		[TestCase("a__b___c", ExpectedResult = new[] { "a__b", "c" })]
		[TestCase("a1_b2_c3", ExpectedResult = new[] { "a1", "b2", "c3" })]
		public string[] SplitsAtLongestRun(string id) => Tokens(id);

		[TestCase("", ExpectedResult = new[] { "" })]
		[TestCase("__abc", ExpectedResult = new[] { "", "abc" })]
		[TestCase("abc__", ExpectedResult = new[] { "abc", "" })]
		[TestCase("_", ExpectedResult = new[] { "", "" })]
		[TestCase("___", ExpectedResult = new[] { "", "" })]
		[TestCase("_a_", ExpectedResult = new[] { "", "a", "" })]
		public string[] KeepsEmptyTokens(string id) => Tokens(id);

		[TestCase("", ExpectedResult = new string[0])]
		[TestCase("__abc", ExpectedResult = new[] { "abc" })]
		[TestCase("_a_b_", ExpectedResult = new[] { "a", "b" })]
		[TestCase("___", ExpectedResult = new string[0])]
		public string[] RemovesEmptyTokens(string id) => Tokens(id, StringSplitOptions.RemoveEmptyEntries);

		[Test]
		public void EmptyUrid64GivesOneEmptyToken()
		{
			int count = 0;
			foreach (var token in new Urid64("").Split())
			{
				Assert.That(token == default(Urid256), Is.True);
				count++;
			}
			Assert.That(count, Is.EqualTo(1));
		}

		[Test]
		public void TypedLoopVariableKeepsLeadingSymbols()
		{
			var tokens = new List<string>();
			foreach (Urid64 token in new Urid256("abcdefghijklmnop_x").Split())
				tokens.Add(token.ToString());
			Assert.That(tokens, Is.EqualTo(new[] { "abcdefghijkl", "x" }));
		}

		[Test]
		public void TokensSplitIntoTheNextLevel()
		{
			var levels = new List<string>();
			foreach (var outer in new Urid128("ab_cd__ef").Split())
			{
				var inner = new List<string>();
				foreach (var token in outer.Split())
					inner.Add(token.ToString());
				levels.Add(string.Join(",", inner));
			}
			Assert.That(levels, Is.EqualTo(new[] { "ab,cd", "e,f" }));
		}

		[Test]
		public void CanBeEnumeratedTwice()
		{
			var split = new Urid64("a_b").Split();
			int first = 0, second = 0;
			foreach (var _ in split) first++;
			foreach (var _ in split) second++;
			Assert.That(first, Is.EqualTo(2));
			Assert.That(second, Is.EqualTo(2));
		}

		/// <summary>
		/// Every string over <c>{a, b, _}</c> up to length 8 must be restored by joining its tokens with the splitting run.
		/// </summary>
		[Test]
		public void JoiningTokensRestoresTheIdentifier()
		{
			const string alphabet = "ab_";
			var stack = new Stack<string>();
			stack.Push("");
			while (stack.Count > 0)
			{
				string s = stack.Pop();

				int run = 0, longest = 0;
				foreach (char c in s)
				{
					run = c == '_' ? run + 1 : 0;
					longest = Math.Max(longest, run);
				}

				var tokens = new List<string>();
				foreach (var token in new Urid64(s).Split())
					tokens.Add(token.ToString());
				Assert.That(string.Join(new string('_', longest), tokens), Is.EqualTo(s), s);

				if (s.Length < 8)
					foreach (char c in alphabet)
						stack.Push(s + c);
			}
		}
	}

	[TestFixture]
	public class NarrowingTests
	{
		[Test]
		public void KeepsFittingIdentifierUnchanged()
		{
			Urid256 wide = "green_button";
			Assert.That(((Urid64)wide).ToString(), Is.EqualTo("green_button"));
			Assert.That(((Urid128)wide).ToString(), Is.EqualTo("green_button"));
			Assert.That(((Urid192)wide).ToString(), Is.EqualTo("green_button"));
		}

		[Test]
		public void KeepsLeadingSymbols()
		{
			Urid256 wide = new string('a', 30) + new string('b', 19);
			Assert.That(((Urid64)wide).ToString(), Is.EqualTo(new string('a', 12)));
			Assert.That(((Urid128)wide).ToString(), Is.EqualTo(new string('a', 24)));
			Assert.That(((Urid192)wide).ToString(), Is.EqualTo(new string('a', 30) + new string('b', 6)));
			Assert.That(((Urid64)(Urid128)"abcdefghijklmnop").ToString(), Is.EqualTo("abcdefghijkl"));
		}

		[Test]
		public void MatchesBuilderTruncation()
		{
			var b = new UridBuilder();
			b.Append("player_spawn_point_");
			b.Append(42);
			Assert.That(b.To64() == (Urid64)b.To256(), Is.True);
			Assert.That(b.To128() == (Urid128)b.To256(), Is.True);
		}
	}
}
