namespace Tests;

using NUnit.Framework;
using ProjectZyheeda;
using Stride.Core.Mathematics;

public class Vector3ExtensionsTests : GameTestCollection {
	[Test]
	public void MoveTowardsExactly() {
		var vector = new Vector3(1, 0, 0);

		vector = vector.MoveTowards(new Vector3(2, 0, 0), 1);

		Assert.That(vector, Is.EqualTo(new Vector3(2, 0, 0)));
	}

	[Test]
	public void MoveTowardsPartly() {
		var vector = new Vector3(1, 2, 0);

		vector = vector.MoveTowards(new Vector3(7, 10, 0), 5);

		Assert.That(vector, Is.EqualTo(new Vector3(4, 6, 0)));
	}

	[Test]
	public void MoveTowardsDoNotOvershoot() {
		var vector = new Vector3(1, 2, 0);

		vector = vector.MoveTowards(new Vector3(7, 10, 0), 100);

		Assert.That(vector, Is.EqualTo(new Vector3(7, 10, 0)));
	}
}
