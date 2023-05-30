namespace Tests;

using System;
using System.Linq;
using Moq;
using NUnit.Framework;
using ProjectZyheeda;

public class TestResultExtensions {
	private struct FakeError : IResult<string> {
		public TOut Switch<TOut>(Func<string, TOut> fst, Func<TOut> snd) {
			return fst("42");
		}
	}

	private struct FakeOk : IResult<string> {
		public TOut Switch<TOut>(Func<string, TOut> fst, Func<TOut> snd) {
			return snd();
		}
	}

	[Test]
	public void SwitchActionError() {
		var result = new TestResultExtensions.FakeError();
		var callback = Mock.Of<Action<string>>();

		result.Switch(callback, () => { });
		Mock
			.Get(callback)
			.Verify(c => c.Invoke("42"), Times.Once);
	}

	[Test]
	public void SwitchActionOk() {
		var result = new TestResultExtensions.FakeOk();
		var callback = Mock.Of<Action>();

		result.Switch(_ => { }, callback);
		Mock
			.Get(callback)
			.Verify(c => c.Invoke(), Times.Once);
	}

	[Test]
	public void ErrorAsString() {
		Result result = Result.Errors((new SystemError[] { "A", "B", "C" }, new PlayerError[] { "a", "b", "c" }));
		Assert.That(result.UnpackToString(), Is.EqualTo("Result(SystemErrors(A, B, C), PlayerErrors(a, b, c))"));
	}

	[Test]
	public void OkAsString() {
		var result = Result.Ok();
		Assert.That(result.UnpackToString(), Is.EqualTo("Result(Ok)"));
	}
}

public class TestGenericResultExtensions {
	private struct FakeError : IResult<int, string> {
		public TOut Switch<TOut>(Func<int, TOut> fst, Func<string, TOut> snd) {
			return fst(42);
		}
	}

	private struct FakeOk : IResult<int, string> {
		public TOut Switch<TOut>(Func<int, TOut> fst, Func<string, TOut> snd) {
			return snd("42");
		}
	}

	[Test]
	public void SwitchActionError() {
		var result = new TestGenericResultExtensions.FakeError();
		var callback = Mock.Of<Action<int>>();

		result.Switch(callback, _ => { });
		Mock
			.Get(callback)
			.Verify(c => c.Invoke(42), Times.Once);
	}

	[Test]
	public void SwitchActionOk() {
		var result = new TestGenericResultExtensions.FakeOk();
		var callback = Mock.Of<Action<string>>();

		result.Switch(_ => { }, callback);
		Mock
			.Get(callback)
			.Verify(c => c.Invoke("42"), Times.Once);
	}

	[Test]
	public void ErrorAsString() {
		var result = new Result<int>((new SystemError[] { "A", "B", "C" }, new PlayerError[] { "a", "b", "c" }));
		Assert.That(result.UnpackToString(), Is.EqualTo("Result<Int32>(SystemErrors(A, B, C), PlayerErrors(a, b, c))"));
	}

	[Test]
	public void ValueAsString() {
		var result = new Result<int>(42);
		Assert.That(result.UnpackToString(), Is.EqualTo("Result<Int32>(42)"));
	}

	[Test]
	public void MapValueToValue() {
		var fifth = (int v) => (float)v / 5;
		var result = new Result<int>(42).Map(fifth);

		var value = result.Switch(
			error: _ => 0f,
			value: v => v
		);

		Assert.That(value, Is.EqualTo(8.4f));
	}

	[Test]
	public void FlatMapResultWithValueAndMapOkayToValue() {
		var invert = (int v) => new Result<float>((float)1 / v);
		var result = new Result<int>(42).FlatMap(invert);

		var value = result.Switch(
			error: _ => 0f,
			value: v => v
		);

		Assert.That(value, Is.EqualTo((float)1 / 42));
	}

	[Test]
	public void FlatMapResultWithValueAndMapOkayNonGenericResultOk() {
		var okayFunk = (int v) => Result.Ok();
		var result = new Result<int>(42).FlatMap(okayFunk);

		var ok = result.Switch(
			_ => false,
			() => true
		);

		Assert.That(ok, Is.True);
	}

	[Test]
	public void FlatMapResultWithValueAndMapErrorToError() {
		var errorMsg = "non divisible by the answer to the universe and everything";
		var func = (int v) => new Result<float>((Array.Empty<SystemError>(), new PlayerError[] { errorMsg }));
		var result = new Result<int>(42).FlatMap(func);

		var value = result.Switch<string>(
			errors => errors.player.First(),
			_ => "OKAY"
		);

		Assert.That(value, Is.EqualTo(errorMsg));
	}

	[Test]
	public void FlatMapResultWithValueAndMapErrorToNonGenericResultError() {
		var errorMsg = "non divisible by the answer to the universe and everything";
		var func = (int v) => (Result)Result.PlayerError(errorMsg);
		var result = new Result<int>(42).FlatMap(func);

		var value = result.Switch<string>(
			errors => errors.player.First(),
			() => "OKAY"
		);

		Assert.That(value, Is.EqualTo(errorMsg));
	}

	[Test]
	public void FlatMapResultWithErrorToError() {
		var func = (int v) => new Result<float>((float)1 / v);
		var result = new Result<int>((new SystemError[] { "ERROR" }, Array.Empty<PlayerError>())).FlatMap(func);

		var value = result.Switch<string>(
			errors => errors.system.First(),
			value: _ => "OKAY"
		);

		Assert.That(value, Is.EqualTo("ERROR"));
	}

	[Test]
	public void FlatMapResultWithErrorToNonGenericResultError() {
		var func = (int v) => Result.Ok();
		var result = new Result<int>((new SystemError[] { "ERROR" }, Array.Empty<PlayerError>())).FlatMap(func);

		var value = result.Switch<string>(
			errors => errors.system.First(),
			() => "OKAY"
		);

		Assert.That(value, Is.EqualTo("ERROR"));
	}

	[Test]
	public void Flatten() {
		var value = new Result<int>(42);
		var nested = new Result<Result<int>>(value);

		Assert.That(nested.Flatten().UnpackOr(-1), Is.EqualTo(42));
	}

	[Test]
	public void FlattenWithNonGenericError() {
		var value = Result.Ok();
		var nested = new Result<Result>(value);
		var ok = nested.Flatten().Switch(_ => false, () => true);

		Assert.That(ok, Is.True);
	}

	[Test]
	public void UnpackFallbackWhenError() {
		var error = new Result<float>((Array.Empty<SystemError>(), new PlayerError[] { "ERROR" }));
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
		var snd = new Result<int>((Array.Empty<SystemError>(), new PlayerError[] { "ERROR" }));
		var trd = new Result<int>(23);

		var sum = new Result<Func<int, Func<int, Func<int, int>>>>(
			(int a) => (int b) => (int c) => a + b + c
		);

		var result = sum
			.Apply(fst)
			.Apply(snd)
			.Apply(trd);

		var firstError = result.Switch<string>(
			error => error.player.First(),
			_ => "WRONG OR NO ERROR"
		);
		Assert.That(firstError, Is.EqualTo("ERROR"));
	}

	[Test]
	public void ApplyMultipleElementsErrorConcatElementErrors() {
		var fst = new Result<int>(4);
		var snd = new Result<int>((Array.Empty<SystemError>(), new PlayerError[] { "ERROR 2" }));
		var trd = new Result<int>((new SystemError[] { "ERROR 3" }, Array.Empty<PlayerError>()));

		var sum = new Result<Func<int, Func<int, Func<int, int>>>>(
			(int a) => (int b) => (int c) => a + b + c
		);

		var result = sum
			.ApplyWeak(fst)
			.ApplyWeak(snd)
			.ApplyWeak(trd);

		var errors = result.Switch(
			errors => errors,
			v => (Array.Empty<SystemError>(), Array.Empty<PlayerError>())
		);
		Assert.That(
			errors,
			Is.EqualTo((new SystemError[] { "ERROR 3" }, new PlayerError[] { "ERROR 2" }))
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
		var fst = new Result<int>((Array.Empty<SystemError>(), new PlayerError[] { "error" }));
		var snd = new Result<int>(10);
		var trd = new Result<int>(11);

		var sum = (int a) => (int b) => (int c) => a + b + c;
		var result = sum
			.Apply(fst)
			.Apply(snd)
			.Apply(trd);

		var firstError = result.Switch<string>(
			errors => errors.player.First(),
			_ => "WRONG OR NO ERROR"
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
		var fst = new Result<int>((Array.Empty<SystemError>(), new PlayerError[] { "error fst" }));
		var snd = new Result<int>(10);
		var trd = new Result<int>((new SystemError[] { "error trd" }, Array.Empty<PlayerError>()));

		var sum = (int a) => (int b) => (int c) => a + b + c;
		var result = sum
			.ApplyWeak(fst)
			.ApplyWeak(snd)
			.ApplyWeak(trd);

		var errors = result.Switch(
			errors => errors,
			v => (Array.Empty<SystemError>(), Array.Empty<PlayerError>())
		);
		Assert.That(
			errors,
			Is.EqualTo((new SystemError[] { "error trd" }, new PlayerError[] { "error fst" }))
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
		var value = new Result<int>((Array.Empty<SystemError>(), Array.Empty<PlayerError>()));
		var some = value.ToMaybe();

		Assert.That(some.UnpackOr(-1), Is.EqualTo(-1));
	}

	[Test]
	public void SystemErrorToNone() {
		var value = new Result<int>((Array.Empty<SystemError>(), Array.Empty<PlayerError>()));
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
			e => Assert.That((string)e.player.First(), Is.EqualTo("error")),
			v => Assert.Fail($"Was {v ?? "null"}, but should have been error")
		);
	}

	[Test]
	public void OkOrPlayerErrorOptionalValueType() {
		int? value = null;
		value.OkOrPlayerError("error").Switch(
			e => Assert.That((string)e.player.First(), Is.EqualTo("error")),
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
			e => Assert.That((string)e.system.First(), Is.EqualTo("error")),
			v => Assert.Fail($"Was {v ?? "null"}, but should have been error")
		);
	}

	[Test]
	public void OkOrSystemErrorOptionalValueType() {
		int? value = null;
		value.OkOrSystemError("error").Switch(
			e => Assert.That((string)e.system.First(), Is.EqualTo("error")),
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

		var error = result.Switch<string>(
			errors => errors.player.First(),
			_ => "no error"
		);
		Assert.That(error, Is.EqualTo("error"));
	}

	[Test]
	public void SystemError() {
		Result<int> result = Result.SystemError("error");

		var error = result.Switch<string>(
			errors => errors.system.First(),
			_ => "no error"
		);
		Assert.That(error, Is.EqualTo("error"));
	}

	[Test]
	public void Error() {
		Result<int> result = Result.Error((SystemError)"error");

		var error = result.Switch<string>(
			errors => errors.system.First(),
			_ => "no error"
		);
		Assert.That(error, Is.EqualTo("error"));
	}

	[Test]
	public void SystemErrors() {
		var errors = (new SystemError[] { "sError" }, new PlayerError[] { "pError" });
		Result<int> result = Result.Errors(errors);

		var rErrors = result.Switch(
			errors => errors,
			_ => (Array.Empty<SystemError>(), Array.Empty<PlayerError>())
		);
		Assert.That(rErrors, Is.EqualTo(errors));
	}
}
