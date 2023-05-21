namespace Tests;

using Moq;
using NUnit.Framework;
using ProjectZyheeda;
using Stride.Engine;

public class ReferenceTest : GameTestCollection {
	public interface IMock { }

	[Test]
	public void ApplyNoneWhenEntityNull() {
		var reference = new Reference<Entity> { Entity = null };
		var value = reference.Switch<Entity?>(
			some: c => c,
			none: () => null
		);
		Assert.That(value, Is.Null);
	}

	[Test]
	public void ApplySomeWhenEntityWithIMock() {
		var mComponent = new Mock<EntityComponent>().As<IMock>();
		var entity = new Entity { (EntityComponent)mComponent.Object };
		var reference = new Reference<IMock> { Entity = entity };
		var value = reference.Switch<IMock?>(
			some: c => c,
			none: () => null
		);
		Assert.That(value, Is.SameAs(mComponent.Object));
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
		var mComponent = new Mock<EntityComponent>().As<IMock>();
		var entity = new Entity { (EntityComponent)mComponent.Object };
		var reference = new Reference<IMock> { Entity = entity };
		reference.Entity = null;
		Assert.That(reference.Entity, Is.Null);
	}

	[Test]
	public void DonNotResetEntityWhenAssigningEntityWithoutIMock() {
		var mComponent = new Mock<EntityComponent>().As<IMock>();
		var entity = new Entity { (EntityComponent)mComponent.Object };
		var reference = new Reference<IMock> { Entity = entity };
		reference.Entity = new Entity();
		Assert.That(reference.Entity, Is.SameAs(entity));
	}

	[Test]
	public void ApplyNoneWhenAssigningEntityWithoutIMock() {
		var reference = new Reference<IMock> { Entity = new Entity() };
		var value = reference.Switch<IMock?>(
			some: c => c,
			none: () => null
		);
		Assert.Multiple(() => {
			Assert.That(value, Is.Null);
			Assert.That(reference.Entity, Is.Null);
		});
	}

	private class MockComponent : StartupScript, IMock { }

	private class MockReference : Reference<MockComponent, IMock> { }

	[Test]
	public void None() {
		var fallback = new MockComponent();
		var reference = new MockReference();
		Assert.That(reference.UnpackOr(fallback), Is.SameAs(fallback));
	}

	[Test]
	public void Some() {
		var fallback = new MockComponent();
		var reference = new MockReference {
			target = new(),
		};
		Assert.That(reference.UnpackOr(fallback), Is.SameAs(reference.target));
	}
}
