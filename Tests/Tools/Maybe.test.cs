namespace Tests;

using NUnit.Framework;
using ProjectZyheeda;

public class MaybeTest : GameTestCollection {
	[Test]
	public void SwitchSome() {
		var maybe = Maybe.Some(42);

		var value = maybe.Switch(
			some: v => v,
			none: () => -1
		);

		Assert.That(value, Is.EqualTo(42));
	}

	[Test]
	public void SwitchNone() {
		var maybe = Maybe.None<int>();

		var value = maybe.Switch(
			some: v => v,
			none: () => -1
		);

		Assert.That(value, Is.EqualTo(-1));
	}

	[Test]
	public void MapNoneToNone() {
		var func = (int v) => v.ToString();
		var result = Maybe.None<int>().Map(func);
		var value = result.Switch(
			some: v => v,
			none: () => ""
		);

		Assert.That(value, Is.Empty);
	}

	[Test]
	public void MapSomeToSome() {
		var func = (int v) => v.ToString();
		var result = Maybe.Some(42).Map(func);
		var value = result.Switch(
			some: v => v,
			none: () => ""
		);

		Assert.That(value, Is.EqualTo("42"));
	}

	[Test]
	public void FlatMapNoneWithNoneToNone() {
		var func = (int v) => Maybe.None<string>();
		var result = Maybe.None<int>().FlatMap(func);
		var value = result.Switch(
			some: v => v,
			none: () => ""
		);

		Assert.That(value, Is.Empty);
	}

	[Test]
	public void FlatMapSomeWithSomeToSome() {
		var func = (int v) => Maybe.Some(v.ToString());
		var result = Maybe.Some(42).FlatMap(func);
		var value = result.Switch(
			some: v => v,
			none: () => ""
		);

		Assert.That(value, Is.EqualTo("42"));
	}

	[Test]
	public void FlatMapSelf() {
		var some = Maybe.Some(42);
		var nested = Maybe.Some(some);

		Assert.That(nested.FlatMap(), Is.SameAs(some));
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

	[Test]
	public void SwitchSomeAction() {
		var some = Maybe.Some(4.2f);
		var result = 0f;

		some.Switch(
			some: v => { result = v; },
			none: () => { result = -1f; }
		);
		Assert.That(result, Is.EqualTo(4.2f));
	}

	[Test]
	public void SwitchNoneAction() {
		var none = Maybe.None<int>();
		var result = 0f;

		none.Switch(
			some: v => { result = v; },
			none: () => { result = -1f; }
		);
		Assert.That(result, Is.EqualTo(-1f));
	}

	[Test]
	public void ApplyMultipleElements() {
		var fst = Maybe.Some(4);
		var snd = Maybe.Some(15);
		var trd = Maybe.Some(23);

		var sum = Maybe.Some((int a) => (int b) => (int c) => a + b + c);
		var result = sum
			.Apply(fst)
			.Apply(snd)
			.Apply(trd);

		Assert.That(result.UnpackOr(-1), Is.EqualTo(42));
	}

	[Test]
	public void ApplyMultipleElementsNone() {
		var fst = Maybe.Some(4);
		var snd = Maybe.None<int>();
		var trd = Maybe.Some(23);

		var sum = Maybe.Some((int a) => (int b) => (int c) => a + b + c);
		var result = sum
			.Apply(fst)
			.Apply(snd)
			.Apply(trd);

		Assert.That(result.UnpackOr(-1), Is.EqualTo(-1));
	}
}
