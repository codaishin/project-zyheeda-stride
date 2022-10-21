namespace Tests;
using NUnit.Framework;
using ProjectZyheeda;

public class FunctionalTests : GameTestCollection {
	[Test]
	public void Apply() {
		var stringify = (int v) => v.ToString();

		var source = 42;
		var parsed = source.Apply(stringify);

		Assert.That(parsed, Is.EqualTo("42"));
	}
}
