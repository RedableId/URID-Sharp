using NUnit.Framework;

namespace URID.Tests
{
    // NUnit 3 constraint-style asserts only (Assert.That) — the same source
    // compiles against Unity's bundled NUnit and the standalone NUnit package.

    [TestFixture]
    public class Urid64Tests
    {
        [Test]
        public void Empty()
        {
            Assert.That(new Urid64("").ToString(), Is.EqualTo(""));
            Assert.That(new Urid64("") == default, Is.True);
            Assert.That(new Urid64("")[0], Is.EqualTo('\0'));
        }

        [TestCase("a")]
        [TestCase("z")]
        [TestCase("0")]
        [TestCase("9")]
        [TestCase("_")]
        [TestCase("abc")]
        [TestCase("abc123")]
        [TestCase("greenbutton")]
        [TestCase("abcdefghijkl")]
        public void RoundTrip(string s) =>
            Assert.That(new Urid64(s).ToString(), Is.EqualTo(s));

        [Test]
        public void UpperCaseIsFolded() =>
            Assert.That(new Urid64("GreenButton").ToString(), Is.EqualTo("greenbutton"));

        [Test]
        public void Indexer()
        {
            var x = new Urid64("hello");
            Assert.That(x[0], Is.EqualTo('h'));
            Assert.That(x[4], Is.EqualTo('o'));
            Assert.That(x[5], Is.EqualTo('\0'), "out of bounds");
        }

        [Test]
        public void Equality()
        {
            Urid64 p = "abc", q = "abc", r = "xyz";
            Assert.That(p == q, Is.True);
            Assert.That(p != r, Is.True);
            Assert.That(p.Equals(q), Is.True);
            Assert.That(p.Equals(r), Is.False);
            Assert.That(p.GetHashCode(), Is.EqualTo(q.GetHashCode()));
        }

        [Test]
        public void ImplicitFromString() =>
            Assert.That(((Urid64)"hello").ToString(), Is.EqualTo("hello"));

        [Test]
        public void TruncatesBeyondMaxLength() =>
            Assert.That(new Urid64("abcdefghijklXXX").ToString(), Is.EqualTo("abcdefghijkl"));
    }

    [TestFixture]
    public class Urid128Tests
    {
        [Test]
        public void Empty() =>
            Assert.That(new Urid128("").ToString(), Is.EqualTo(""));

        [TestCase("a")]
        [TestCase("abc123")]
        [TestCase("abcdefghijklmnopqrstuvwx")]
        public void RoundTrip(string s) =>
            Assert.That(new Urid128(s).ToString(), Is.EqualTo(s));

        [Test]
        public void Equality()
        {
            Assert.That(new Urid128("hello_world") == new Urid128("hello_world"), Is.True);
            Assert.That(new Urid128("hello") != new Urid128("world"), Is.True);
        }
    }

    [TestFixture]
    public class Urid192Tests
    {
        [Test]
        public void Empty() =>
            Assert.That(new Urid192("").ToString(), Is.EqualTo(""));

        [TestCase("a")]
        [TestCase("abc123")]
        [TestCase("abcdefghijklmnopqrstuvwxyz0123456789")]
        public void RoundTrip(string s) =>
            Assert.That(new Urid192(s).ToString(), Is.EqualTo(s));

        [Test]
        public void Indexer()
        {
            var id = new Urid192("test_id");
            Assert.That(id[0], Is.EqualTo('t'));
            Assert.That(id[4], Is.EqualTo('_'));
        }
    }

    [TestFixture]
    public class Urid256Tests
    {
        [Test]
        public void Empty()
        {
            Assert.That(new Urid256("").ToString(), Is.EqualTo(""));
            Assert.That(default(Urid256) == new Urid256(""), Is.True);
        }

        [TestCase("z")]
        [TestCase("unit31_dark")]
        [TestCase("green_buy_button_v2")]
        public void RoundTrip(string s) =>
            Assert.That(new Urid256(s).ToString(), Is.EqualTo(s));

        [Test]
        public void RoundTripMaxLength()
        {
            string s = new string('a', Urid256.MaxLength);
            Assert.That(new Urid256(s).ToString(), Is.EqualTo(s));
        }
    }

    [TestFixture]
    public class UridBuilderTests
    {
        [Test]
        public void TokensAndSeparator()
        {
            var b = new UridBuilder();
            b.Append("green");
            b.AppendSeparator();
            b.Append("button");
            Assert.That(b.To64().ToString(),  Is.EqualTo("green_button"));
            Assert.That(b.To128().ToString(), Is.EqualTo("green_button"));
            Assert.That(b.To256().ToString(), Is.EqualTo("green_button"));
        }

        [Test]
        public void AppendChar()
        {
            var b = new UridBuilder();
            b.Append('h');
            b.Append('i');
            Assert.That(b.To64().ToString(), Is.EqualTo("hi"));
        }

        [Test]
        public void AppendInt()
        {
            var b = new UridBuilder();
            b.Append("unit");
            b.Append(31);
            Assert.That(b.To128().ToString(), Is.EqualTo("unit31"));
        }

        [Test]
        public void AppendZero()
        {
            var b = new UridBuilder();
            b.Append(0);
            Assert.That(b.To64().ToString(), Is.EqualTo("0"));
        }

        [Test]
        public void AppendMultiDigitInt()
        {
            var b = new UridBuilder();
            b.Append(1024);
            Assert.That(b.To64().ToString(), Is.EqualTo("1024"));
        }

        [Test]
        public void TracksLength()
        {
            var b = new UridBuilder();
            b.Append("abc");
            Assert.That(b.Length, Is.EqualTo(3));
            b.Append("de");
            Assert.That(b.Length, Is.EqualTo(5));
        }

        [Test]
        public void To64Truncates()
        {
            var b = new UridBuilder();
            b.Append("abcdefghijklmnop"); // 16 chars
            Assert.That(b.Length, Is.EqualTo(16));
            Assert.That(b.To64().ToString(),  Is.EqualTo("abcdefghijkl"));
            Assert.That(b.To128().ToString(), Is.EqualTo("abcdefghijklmnop"));
        }

        [Test]
        public void MatchesDirectConstruction()
        {
            var b = new UridBuilder();
            b.Append("player");
            b.AppendSeparator();
            b.Append("x");
            b.Append(3);
            Assert.That(b.To128().ToString(), Is.EqualTo("player_x3"));
            Assert.That(b.To128() == new Urid128("player_x3"), Is.True);
        }
    }

    [TestFixture]
    public class WideningTests
    {
        [Test]
        public void From64()
        {
            Urid64 a = "greenbutton";
            Assert.That(((Urid128)a).ToString(), Is.EqualTo(a.ToString()));
            Assert.That(((Urid192)a).ToString(), Is.EqualTo(a.ToString()));
            Assert.That(((Urid256)a).ToString(), Is.EqualTo(a.ToString()));
            Assert.That((Urid128)a == new Urid128("greenbutton"), Is.True, "value identity");
        }

        [Test]
        public void From128()
        {
            Urid128 x = "hello_world";
            Assert.That(((Urid192)x).ToString(), Is.EqualTo(x.ToString()));
            Assert.That(((Urid256)x).ToString(), Is.EqualTo(x.ToString()));
        }

        [Test]
        public void From192()
        {
            Urid192 p = "unit31_dark_theme";
            Assert.That(((Urid256)p).ToString(), Is.EqualTo(p.ToString()));
        }
    }
}
