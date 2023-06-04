namespace Tests;
using ProjectZyheeda;
using Xunit;

public class FunctionalTests : GameTestCollection {
	public FunctionalTests(GameFixture fixture) : base(fixture) { }

	[Fact]
	public void Apply() {
		var stringify = (int v) => v.ToString();

		var source = 42;
		var parsed = source.Apply(stringify);

		Assert.Equal("42", parsed);
	}
}
