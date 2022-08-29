namespace Tests;

using NUnit.Framework;
using ProjectZyheeda;

public class MaybeTest : GameTestCollection {
	[Test]
	public void MatchSomeValue() {
		var maybe = Maybe.Some(42);
		var (callSome, callNone) = (0, 0);

		maybe.Match(
			some: v => {
				++callSome;
				Assert.That(v, Is.EqualTo(42));
			},
			none: () => ++callNone
		);

		Assert.That((callSome, callNone), Is.EqualTo((1, 0)));
	}

	[Test]
	public void MatchNone() {
		var maybe = Maybe.None<int>();
		var (callSome, callNone) = (0, 0);

		maybe.Match(
			some: _ => ++callSome,
			none: () => ++callNone
		);

		Assert.That((callSome, callNone), Is.EqualTo((0, 1)));
	}

	[Test]
	public void MapNoneToNone() {
		var func = (int v) => v.ToString();
		var result = Maybe.None<int>().Map(func);
		result.Match(
			some: _ => Assert.Fail("some called"),
			none: () => Assert.Pass()
		);
	}

	[Test]
	public void MapSomeToSome() {
		var func = (int v) => v.ToString();
		var result = Maybe.Some(42).Map(func);
		result.Match(
			some: v => Assert.That(v, Is.EqualTo("42")),
			none: () => Assert.Fail("none called")
		);
	}

	[Test]
	public void BindNoneWithNoneToNone() {
		var func = (int v) => Maybe.None<string>();
		var result = Maybe.None<int>().Bind(func);
		result.Match(
			some: _ => Assert.Fail("some called"),
			none: () => Assert.Pass()
		);
	}

	[Test]
	public void BindSomeWithSomeToSome() {
		var func = (int v) => Maybe.Some(v.ToString());
		var result = Maybe.Some(42).Bind(func);
		result.Match(
			some: v => Assert.That(v, Is.EqualTo("42")),
			none: () => Assert.Fail("none called")
		);
	}

	[Test]
	public void UnpackFallbackWhenNone() {
		var none = Maybe.None<float>();
		Assert.That(none.UnpackOr(42f), Is.EqualTo(42f));
	}

	[Test]
	public void UnpackSome() {
		var some = Maybe.Some(4.2f);
		Assert.That(some.UnpackOr(42f), Is.EqualTo(4.2f));
	}
}
