namespace Tests;

using NUnit.Framework;
using ProjectZyheeda;

public class EitherTest : GameTestCollection {
	[Test]
	public void SwitchValue() {
		var either = Either.NoError<string>().WithValue(42);

		var value = either.Switch(
			error: _ => -1,
			value: v => v
		);

		Assert.That(value, Is.EqualTo(42));
	}

	[Test]
	public void SwitchError() {
		var either = Either.NoValue<int>().WithError("ERROR");

		var value = either.Switch(
			error: v => v,
			value: _ => "OKAY"
		);

		Assert.That(value, Is.EqualTo("ERROR"));
	}

	[Test]
	public void MapErrorToError() {
		var fifth = (int v) => (float)v / 5;
		var result = Either.NoValue<int>().WithError("ERROR").Map(fifth);

		var value = result.Switch(
			error: e => e,
			value: _ => ""
		);

		Assert.That(value, Is.EqualTo("ERROR"));
	}

	[Test]
	public void MapValueToValue() {
		var fifth = (int v) => (float)v / 5;
		var result = Either.NoError<string>().WithValue(42).Map(fifth);

		var value = result.Switch(
			error: _ => 0f,
			value: v => v
		);

		Assert.That(value, Is.EqualTo(8.4f));
	}

	[Test]
	public void FlatMapEitherWithValueAndMapOkayToValue() {
		var inverse = (int v) => Either.NoError<string>().WithValue((float)1 / v);
		var result = Either.NoError<string>().WithValue(42).FlatMap(inverse);

		var value = result.Switch(
			error: _ => 0f,
			value: v => v
		);

		Assert.That(value, Is.EqualTo((float)1 / 42));
	}

	[Test]
	public void FlatMapEitherWithValueAndMapErrorToError() {
		var errorMsg = "non divisible by the answer to the universe and everything";
		var inverse = (int v) => Either.NoValue<float>().WithError(errorMsg);
		var result = Either.NoError<string>().WithValue(42).FlatMap(inverse);

		var value = result.Switch(
			error: e => e,
			value: _ => "OKAY"
		);

		Assert.That(value, Is.EqualTo(errorMsg));
	}

	[Test]
	public void FlatMapEitherWithErrorToError() {
		var inverse = (int v) => Either.NoError<string>().WithValue((float)1 / v);
		var result = Either.NoValue<int>().WithError("ERROR").FlatMap(inverse);

		var value = result.Switch(
			error: e => e,
			value: _ => "OKAY"
		);

		Assert.That(value, Is.EqualTo("ERROR"));
	}

	[Test]
	public void FlatMapSelf() {
		var value = Either.NoError<string>().WithValue(42);
		var nested = Either.NoError<string>().WithValue(value);

		Assert.That(nested.FlatMap(), Is.SameAs(value));
	}

	[Test]
	public void UnpackFallbackWhenError() {
		var error = Either.NoValue<float>().WithError("ERROR");
		Assert.That(error.UnpackOr(-4.2f), Is.EqualTo(-4.2f));
	}

	[Test]
	public void UnpackSome() {
		var value = Either.NoError<string>().WithValue(4.2f);
		Assert.That(value.UnpackOr(42f), Is.EqualTo(4.2f));
	}

	[Test]
	public void SwitchValueAction() {
		var value = Either.NoError<string>().WithValue(42);
		var result = 0;

		value.Switch(
			error: _ => { result = -1; },
			value: v => { result = v; }
		);
		Assert.That(result, Is.EqualTo(42));
	}

	[Test]
	public void SwitchErrorAction() {
		var some = Either.NoValue<int>().WithError("ERROR");
		var result = "";

		some.Switch(
			error: e => { result = e; },
			value: _ => { result = "OKAY"; }
		);
		Assert.That(result, Is.EqualTo("ERROR"));
	}

	[Test]
	public void ApplyMultipleElements() {
		var noError = Either.NoError<string>();
		var fst = noError.WithValue(4);
		var snd = noError.WithValue(15);
		var trd = noError.WithValue(23);

		var sum = Either
			.NoError<string>()
			.WithValue((int a) => (int b) => (int c) => a + b + c);

		var result = sum
			.Apply(fst)
			.Apply(snd)
			.Apply(trd);

		Assert.That(result.UnpackOr(-1), Is.EqualTo(42));
	}

	[Test]
	public void ApplyMultipleElementsError() {
		var noError = Either.NoError<string>();
		var noValue = Either.NoValue<int>();
		var fst = noError.WithValue(4);
		var snd = noValue.WithError("ERROR");
		var trd = noError.WithValue(23);

		var sum = Either
			.NoError<string>()
			.WithValue((int a) => (int b) => (int c) => a + b + c);

		var result = sum
			.Apply(fst)
			.Apply(snd)
			.Apply(trd)
			.Switch(
				error: e => e,
				value: _ => "OKAY"
			);

		Assert.That(result, Is.EqualTo("ERROR"));
	}
}
