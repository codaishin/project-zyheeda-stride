namespace Tests;

using NUnit.Framework;
using ProjectZyheeda;
using Stride.Core.Mathematics;
using Stride.Engine;

public class TestBehaviorTargetExtensions : GameTestCollection {
	[Test]
	public void GetTargetVector() {
		var target = new U<Vector3, Entity>(new Vector3(1, 2, 3));

		Assert.That(target.Position(), Is.EqualTo(new Vector3(1, 2, 3)));
	}

	[Test]
	public void GetTargetEntityPositionVector() {
		var entity = new Entity();
		var target = new U<Vector3, Entity>(entity);

		entity.Transform.Position = new Vector3(1, 2, 3);

		Assert.That(target.Position(), Is.EqualTo(new Vector3(1, 2, 3)));
	}
}
