namespace Tests;

using System;
using System.Linq;
using NUnit.Framework;
using ProjectZyheeda;

public class TestResult : GameTestCollection {
	[Test]
	public void SwitchValue() {
		var either = new Result<string>("42");
		var result = either.Switch(
			errors => "-1",
			value => value
		);
		Assert.That(result, Is.EqualTo("42"));
	}

	[Test]
	public void SwitchPlayerError() {
		var result = new Result<string>((PlayerStr)"AAA").Switch(
			errors => errors.First().Switch(v => (string)v, v => (string)v),
			_ => "BBB"
		);
		Assert.That(result, Is.EqualTo("AAA"));
	}

	[Test]
	public void SwitchSystemError() {
		var result = new Result<string>((SystemStr)"AAA").Switch(
			errors => errors.First().Switch(v => (string)v, v => (string)v),
			_ => "BBB"
		);
		Assert.That(result, Is.EqualTo("AAA"));
	}

	[Test]
	public void SwitchErrors() {
		var errors = new U<SystemStr, PlayerStr>[] { (SystemStr)"AAA", (PlayerStr)"BBB" };
		var result = new Result<string>(errors).Switch(
			errors => errors,
			_ => Array.Empty<U<SystemStr, PlayerStr>>()
		);
		Assert.That(result, Is.SameAs(errors));
	}
}

public class TestResultExtensions : GameTestCollection {
	[Test]
	public void MapValueToValue() {
		var fifth = (int v) => (float)v / 5;
		var result = new Result<int>(42).Map(fifth);

		var value = result.Switch(
			errors: _ => 0f,
			value: v => v
		);

		Assert.That(value, Is.EqualTo(8.4f));
	}

	[Test]
	public void FlatMapEitherWithValueAndMapOkayToValue() {
		var invert = (int v) => new Result<float>((float)1 / v);
		var result = new Result<int>(42).FlatMap(invert);

		var value = result.Switch(
			errors: _ => 0f,
			value: v => v
		);

		Assert.That(value, Is.EqualTo((float)1 / 42));
	}

	[Test]
	public void FlatMapEitherWithValueAndMapErrorToError() {
		var errorMsg = "non divisible by the answer to the universe and everything";
		var inverse = (int v) => new Result<float>((PlayerStr)errorMsg);
		var result = new Result<int>(42).FlatMap(inverse);

		var value = result.Switch(
			errors => errors.First().Switch(v => (string)v, v => (string)v),
			_ => "OKAY"
		);

		Assert.That(value, Is.EqualTo(errorMsg));
	}

	[Test]
	public void FlatMapEitherWithErrorToError() {
		var inverse = (int v) => new Result<float>((float)1 / v);
		var result = new Result<int>((SystemStr)"ERROR").FlatMap(inverse);

		var value = result.Switch(
			errors => errors.First().Switch(v => (string)v, v => (string)v),
			value: _ => "OKAY"
		);

		Assert.That(value, Is.EqualTo("ERROR"));
	}

	[Test]
	public void FlatMapSelf() {
		var value = new Result<int>(42);
		var nested = new Result<Result<int>>(value);

		Assert.That(nested.Flatten().UnpackOr(-1), Is.EqualTo(42));
	}

	[Test]
	public void UnpackFallbackWhenError() {
		var error = new Result<float>((PlayerStr)"ERROR");
		Assert.That(error.UnpackOr(-4.2f), Is.EqualTo(-4.2f));
	}

	[Test]
	public void UnpackValue() {
		var value = new Result<float>(4.2f);
		Assert.That(value.UnpackOr(42f), Is.EqualTo(4.2f));
	}

	[Test]
	public void ApplyMultipleElements() {
		var fst = new Result<int>(4);
		var snd = new Result<int>(15);
		var trd = new Result<int>(23);

		var sum = new Result<Func<int, Func<int, Func<int, int>>>>(
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
		var fst = new Result<int>(4);
		var snd = new Result<int>((PlayerStr)"ERROR");
		var trd = new Result<int>(23);

		var sum = new Result<Func<int, Func<int, Func<int, int>>>>(
			(int a) => (int b) => (int c) => a + b + c
		);

		var result = sum
			.Apply(fst)
			.Apply(snd)
			.Apply(trd);

		var firstError = result.Switch(
			e => e.First().Switch(
				e => (string)e,
				e => (string)e
			),
			_ => "NO ERROR"
		);
		Assert.That(firstError, Is.EqualTo("ERROR"));
	}

	[Test]
	public void ApplyMultipleElementsErrorConcatElementErrors() {
		var fst = new Result<int>(4);
		var snd = new Result<int>((PlayerStr)"ERROR 2");
		var trd = new Result<int>((SystemStr)"ERROR 3");

		var sum = new Result<Func<int, Func<int, Func<int, int>>>>(
			(int a) => (int b) => (int c) => a + b + c
		);

		var result = sum
			.ApplyWeak(fst)
			.ApplyWeak(snd)
			.ApplyWeak(trd);

		var errors = result.Switch(e => e, v => Array.Empty<U<SystemStr, PlayerStr>>());
		Assert.That(
			errors,
			Is.EquivalentTo(new U<SystemStr, PlayerStr>[] { (PlayerStr)"ERROR 2", (SystemStr)"ERROR 3" })
		);
	}

	[Test]
	public void ApplyOnFunc() {
		var fst = new Result<int>(4);
		var snd = new Result<int>(10);
		var trd = new Result<int>(11);

		var sum = (int a) => (int b) => (int c) => a + b + c;
		var result = sum
			.Apply(fst)
			.Apply(snd)
			.Apply(trd);

		Assert.That(result.UnpackOr(-1), Is.EqualTo(25));
	}

	[Test]
	public void ApplyOnFuncError() {
		var fst = new Result<int>((PlayerStr)"error");
		var snd = new Result<int>(10);
		var trd = new Result<int>(11);

		var sum = (int a) => (int b) => (int c) => a + b + c;
		var result = sum
			.Apply(fst)
			.Apply(snd)
			.Apply(trd);

		var firstError = result.Switch(
			e => e.First().Switch(
				e => (string)e,
				e => (string)e
			),
			_ => "NO ERROR"
		);
		Assert.That(firstError, Is.EqualTo("error"));
	}

	[Test]
	public void ApplyWeakOnFunc() {
		var fst = new Result<int>(4);
		var snd = new Result<int>(10);
		var trd = new Result<int>(11);

		var sum = (int a) => (int b) => (int c) => a + b + c;
		var result = sum
			.ApplyWeak(fst)
			.ApplyWeak(snd)
			.ApplyWeak(trd);

		Assert.That(result.UnpackOr(-1), Is.EqualTo(25));
	}

	[Test]
	public void ApplyWeakOnFuncErrors() {
		var fst = new Result<int>((PlayerStr)"error fst");
		var snd = new Result<int>(10);
		var trd = new Result<int>((SystemStr)"error trd");

		var sum = (int a) => (int b) => (int c) => a + b + c;
		var result = sum
			.ApplyWeak(fst)
			.ApplyWeak(snd)
			.ApplyWeak(trd);

		var errors = result.Switch(e => e, v => Array.Empty<U<SystemStr, PlayerStr>>());
		Assert.That(
			errors,
			Is.EquivalentTo(new U<SystemStr, PlayerStr>[] { (PlayerStr)"error fst", (SystemStr)"error trd" })
		);
	}

	[Test]
	public void ValueToSome() {
		var value = new Result<int>(42);
		var some = value.ToMaybe();

		Assert.That(some.UnpackOr(-1), Is.EqualTo(42));
	}

	[Test]
	public void PlayerErrorToNone() {
		var value = new Result<int>((PlayerStr)"Error");
		var some = value.ToMaybe();

		Assert.That(some.UnpackOr(-1), Is.EqualTo(-1));
	}

	[Test]
	public void SystemErrorToNone() {
		var value = new Result<int>((SystemStr)"Error");
		var some = value.ToMaybe();

		Assert.That(some.UnpackOr(-1), Is.EqualTo(-1));
	}

	[Test]
	public void OkOrPlayerErrorOkFromRefType() {
		var value = "Hello";
		value.OkOrPlayerError("error").Switch(
			e => Assert.Fail($"Was {string.Join(", ", e)}, but should have been {value}"),
			v => Assert.That(v, Is.SameAs(value))
		);
	}

	[Test]
	public void OkOrPlayerErrorOkFromOptionalValueType() {
		int? value = 42;
		value.OkOrPlayerError("error").Switch(
			e => Assert.Fail($"Was {string.Join(", ", e)}, but should have been {value}"),
			v => Assert.That(v, Is.EqualTo(value))
		);
	}

	[Test]
	public void OkOrPlayerErrorOptionalRefType() {
		var value = null as string;
		value.OkOrPlayerError("error").Switch(
			e => Assert.That(e.First().Switch<PlayerStr>(_ => "", v => v), Is.EqualTo((PlayerStr)"error")),
			v => Assert.Fail($"Was {v ?? "null"}, but should have been error")
		);
	}

	[Test]
	public void OkOrPlayerErrorOptionalValueType() {
		int? value = null;
		value.OkOrPlayerError("error").Switch(
			e => Assert.That(e.First().Switch<PlayerStr>(_ => "", v => v), Is.EqualTo((PlayerStr)"error")),
			v => Assert.Fail($"Was {v}, but should have been no number")
		);
	}

	[Test]
	public void OkOrSystemErrorOkFromRefType() {
		var value = "Hello";
		value.OkOrSystemError("error").Switch(
			e => Assert.Fail($"Was {string.Join(", ", e)}, but should have been {value}"),
			v => Assert.That(v, Is.SameAs(value))
		);
	}

	[Test]
	public void OkOrSystemErrorOkFromOptionalValueType() {
		int? value = 42;
		value.OkOrSystemError("error").Switch(
			e => Assert.Fail($"Was {string.Join(", ", e)}, but should have been {value}"),
			v => Assert.That(v, Is.EqualTo(value))
		);
	}

	[Test]
	public void OkOrSystemErrorOptionalRefType() {
		var value = null as string;
		value.OkOrSystemError("error").Switch(
			e => Assert.That(e.First().Switch<SystemStr>(v => v, _ => ""), Is.EqualTo((SystemStr)"error")),
			v => Assert.Fail($"Was {v ?? "null"}, but should have been error")
		);
	}

	[Test]
	public void OkOrSystemErrorOptionalValueType() {
		int? value = null;
		value.OkOrSystemError("error").Switch(
			e => Assert.That(e.First().Switch<SystemStr>(v => v, _ => ""), Is.EqualTo((SystemStr)"error")),
			v => Assert.Fail($"Was {v}, but should have been no number")
		);
	}
}

public class TestResultInstantiateMethods {
	[Test]
	public void Ok() {
		var result = Result.Ok(42);

		Assert.That(result.UnpackOr(-1), Is.EqualTo(42));
	}

	[Test]
	public void PlayerError() {
		Result<int> result = Result.PlayerError("error");

		var error = result.Switch(
			errors => errors.First().Switch<string>(
				_ => "wrong error",
				p => (string)p
			),
			_ => "no error"
		);
		Assert.That(error, Is.EqualTo("error"));
	}

	[Test]
	public void SystemError() {
		Result<int> result = Result.SystemError("error");

		var error = result.Switch(
			errors => errors.First().Switch<string>(
				s => (string)s,
				_ => "wrong error"
			),
			_ => "no error"
		);
		Assert.That(error, Is.EqualTo("error"));
	}

	[Test]
	public void Error() {
		Result<int> result = Result.Error((SystemStr)"error");

		var error = result.Switch(
			errors => errors.First().Switch<string>(
				s => (string)s,
				_ => "wrong error"
			),
			_ => "no error"
		);
		Assert.That(error, Is.EqualTo("error"));
	}

	[Test]
	public void SystemErrors() {
		var errors = new U<SystemStr, PlayerStr>[] {
			(SystemStr)"sError",
			(PlayerStr)"pError"
		};
		Result<int> result = Result.Errors(errors);

		var rErrors = result.Switch(
			errors => errors,
			_ => Array.Empty<U<SystemStr, PlayerStr>>()
		);
		Assert.That(rErrors, Is.EquivalentTo(errors));
	}
}
