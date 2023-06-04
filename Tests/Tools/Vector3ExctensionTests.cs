namespace Tests;

using ProjectZyheeda;
using Stride.Core.Mathematics;
using Xunit;

public class Vector3ExtensionsTests : GameTestCollection {
	public Vector3ExtensionsTests(GameFixture fixture) : base(fixture) { }

	[Fact]
	public void MoveTowardsExactly() {
		var vector = new Vector3(1, 0, 0);

		vector = vector.MoveTowards(new Vector3(2, 0, 0), 1);

		Assert.Equal(new Vector3(2, 0, 0), vector);
	}

	[Fact]
	public void MoveTowardsPartly() {
		var vector = new Vector3(1, 2, 0);

		vector = vector.MoveTowards(new Vector3(7, 10, 0), 5);

		Assert.Equal(new Vector3(4, 6, 0), vector);
	}

	[Fact]
	public void MoveTowardsDoNotOvershoot() {
		var vector = new Vector3(1, 2, 0);

		vector = vector.MoveTowards(new Vector3(7, 10, 0), 100);

		Assert.Equal(new Vector3(7, 10, 0), vector);
	}
}
