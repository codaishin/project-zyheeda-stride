namespace Tests;

using NUnit.Framework;
using ProjectZyheeda;
using Stride.Engine;

public class ReferenceTest : GameTestCollection {

	private interface IMock { }

	private class MockComponent : SyncScript, IMock {
		public override void Update() { }
	}

	[Test]
	public void ApplyNoneWhenEntityNull() {
		var reference = new Reference<IMock> { Entity = null };
		var nullComponent = new MockComponent();
		var value = reference.Switch(
			some: c => c,
			none: () => nullComponent
		);
		Assert.That(value, Is.SameAs(nullComponent));
	}

	[Test]
	public void ApplySomeWhenEntityWithIMock() {
		var entity = new Entity();
		var component = new MockComponent();
		entity.Add(component);
		var reference = new Reference<IMock> { Entity = entity };
		var value = reference.Switch(
			some: c => c,
			none: () => new MockComponent()
		);
		Assert.That(value, Is.SameAs(component));
	}

	[Test]
	public void ApplySomeWhenEntity() {
		var entity = new Entity();
		var reference = new Reference<Entity> { Entity = entity };
		var value = reference.Switch(
			some: e => e,
			none: () => new Entity()
		);
		Assert.That(value, Is.SameAs(entity));
	}

	[Test]
	public void EntityResetWhenAssigningNull() {
		var entity = new Entity();
		var component = new MockComponent();
		entity.Add(component);
		var reference = new Reference<IMock> { Entity = entity };
		reference.Entity = null;
		Assert.That(reference.Entity, Is.Null);
	}

	[Test]
	public void DonNotResetEntityWhenEntityWithoutIMock() {
		var entity = new Entity();
		var component = new MockComponent();
		entity.Add(component);
		var reference = new Reference<IMock> { Entity = entity };
		reference.Entity = new Entity();
		Assert.That(reference.Entity, Is.SameAs(entity));
	}

	[Test]
	public void ApplyNoneWhenEntityWithoutIMock() {
		var reference = new Reference<IMock> { Entity = new Entity() };
		var nullComponent = new MockComponent();
		var value = reference.Switch(
			some: c => c,
			none: () => nullComponent
		);
		Assert.Multiple(() => {
			Assert.That(value, Is.SameAs(nullComponent));
			Assert.That(reference.Entity, Is.Null);
		});
	}
}
