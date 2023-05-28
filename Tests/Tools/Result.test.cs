namespace Tests;

using System;
using System.Collections.Generic;
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
		var result = new Result<string>((Array.Empty<SystemError>(), new PlayerError[] { "AAA" })).Switch<string>(
			errors => errors.player.First(),
			_ => "BBB"
		);
		Assert.That(result, Is.EqualTo("AAA"));
	}

	[Test]
	public void SwitchSystemError() {
		var result = new Result<string>((new SystemError[] { "AAA" }, Array.Empty<PlayerError>())).Switch<string>(
			errors => errors.system.First(),
			_ => "BBB"
		);
		Assert.That(result, Is.EqualTo("AAA"));
	}

	[Test]
	public void SwitchErrors() {
		(IEnumerable<SystemError>, IEnumerable<PlayerError>) errors = (new SystemError[] { "AAA" }, new PlayerError[] { "BBB" });
		var result = new Result<string>(errors).Switch(
			errors => errors,
			_ => (Array.Empty<SystemError>(), Array.Empty<PlayerError>())
		);
		Assert.That(result, Is.EqualTo(errors));
	}
}
