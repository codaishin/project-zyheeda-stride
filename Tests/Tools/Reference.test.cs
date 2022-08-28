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
	public void MatchNoneWhenEntityNull() {
		var reference = new Reference<IMock> { Entity = null };
		var (callSome, callNone) = (0, 0);
		reference.Match(
			some: _ => ++callSome,
			none: () => ++callNone
		);
		Assert.That((callSome, callNone), Is.EqualTo((0, 1)));
	}

	[Test]
	public void MatchSomeWhenEntityWithIMock() {
		var entity = new Entity();
		var component = new MockComponent();
		entity.Add(component);
		var reference = new Reference<IMock> { Entity = entity };
		var (callSome, callNone) = (0, 0);
		reference.Match(
			some: c => {
				++callSome;
				Assert.That(c, Is.SameAs(component));
			},
			none: () => ++callNone
		);
		Assert.That((callSome, callNone), Is.EqualTo((1, 0)));
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
	public void MatchNoneWhenEntityWithoutIMock() {
		var reference = new Reference<IMock> { Entity = new Entity() };
		var (callSome, callNone) = (0, 0);
		reference.Match(
			some: c => ++callSome,
			none: () => ++callNone
		);
		Assert.Multiple(() => {
			Assert.That((callSome, callNone), Is.EqualTo((0, 1)));
			Assert.That(reference.Entity, Is.Null);
		});
	}
}
