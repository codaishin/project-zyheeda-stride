namespace Tests;

using System.Linq;
using ProjectZyheeda;
using Xunit;

public class ReferenceTests {
	private class StringReference : Reference<string> {
		public string? Target {
			get => this.GetRef();
			set => this.SetRef(value);
		}

		public Result<string> ExposedInnerReference => this.target;
	}

	[Fact]
	public void Ok() {
		var reference = new StringReference { Target = "42" };

		Assert.Equal("42", reference.ExposedInnerReference.UnpackOr("-1"));
	}

	[Fact]
	public void NotSet() {
		var reference = new StringReference();
		var errors = reference.ExposedInnerReference.Switch(
			errors => (
				string.Join(", ", errors.system.Select(e => (string)e)),
				string.Join(", ", errors.player.Select(e => (string)e))
			).ToString(),
			_ => "no errors"
		);

		Assert.Equal((reference.MissingField("target"), "").ToString(), errors);
	}

	[Fact]
	public void SetNull() {
		var reference = new StringReference { Target = null };
		var errors = reference.ExposedInnerReference.Switch(
			errors => (
				string.Join(", ", errors.system.Select(e => (string)e)),
				string.Join(", ", errors.player.Select(e => (string)e))
			).ToString(),
			_ => "no errors"
		);

		Assert.Equal((reference.MissingField("target"), "").ToString(), errors);
	}
}
