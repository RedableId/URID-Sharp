using System;
using System.Collections.Generic;
using NUnit.Framework;

namespace URID.Tests
{
	[TestFixture]
	public class FormatTests
	{
		[TestCase("some_identifier_123__red", ExpectedResult = "some_identifier_123__red")]
		[TestCase("__init__", ExpectedResult = "__init__")]
		public string SnakeCase(string s) => new Urid128(s).ToString(singleCase: true, upperCase: false);

		[TestCase("some_identifier_123__red", ExpectedResult = "SOME_IDENTIFIER_123__RED")]
		[TestCase("__init__", ExpectedResult = "__INIT__")]
		public string ScreamCase(string s) => new Urid128(s).ToString(singleCase: true, upperCase: true);

		[TestCase("some_identifier_123__red", ExpectedResult = "SomeIdentifier_123_Red")]
		[TestCase("__init__", ExpectedResult = "__Init__")]
		[TestCase("init", ExpectedResult = "Init")]
		[TestCase("_init", ExpectedResult = "_Init")]
		[TestCase("_1_a", ExpectedResult = "_1A")]
		[TestCase("123abc", ExpectedResult = "123abc")]
		[TestCase("a_", ExpectedResult = "A_")]
		public string PascalCase(string s) => new Urid128(s).ToString(singleCase: false, upperCase: true);

		[TestCase("some_identifier_123__red", ExpectedResult = "someIdentifier_123_Red")]
		[TestCase("__init__", ExpectedResult = "_Init__")]
		[TestCase("init", ExpectedResult = "init")]
		[TestCase("_init", ExpectedResult = "Init")]
		public string CamelCase(string s) => new Urid128(s).ToString(singleCase: false, upperCase: false);

		[Test]
		public void CustomSeparator()
		{
			Assert.That(new Urid64("a_b__1").ToString(true, false, '-'), Is.EqualTo("a-b--1"));
			Assert.That(new Urid64("a_b__1").ToString(false, true, '-'), Is.EqualTo("AB--1"));
		}

		[Test]
		public void SingleCaseParsesBack()
		{
			var id = new Urid64("ab_12__c");
			Assert.That(new Urid64(id.ToString(true, true, '-')), Is.EqualTo(id));
		}

		[Test]
		public void Empty()
		{
			Assert.That(default(Urid64).ToString(true, false), Is.EqualTo(""));
			Assert.That(default(Urid64).ToString(false, true), Is.EqualTo(""));
		}

		[TestCase('a')]
		[TestCase('Z')]
		[TestCase('5')]
		public void RejectsAlphanumericSeparator(char separator) =>
			Assert.Throws<ArgumentException>(() => new Urid64("a_b").ToString(true, false, separator));

		[Test]
		public void ToStringIsSnakeCase() =>
			Assert.That(new Urid128("Some_ID").ToString(), Is.EqualTo("some_id"));

		/// <summary>Every string over <c>{a, b, 1, _}</c> up to length 6 must format to a distinct result.</summary>
		[TestCase(true, false)]
		[TestCase(true, true)]
		[TestCase(false, false)]
		[TestCase(false, true)]
		public void IsInjective(bool singleCase, bool upperCase)
		{
			const string alphabet = "ab1_";
			var seen = new Dictionary<string, string>();
			var stack = new Stack<string>();
			stack.Push("");
			while (stack.Count > 0)
			{
				string s = stack.Pop();
				string f = new Urid64(s).ToString(singleCase, upperCase);
				if (seen.TryGetValue(f, out string? other))
					Assert.Fail($"'{s}' and '{other}' both format to '{f}'");
				seen.Add(f, s);

				if (s.Length < 6)
					foreach (char c in alphabet)
						stack.Push(s + c);
			}
			Assert.That(seen.Count, Is.EqualTo(1 + 4 + 16 + 64 + 256 + 1024 + 4096));
		}
	}
}
