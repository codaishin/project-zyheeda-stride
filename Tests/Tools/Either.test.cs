namespace Tests;

using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using ProjectZyheeda;

public class EitherTest : GameTestCollection {
	[Test]
	public void SwitchValue() {
		var either = Either.New(42).WithNoError<string>();

		var value = either.Switch(
			error: _ => -1,
			value: v => v
		);

		Assert.That(value, Is.EqualTo(42));
	}

	[Test]
	public void SwitchError() {
		var either = Either.New("ERROR").WithNoValue<int>();

		var value = either.Switch(
			error: v => v,
			value: _ => "OKAY"
		);

		Assert.That(value, Is.EqualTo("ERROR"));
	}

	[Test]
	public void MapErrorToError() {
		var fifth = (int v) => (float)v / 5;
		var result = Either.New("ERROR").WithNoValue<int>().Map(fifth);

		var value = result.Switch(
			error: e => e,
			value: _ => ""
		);

		Assert.That(value, Is.EqualTo("ERROR"));
	}

	[Test]
	public void MapValueToValue() {
		var fifth = (int v) => (float)v / 5;
		var result = Either.New(42).WithNoError<string>().Map(fifth);

		var value = result.Switch(
			error: _ => 0f,
			value: v => v
		);

		Assert.That(value, Is.EqualTo(8.4f));
	}

	[Test]
	public void FlatMapEitherWithValueAndMapOkayToValue() {
		var inverse = (int v) => Either.New((float)1 / v).WithNoError<string>();
		var result = Either.New(42).WithNoError<string>().FlatMap(inverse);

		var value = result.Switch(
			error: _ => 0f,
			value: v => v
		);

		Assert.That(value, Is.EqualTo((float)1 / 42));
	}

	[Test]
	public void FlatMapEitherWithValueAndMapErrorToError() {
		var errorMsg = "non divisible by the answer to the universe and everything";
		var inverse = (int v) => Either.New(errorMsg).WithNoValue<float>();
		var result = Either.New(42).WithNoError<string>().FlatMap(inverse);

		var value = result.Switch(
			error: e => e,
			value: _ => "OKAY"
		);

		Assert.That(value, Is.EqualTo(errorMsg));
	}

	[Test]
	public void FlatMapEitherWithErrorToError() {
		var inverse = (int v) => Either.New((float)1 / v).WithNoError<string>();
		var result = Either.New("ERROR").WithNoValue<int>().FlatMap(inverse);

		var value = result.Switch(
			error: e => e,
			value: _ => "OKAY"
		);

		Assert.That(value, Is.EqualTo("ERROR"));
	}

	[Test]
	public void FlatMapSelf() {
		var value = Either.New(42).WithNoError<string>();
		var nested = Either.New(value).WithNoError<string>();

		Assert.That(nested.FlatMap(), Is.SameAs(value));
	}

	[Test]
	public void UnpackFallbackWhenError() {
		var error = Either.New("ERROR").WithNoValue<float>();
		Assert.That(error.UnpackOr(-4.2f), Is.EqualTo(-4.2f));
	}

	[Test]
	public void UnpackValue() {
		var value = Either.New(4.2f).WithNoError<string>();
		Assert.That(value.UnpackOr(42f), Is.EqualTo(4.2f));
	}

	[Test]
	public void UnpackErrorWhenError() {
		var error = Either.New("ERROR").WithNoValue<float>();
		Assert.That(error.UnpackErrorOr(""), Is.EqualTo("ERROR"));
	}

	[Test]
	public void UnpackErrorWhenNoError() {
		var value = Either.New(4.2f).WithNoError<string>();
		Assert.That(value.UnpackErrorOr("NO ERROR"), Is.EqualTo("NO ERROR"));
	}

	[Test]
	public void SwitchValueAction() {
		var value = Either.New(42).WithNoError<string>();
		var result = 0;

		value.Switch(
			error: _ => { result = -1; },
			value: v => { result = v; }
		);
		Assert.That(result, Is.EqualTo(42));
	}

	[Test]
	public void SwitchErrorAction() {
		var value = Either.New("ERROR").WithNoValue<int>();
		var result = "";

		value.Switch(
			error: e => { result = e; },
			value: _ => { result = "OKAY"; }
		);
		Assert.That(result, Is.EqualTo("ERROR"));
	}

	[Test]
	public void ApplyMultipleElements() {
		var fst = Either.New(4).WithNoError<string>();
		var snd = Either.New(15).WithNoError<string>();
		var trd = Either.New(23).WithNoError<string>();

		var sum = Either
			.New((int a) => (int b) => (int c) => a + b + c)
			.WithNoError<string>();

		var result = sum
			.Apply(fst)
			.Apply(snd)
			.Apply(trd);

		Assert.That(result.UnpackOr(-1), Is.EqualTo(42));
	}

	[Test]
	public void ApplyMultipleElementsError() {
		var fst = Either.New(4).WithNoError<string>();
		var snd = Either.New("ERROR").WithNoValue<int>();
		var trd = Either.New(23).WithNoError<string>();

		var sum = Either
			.New((int a) => (int b) => (int c) => a + b + c)
			.WithNoError<string>();

		var result = sum
			.Apply(fst)
			.Apply(snd)
			.Apply(trd);

		Assert.That(result.UnpackErrorOr("OKAY"), Is.EqualTo("ERROR"));
	}

	[Test]
	public void ApplyMultipleElementsAndConcatError() {
		var fst = Either.New(4).WithNoError<string>();
		var snd = Either.New(15).WithNoError<string>();
		var trd = Either.New(23).WithNoError<string>();

		var sum = Either
			.New((int a) => (int b) => (int c) => a + b + c)
			.WithNoError<IEnumerable<string>>();

		var result = sum
			.Apply(fst)
			.Apply(snd)
			.Apply(trd);

		Assert.That(result.UnpackOr(-1), Is.EqualTo(42));
	}

	[Test]
	public void ApplyMultipleElementsErrorConcatElementErrors() {
		var fst = Either.New(4).WithNoError<string>();
		var snd = Either.New("ERROR 2").WithNoValue<int>();
		var trd = Either.New("ERROR 3").WithNoValue<int>();

		var sum = Either
			.New((int a) => (int b) => (int c) => a + b + c)
			.WithNoError<IEnumerable<string>>();

		var result = sum
			.Apply(fst)
			.Apply(snd)
			.Apply(trd);

		CollectionAssert.AreEqual(
			new[] { "ERROR 2", "ERROR 3" },
			result.UnpackErrorOr(Enumerable.Empty<string>())
		);
	}
}
