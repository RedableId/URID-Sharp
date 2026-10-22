using System;
using NUnit.Framework;

namespace URID.Tests
{
	/// <summary>
	/// Random round trips over every length of every size: multi-word arithmetic bugs only show up for some values,
	/// so hand-picked strings are not enough.
	/// </summary>
	[TestFixture]
	public class RoundTripTests
	{
		private const string Alphabet = "abcdefghijklmnopqrstuvwxyz0123456789_";
		private const int SamplesPerLength = 200;

		private static string RandomId(Random random, int length)
		{
			var chars = new char[length];
			for (int i = 0; i < length; i++)
				chars[i] = Alphabet[random.Next(Alphabet.Length)];
			return new string(chars);
		}

		[TestCase(Urid64.MaxLength)]
		[TestCase(Urid128.MaxLength)]
		[TestCase(Urid192.MaxLength)]
		[TestCase(Urid256.MaxLength)]
		public void ParseThenFormatRestoresTheString(int maxLength)
		{
			var random = new Random(maxLength);
			for (int length = 0; length <= maxLength; length++)
			{
				for (int i = 0; i < SamplesPerLength; i++)
				{
					string s = RandomId(random, length);
					string back;
					switch (maxLength)
					{
						case Urid64.MaxLength: back = new Urid64(s).ToString(); break;
						case Urid128.MaxLength: back = new Urid128(s).ToString(); break;
						case Urid192.MaxLength: back = new Urid192(s).ToString(); break;
						default: back = new Urid256(s).ToString(); break;
					}
					Assert.That(back, Is.EqualTo(s));
				}
			}
		}

		[Test]
		public void BuilderMatchesParsing()
		{
			var random = new Random(1);
			for (int length = 0; length <= Urid256.MaxLength; length++)
			{
				for (int i = 0; i < SamplesPerLength; i++)
				{
					string s = RandomId(random, length);
					var b = new UridBuilder();
					b.Append(s);
					Assert.That(b.To256() == new Urid256(s), Is.True, s);
					Assert.That(b.To64() == new Urid64(s), Is.True, s);
				}
			}
		}
	}
}
