namespace Tests;

using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using ProjectZyheeda;

public class EitherTest : GameTestCollection {
	[Test]
	public void SwitchValue() {
		var either = new Either<int, string>("42");
		var result = either.Switch(
			error: _ => "-1",
			value: v => v + "!"
		);
		Assert.That(result, Is.EqualTo("42!"));
	}

	[Test]
	public void SwitchError() {
		var either = new Either<int, string>(42);
		var result = either.Switch(
			error: v => v + 1,
			value: _ => -1
		);
		Assert.That(result, Is.EqualTo(43));
	}

	[Test]
	public void ValueToEither() {
		Either<int, string> either = "42";
		var result = either.Switch(
			error: _ => "-1",
			value: v => v + "!"
		);
		Assert.That(result, Is.EqualTo("42!"));
	}

	[Test]
	public void ErrorToEither() {
		Either<int, string> either = 42;
		var result = either.Switch(
			error: v => v + 1,
			value: _ => -1
		);
		Assert.That(result, Is.EqualTo(43));
	}
}

public class EitherToolTest : GameTestCollection {
	[Test]
	public void MapErrorToError() {
		var fifth = (int v) => (float)v / 5;
		var result = new Either<string, int>("ERROR");

		var value = result.Switch(
			error: e => e,
			value: _ => ""
		);

		Assert.That(value, Is.EqualTo("ERROR"));
	}

	[Test]
	public void MapValueToValue() {
		var fifth = (int v) => (float)v / 5;
		var result = new Either<string, int>(42).Map(fifth);

		var value = result.Switch(
			error: _ => 0f,
			value: v => v
		);

		Assert.That(value, Is.EqualTo(8.4f));
	}

	[Test]
	public void MapErrorErrorToError() {
		var toLength = (string v) => v.Length;
		var result = new Either<string, int>("ERROR").MapError(toLength);

		var value = result.Switch(
			error: e => e,
			value: _ => -1
		);

		Assert.That(value, Is.EqualTo(5));
	}

	[Test]
	public void MapErrorValueToValue() {
		var toLength = (string v) => v.Length;
		var result = new Either<string, float>(4.2f).MapError(toLength);

		var value = result.Switch(
			error: _ => -1f,
			value: v => v
		);

		Assert.That(value, Is.EqualTo(4.2f));
	}

	[Test]
	public void FlatMapEitherWithValueAndMapOkayToValue() {
		var inverse = (int v) => new Either<string, float>((float)1 / v);
		var result = new Either<string, int>(42).FlatMap(inverse);

		var value = result.Switch(
			error: _ => 0f,
			value: v => v
		);

		Assert.That(value, Is.EqualTo((float)1 / 42));
	}

	[Test]
	public void FlatMapEitherWithValueAndMapErrorToError() {
		var errorMsg = "non divisible by the answer to the universe and everything";
		var inverse = (int v) => new Either<string, float>(errorMsg);
		var result = new Either<string, int>(42).FlatMap(inverse);

		var value = result.Switch(
			error: e => e,
			value: _ => "OKAY"
		);

		Assert.That(value, Is.EqualTo(errorMsg));
	}

	[Test]
	public void FlatMapEitherWithErrorToError() {
		var inverse = (int v) => new Either<string, float>((float)1 / v);
		var result = new Either<string, int>("ERROR").FlatMap(inverse);

		var value = result.Switch(
			error: e => e,
			value: _ => "OKAY"
		);

		Assert.That(value, Is.EqualTo("ERROR"));
	}

	[Test]
	public void FlatMapSelf() {
		var value = new Either<string, int>(42);
		var nested = new Either<string, Either<string, int>>(value);

		Assert.That(nested.Flatten().UnpackOr(-1), Is.EqualTo(42));
	}

	[Test]
	public void UnpackFallbackWhenError() {
		var error = new Either<string, float>("ERROR");
		Assert.That(error.UnpackOr(-4.2f), Is.EqualTo(-4.2f));
	}

	[Test]
	public void UnpackValue() {
		var value = new Either<string, float>(4.2f);
		Assert.That(value.UnpackOr(42f), Is.EqualTo(4.2f));
	}

	[Test]
	public void UnpackErrorWhenError() {
		var error = new Either<string, float>("ERROR");
		Assert.That(error.UnpackErrorOr(""), Is.EqualTo("ERROR"));
	}

	[Test]
	public void UnpackErrorWhenNoError() {
		var value = new Either<string, float>(4.2f);
		Assert.That(value.UnpackErrorOr("NO ERROR"), Is.EqualTo("NO ERROR"));
	}

	[Test]
	public void SwitchValueAction() {
		var value = new Either<string, int>(42);
		var result = 0;

		value.Switch(
			error: _ => { result = -1; },
			value: v => { result = v; }
		);
		Assert.That(result, Is.EqualTo(42));
	}

	[Test]
	public void SwitchErrorAction() {
		var value = new Either<string, int>("ERROR");
		var result = "";

		value.Switch(
			error: e => { result = e; },
			value: _ => { result = "OKAY"; }
		);
		Assert.That(result, Is.EqualTo("ERROR"));
	}

	[Test]
	public void ApplyMultipleElements() {
		var fst = new Either<string, int>(4);
		var snd = new Either<string, int>(15);
		var trd = new Either<string, int>(23);

		var sum = new Either<string, Func<int, Func<int, Func<int, int>>>>(
			(int a) => (int b) => (int c) => a + b + c
		);

		var result = sum
			.Apply(fst)
			.Apply(snd)
			.Apply(trd);

		Assert.That(result.UnpackOr(-1), Is.EqualTo(42));
	}

	[Test]
	public void ApplyMultipleElementsError() {
		var fst = new Either<string, int>(4);
		var snd = new Either<string, int>("ERROR");
		var trd = new Either<string, int>(23);

		var sum = new Either<string, Func<int, Func<int, Func<int, int>>>>(
			(int a) => (int b) => (int c) => a + b + c
		);

		var result = sum
			.Apply(fst)
			.Apply(snd)
			.Apply(trd);

		Assert.That(result.UnpackErrorOr("OKAY"), Is.EqualTo("ERROR"));
	}

	[Test]
	public void ApplyMultipleElementsAndConcatError() {
		var fst = new Either<string, int>(4);
		var snd = new Either<string, int>(15);
		var trd = new Either<string, int>(23);

		var sum = new Either<IEnumerable<string>, Func<int, Func<int, Func<int, int>>>>(
			(int a) => (int b) => (int c) => a + b + c
		);

		var result = sum
			.ApplyWeak(fst)
			.ApplyWeak(snd)
			.ApplyWeak(trd);

		Assert.That(result.UnpackOr(-1), Is.EqualTo(42));
	}

	[Test]
	public void ApplyMultipleElementsErrorConcatElementErrors() {
		var fst = new Either<string, int>(4);
		var snd = new Either<string, int>("ERROR 2");
		var trd = new Either<string, int>("ERROR 3");

		var sum = new Either<IEnumerable<string>, Func<int, Func<int, Func<int, int>>>>(
			(int a) => (int b) => (int c) => a + b + c
		);

		var result = sum
			.ApplyWeak(fst)
			.ApplyWeak(snd)
			.ApplyWeak(trd);

		CollectionAssert.AreEqual(
			new[] { "ERROR 2", "ERROR 3" },
			result.UnpackErrorOr(Enumerable.Empty<string>())
		);
	}

	[Test]
	public void ValueToSome() {
		var value = new Either<string, int>(42);
		var some = value.ToMaybe();

		Assert.That(some.UnpackOr(-1), Is.EqualTo(42));
	}

	[Test]
	public void ErrorToNone() {
		var value = new Either<string, int>("Error");
		var some = value.ToMaybe();

		Assert.That(some.UnpackOr(-1), Is.EqualTo(-1));
	}
}
