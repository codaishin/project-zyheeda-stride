namespace Tests;

using System;
using System.Collections.Generic;
using System.Linq;
using ProjectZyheeda;
using Xunit;

public class TestResult {
	[Fact]
	public void SwitchOk() {
		var msg = Result.Ok().Switch(_ => "errors", () => "ok");
		Assert.Equal("ok", msg);
	}

	[Fact]
	public void SwitchError() {
		Result result = Result.PlayerError("error");
		var msg = result.Switch(errors => (string)errors.player.First(), () => "ok");
		Assert.Equal("error", msg);
	}
}

public class TestGenericResult {
	[Fact]
	public void SwitchValue() {
		var either = new Result<string>("42");
		var result = either.Switch(
			errors => "-1",
			value => value
		);
		Assert.Equal("42", result);
	}

	[Fact]
	public void SwitchPlayerError() {
		var result = new Result<string>((Array.Empty<SystemError>(), new PlayerError[] { "AAA" })).Switch<string>(
			errors => errors.player.First(),
			_ => "BBB"
		);
		Assert.Equal("AAA", result);
	}

	[Fact]
	public void SwitchSystemError() {
		var result = new Result<string>((new SystemError[] { "AAA" }, Array.Empty<PlayerError>())).Switch<string>(
			errors => errors.system.First(),
			_ => "BBB"
		);
		Assert.Equal("AAA", result);
	}

	[Fact]
	public void SwitchErrors() {
		(IEnumerable<SystemError>, IEnumerable<PlayerError>) errors = (new SystemError[] { "AAA" }, new PlayerError[] { "BBB" });
		var result = new Result<string>(errors).Switch(
			errors => errors,
			_ => (Array.Empty<SystemError>(), Array.Empty<PlayerError>())
		);
		Assert.Equal(result, errors);
	}

	[Fact]
	public void ImplicitCastOk() {
		Result<int> result = 42;
		result.Switch(
			errors => Assert.Fail(result.UnpackToString()),
			value => Assert.Equal(42, value)
		);
	}

	[Fact]
	public void ImplicitSystemError() {
		Result<int> result = new SystemError("OUCHIE");
		result.Switch(
			errors => Assert.Equal(errors.system.First(), (SystemError)"OUCHIE"),
			value => Assert.Equal(42, value)
		);
	}

	[Fact]
	public void ImplicitPlayerError() {
		Result<int> result = new PlayerError("OUCHIE");
		result.Switch(
			errors => Assert.Equal(errors.player.First(), (PlayerError)"OUCHIE"),
			value => Assert.Equal(42, value)
		);
	}

	[Fact]
	public void ErrorToString() {
		Result result = Result.Errors((new SystemError[] { "A", "B", "C" }, new PlayerError[] { "a", "b", "c" }));
		Assert.Equal("Result(SystemErrors(A, B, C), PlayerErrors(a, b, c))", result.ToString());
	}

	[Fact]
	public void OkToString() {
		var result = Result.Ok();
		Assert.Equal("Result(Ok)", result.ToString());
	}
}
