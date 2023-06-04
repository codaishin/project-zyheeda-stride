namespace Tests;

using System;
using System.Linq;
using System.Threading.Tasks;
using Moq;
using ProjectZyheeda;
using Xunit;

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

	[Fact]
	public void SwitchActionError() {
		var result = new TestResultExtensions.FakeError();
		var callback = Mock.Of<Action<string>>();

		result.Switch(callback, () => { });
		Mock
			.Get(callback)
			.Verify(c => c.Invoke("42"), Times.Once);
	}

	[Fact]
	public void SwitchActionOk() {
		var result = new TestResultExtensions.FakeOk();
		var callback = Mock.Of<Action>();

		result.Switch(_ => { }, callback);
		Mock
			.Get(callback)
			.Verify(c => c.Invoke(), Times.Once);
	}

	[Fact]
	public void ErrorAsString() {
		Result result = Result.Errors((new SystemError[] { "A", "B", "C" }, new PlayerError[] { "a", "b", "c" }));
		Assert.Equal("Result(SystemErrors(A, B, C), PlayerErrors(a, b, c))", result.UnpackToString());
	}

	[Fact]
	public void OkAsString() {
		var result = Result.Ok();
		Assert.Equal("Result(Ok)", result.UnpackToString());
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

	[Fact]
	public void SwitchActionError() {
		var result = new TestGenericResultExtensions.FakeError();
		var callback = Mock.Of<Action<int>>();

		result.Switch(callback, _ => { });
		Mock
			.Get(callback)
			.Verify(c => c.Invoke(42), Times.Once);
	}

	[Fact]
	public void SwitchActionOk() {
		var result = new TestGenericResultExtensions.FakeOk();
		var callback = Mock.Of<Action<string>>();

		result.Switch(_ => { }, callback);
		Mock
			.Get(callback)
			.Verify(c => c.Invoke("42"), Times.Once);
	}

	[Fact]
	public void ErrorAsString() {
		var result = new Result<int>((new SystemError[] { "A", "B", "C" }, new PlayerError[] { "a", "b", "c" }));
		Assert.Equal("Result<Int32>(SystemErrors(A, B, C), PlayerErrors(a, b, c))", result.UnpackToString());
	}

	[Fact]
	public void ValueAsString() {
		var result = new Result<int>(42);
		Assert.Equal("Result<Int32>(42)", result.UnpackToString());
	}

	[Fact]
	public void MapValueToValue() {
		var fifth = (int v) => (float)v / 5;
		var result = new Result<int>(42).Map(fifth);

		var value = result.Switch(
			error: _ => 0f,
			value: v => v
		);

		Assert.Equal(8.4f, value);
	}

	[Fact]
	public void MapVoidOkay() {
		var result = Result.Ok();
		var ok = result.Map(() => true).UnpackOr(false);

		Assert.True(ok);
	}

	[Fact]
	public void MapVoidErrors() {
		var result = (Result)Result.Errors((new SystemError[] { "AAA" }, new PlayerError[] { "BBB" }));
		var errors = result.Map(() => true).Switch<(string, string)>(
			errors => (errors.system.First(), errors.player.First()),
			_ => ("", "")
		);

		Assert.Equal(("AAA", "BBB"), errors);
	}

	[Fact]
	public void MapOkVoidToAction() {
		var action = Mock.Of<Action>();
		var ok = Result.Ok().Map(action).Switch(
			_ => false,
			() => true
		);

		Assert.True(ok);
		Mock
			.Get(action)
			.Verify(a => a(), Times.Once);
	}

	[Fact]
	public void MapErrorVoidToAction() {
		var action = Mock.Of<Action>();
		var result = (Result)Result.Errors((new SystemError[] { "AAA" }, new PlayerError[] { "aaa" }));
		var errors = result.Map(action).Switch(
			errors => (
				string.Join(", ", errors.system.Select(e => (string)e)),
				string.Join(", ", errors.player.Select(e => (string)e))
			).ToString(),
			() => "no errors"
		);

		Assert.Equal(("AAA", "aaa").ToString(), errors);
		Mock
			.Get(action)
			.Verify(a => a(), Times.Never);
	}

	[Fact]
	public void FlatMapVoidOkayAndMapVoidOkayToOkay() {
		var func = () => Result.Ok();
		var result = Result.Ok();

		var ok = result.FlatMap(func).Switch(
			_ => false,
			() => true
		);

		Assert.True(ok);
	}

	[Fact]
	public void FlatMapVoidOkayAndMapVoidErrorsToErrors() {
		var func = () => (Result)Result.Errors((new SystemError[] { "AAA" }, new PlayerError[] { "aaa" }));
		var result = Result.Ok();

		var errors = result.FlatMap(func).Switch<(string, string)>(
			errors => (errors.system.First(), errors.player.First()),
			() => ("", "")
		);

		Assert.Equal(("AAA", "aaa"), errors);
	}

	[Fact]
	public void FlatMapVoidErrorsAndMapVoidErrorsToErrors() {
		var func = () => (Result)Result.Errors((new SystemError[] { "BBB" }, new PlayerError[] { "bbb" }));
		var result = (Result)Result.Errors((new SystemError[] { "AAA" }, new PlayerError[] { "aaa" }));

		var errors = result.FlatMap(func).Switch<(string, string)>(
			errors => (
				string.Join(", ", errors.system.Select(e => (string)e)),
				string.Join(", ", errors.player.Select(e => (string)e))
			),
			() => ("", "")
		);

		Assert.Equal(("AAA, BBB", "aaa, bbb"), errors);
	}

	[Fact]
	public void FlatMapVoidOkayAndMapOkayToOkay() {
		var func = () => Result.Ok(42);
		var result = Result.Ok();

		var value = result.FlatMap(func).Switch(
			_ => -1,
			v => 42
		);

		Assert.Equal(42, value);
	}

	[Fact]
	public void FlatMapVoidErrorsAndMapOkayToErrors() {
		var func = () => Result.Ok(42);
		var result = (Result)Result.Errors((new SystemError[] { "AAA" }, new PlayerError[] { "aaa" }));

		var errors = result.FlatMap(func).Switch<(string, string)>(
			errors => (errors.system.First(), errors.player.First()),
			v => ("", "")
		);

		Assert.Equal(("AAA", "aaa"), errors);
	}

	[Fact]
	public void FlatMapVoidErrorsAndMapErrorsToErrors() {
		var func = () => (Result<int>)Result.Errors((new SystemError[] { "BBB" }, new PlayerError[] { "bbb" }));
		var result = (Result)Result.Errors((new SystemError[] { "AAA" }, new PlayerError[] { "aaa" }));

		var errors = result.FlatMap(func).Switch<(string, string)>(
			errors => (
				string.Join(", ", errors.system.Select(e => (string)e)),
				string.Join(", ", errors.player.Select(e => (string)e))
			),
			v => ("", "")
		);

		Assert.Equal(("AAA, BBB", "aaa, bbb"), errors);
	}

	[Fact]
	public void FlatMapResultWithValueAndMapOkayToValue() {
		var invert = (int v) => new Result<float>((float)1 / v);
		var result = new Result<int>(42).FlatMap(invert);

		var value = result.Switch(
			error: _ => 0f,
			value: v => v
		);

		Assert.Equal((float)1 / 42, value);
	}

	[Fact]
	public void FlatMapResultWithValueAndMapOkayNonGenericResultOk() {
		var okayFunk = (int v) => Result.Ok();
		var result = new Result<int>(42).FlatMap(okayFunk);

		var ok = result.Switch(
			_ => false,
			() => true
		);

		Assert.True(ok);
	}

	[Fact]
	public void FlatMapResultWithValueAndMapErrorToError() {
		var errorMsg = "non divisible by the answer to the universe and everything";
		var func = (int v) => new Result<float>((Array.Empty<SystemError>(), new PlayerError[] { errorMsg }));
		var result = new Result<int>(42).FlatMap(func);

		var value = result.Switch<string>(
			errors => errors.player.First(),
			_ => "OKAY"
		);

		Assert.Equal(errorMsg, value);
	}

	[Fact]
	public void FlatMapResultWithValueAndMapErrorToNonGenericResultError() {
		var errorMsg = "non divisible by the answer to the universe and everything";
		var func = (int v) => (Result)Result.PlayerError(errorMsg);
		var result = new Result<int>(42).FlatMap(func);

		var value = result.Switch<string>(
			errors => errors.player.First(),
			() => "OKAY"
		);

		Assert.Equal(errorMsg, value);
	}

	[Fact]
	public void FlatMapResultWithErrorToError() {
		var func = (int v) => new Result<float>((float)1 / v);
		var result = new Result<int>((new SystemError[] { "ERROR" }, Array.Empty<PlayerError>())).FlatMap(func);

		var value = result.Switch<string>(
			errors => errors.system.First(),
			value: _ => "OKAY"
		);

		Assert.Equal("ERROR", value);
	}

	[Fact]
	public void FlatMapResultWithErrorToNonGenericResultError() {
		var func = (int v) => Result.Ok();
		var result = new Result<int>((new SystemError[] { "ERROR" }, Array.Empty<PlayerError>())).FlatMap(func);

		var value = result.Switch<string>(
			errors => errors.system.First(),
			() => "OKAY"
		);

		Assert.Equal("ERROR", value);
	}

	[Fact]
	public void Flatten() {
		var value = new Result<int>(42);
		var nested = new Result<Result<int>>(value);

		Assert.Equal(42, nested.Flatten().UnpackOr(-1));
	}

	[Fact]
	public void FlattenWithNonGenericError() {
		var value = Result.Ok();
		var nested = new Result<Result>(value);
		var ok = nested.Flatten().Switch(_ => false, () => true);

		Assert.True(ok);
	}

	[Fact]
	public async Task FlattenResultWithTaskWithVoidResultOkay() {
		var result = Result.Ok(Task.FromResult(Result.Ok()));
		var flat = await result.Flatten();
		var ok = flat.Switch(
			_ => false,
			() => true
		);

		Assert.True(ok);
	}

	[Fact]
	public async Task FlattenResultErrorsWithTaskWithVoidResult() {
		Result<Task<Result>> result = Result.PlayerError("AAA");
		var flat = await result.Flatten();
		var errors = flat.Switch(
			errors => (string)errors.player.FirstOrDefault(),
			() => "no errors"
		);

		Assert.Equal("AAA", errors);
	}

	[Fact]
	public async Task FlattenResultWithTaskWithVoidResultErrors() {
		var result = Result.Ok(Task.FromResult<Result>(Result.PlayerError("AAA")));
		var flat = await result.Flatten();
		var errors = flat.Switch(
			errors => (string)errors.player.FirstOrDefault(),
			() => "no errors"
		);

		Assert.Equal("AAA", errors);
	}

	[Fact]
	public void UnpackFallbackWhenError() {
		var error = new Result<float>((Array.Empty<SystemError>(), new PlayerError[] { "ERROR" }));
		Assert.Equal(-4.2f, error.UnpackOr(-4.2f));
	}

	[Fact]
	public void UnpackValue() {
		var value = new Result<float>(4.2f);
		Assert.Equal(4.2f, value.UnpackOr(42f));
	}

	[Fact]
	public void ApplyMultipleElementsErrorConcatElementErrors() {
		var fst = new Result<int>(4);
		var snd = new Result<int>((Array.Empty<SystemError>(), new PlayerError[] { "ERROR 2" }));
		var trd = new Result<int>((new SystemError[] { "ERROR 3" }, Array.Empty<PlayerError>()));

		var sum = new Result<Func<int, Func<int, Func<int, int>>>>(
			(int a) => (int b) => (int c) => a + b + c
		);

		var result = sum
			.Apply(fst)
			.Apply(snd)
			.Apply(trd);

		var errors = result.Switch(
			errors => $"{(string)errors.system.FirstOrDefault()}, {(string)errors.player.FirstOrDefault()}",
			v => "no errors"
		);
		Assert.Equal("ERROR 3, ERROR 2", errors);
	}

	[Fact]
	public void ApplyOnFunc() {
		var fst = new Result<int>(4);
		var snd = new Result<int>(10);
		var trd = new Result<int>(11);

		var sum = (int a) => (int b) => (int c) => a + b + c;
		var result = sum
			.Apply(fst)
			.Apply(snd)
			.Apply(trd);

		Assert.Equal(25, result.UnpackOr(-1));
	}

	[Fact]
	public void ApplyOnFuncErrors() {
		var fst = new Result<int>((Array.Empty<SystemError>(), new PlayerError[] { "error fst" }));
		var snd = new Result<int>(10);
		var trd = new Result<int>((new SystemError[] { "error trd" }, Array.Empty<PlayerError>()));

		var sum = (int a) => (int b) => (int c) => a + b + c;
		var result = sum
			.Apply(fst)
			.Apply(snd)
			.Apply(trd);

		var errors = result.Switch(
			errors => $"{(string)errors.system.FirstOrDefault()}, {(string)errors.player.FirstOrDefault()}",
			v => "no errors"
		);
		Assert.Equal("error trd, error fst", errors);
	}

	[Fact]
	public void ValueToSome() {
		var value = new Result<int>(42);
		var some = value.ToMaybe();

		Assert.Equal(42, some.UnpackOr(-1));
	}

	[Fact]
	public void PlayerErrorToNone() {
		var value = new Result<int>((Array.Empty<SystemError>(), Array.Empty<PlayerError>()));
		var some = value.ToMaybe();

		Assert.Equal(-1, some.UnpackOr(-1));
	}

	[Fact]
	public void SystemErrorToNone() {
		var value = new Result<int>((Array.Empty<SystemError>(), Array.Empty<PlayerError>()));
		var some = value.ToMaybe();

		Assert.Equal(-1, some.UnpackOr(-1));
	}

	[Fact]
	public void OkOrPlayerErrorOkFromRefType() {
		var value = "Hello";
		value.OkOrPlayerError("error").Switch(
			e => Assert.Fail($"Was {string.Join(", ", e)}, but should have been {value}"),
			v => Assert.Same(value, v)
		);
	}

	[Fact]
	public void OkOrPlayerErrorOkFromOptionalValueType() {
		int? value = 42;
		value.OkOrPlayerError("error").Switch(
			e => Assert.Fail($"Was {string.Join(", ", e)}, but should have been {value}"),
			v => Assert.Equal(value, v)
		);
	}

	[Fact]
	public void OkOrPlayerErrorOptionalRefType() {
		var value = null as string;
		value.OkOrPlayerError("error").Switch(
			e => Assert.Equal("error", (string)e.player.First()),
			v => Assert.Fail($"Was {v ?? "null"}, but should have been error")
		);
	}

	[Fact]
	public void OkOrPlayerErrorOptionalValueType() {
		int? value = null;
		value.OkOrPlayerError("error").Switch(
			e => Assert.Equal("error", (string)e.player.First()),
			v => Assert.Fail($"Was {v}, but should have been no number")
		);
	}

	[Fact]
	public void OkOrSystemErrorOkFromRefType() {
		var value = "Hello";
		value.OkOrSystemError("error").Switch(
			e => Assert.Fail($"Was {string.Join(", ", e)}, but should have been {value}"),
			v => Assert.Same(value, v)
		);
	}

	[Fact]
	public void OkOrSystemErrorOkFromOptionalValueType() {
		int? value = 42;
		value.OkOrSystemError("error").Switch(
			e => Assert.Fail($"Was {string.Join(", ", e)}, but should have been {value}"),
			v => Assert.Equal(value, v)
		);
	}

	[Fact]
	public void OkOrSystemErrorOptionalRefType() {
		var value = null as string;
		value.OkOrSystemError("error").Switch(
			e => Assert.Equal("error", (string)e.system.First()),
			v => Assert.Fail($"Was {v ?? "null"}, but should have been error")
		);
	}

	[Fact]
	public void OkOrSystemErrorOptionalValueType() {
		int? value = null;
		value.OkOrSystemError("error").Switch(
			e => Assert.Equal("error", (string)e.system.First()),
			v => Assert.Fail($"Was {v}, but should have been no number")
		);
	}
}

public class TestResultInstantiateMethods {
	[Fact]
	public void Ok() {
		var result = Result.Ok(42);

		Assert.Equal(42, result.UnpackOr(-1));
	}

	[Fact]
	public void PlayerError() {
		Result<int> result = Result.PlayerError("error");

		var error = result.Switch<string>(
			errors => errors.player.First(),
			_ => "no error"
		);
		Assert.Equal("error", error);
	}

	[Fact]
	public void SystemError() {
		Result<int> result = Result.SystemError("error");

		var error = result.Switch<string>(
			errors => errors.system.First(),
			_ => "no error"
		);
		Assert.Equal("error", error);
	}

	[Fact]
	public void Error() {
		Result<int> result = Result.Error((SystemError)"error");

		var error = result.Switch<string>(
			errors => errors.system.First(),
			_ => "no error"
		);
		Assert.Equal("error", error);
	}

	[Fact]
	public void SystemErrors() {
		var errors = (new SystemError[] { "sError" }, new PlayerError[] { "pError" });
		Result<int> result = Result.Errors(errors);

		var rErrors = result.Switch(
			errors => errors,
			_ => (Array.Empty<SystemError>(), Array.Empty<PlayerError>())
		);
		Assert.Equal(errors, rErrors);
	}
}
