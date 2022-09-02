namespace Tests;

using System;
using NUnit.Framework;
using ProjectZyheeda;
using Stride.Engine;

public class EventReferenceTests : GameTestCollection {
	private class MockWrapped<T> : IReference, IMaybe<T> {
		public Action<Action<T>, Action?> match = (_, __) => { };
		public Action<Entity?> entitySet = _ => { };
		public Func<Entity?> entityGet = () => null;

		public Entity? Entity {
			get => this.entityGet();
			set => this.entitySet(value);
		}

		public void Match(Action<T> some, Action? none = null) {
			this.match(some, none);
		}
	}

	[Test]
	public void UseWrappedMatchSome() {
		var (calledSome, calledNone) = (0, 0);
		var wrapped = new MockWrapped<int> { match = (some, _) => some(42), };
		var reference = new EventReference<MockWrapped<int>, int>(wrapped);

		reference.Match(v => calledSome += v, () => ++calledNone);
		Assert.That((calledSome, calledNone), Is.EqualTo((42, 0)));
	}

	[Test]
	public void UseWrappedMatchNone() {
		var (calledSome, calledNone) = (0, 0);
		var wrapped = new MockWrapped<int> { match = (_, none) => none?.Invoke(), };
		var reference = new EventReference<MockWrapped<int>, int>(wrapped);

		reference.Match(v => calledSome += v, () => ++calledNone);
		Assert.That((calledSome, calledNone), Is.EqualTo((0, 1)));
	}

	[Test]
	public void UseWrappedEntitySet() {
		var gotEntity = null as Entity;
		var expectedEntity = new Entity();
		var wrapped = new MockWrapped<int> { entitySet = v => gotEntity = v };
		var reference = new EventReference<MockWrapped<int>, int>(wrapped) {
			Entity = expectedEntity
		};

		Assert.That(gotEntity, Is.SameAs(expectedEntity));
	}

	[Test]
	public void UseWrappedEntityGet() {
		var expectedEntity = new Entity();
		var wrapped = new MockWrapped<int> { entityGet = () => expectedEntity };
		var reference = new EventReference<MockWrapped<int>, int>(wrapped);

		Assert.That(reference.Entity, Is.SameAs(expectedEntity));
	}

	[Test]
	public void SendWrappedToEvents() {
		var called = 0;
		var wrapped = new MockWrapped<int>();
		var reference = new EventReference<MockWrapped<int>, int>(
			wrapped,
			() => ++called
		) {
			Entity = new Entity()
		};

		Assert.That(called, Is.EqualTo(1));
	}
}
