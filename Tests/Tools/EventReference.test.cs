namespace Tests;

using System;
using NUnit.Framework;
using ProjectZyheeda;
using Stride.Engine;

public class EventReferenceTests : GameTestCollection {
	private class MockWrapped<T> : IReference, IMaybe<T> {
		public Func<Func<T, object>, Func<object>, object> apply = (_, none) => none();
		public Action<Entity?> entitySet = _ => { };
		public Func<Entity?> entityGet = () => null;

		public Entity? Entity {
			get => this.entityGet();
			set => this.entitySet(value);
		}

		public TReturn Switch<TReturn>(Func<T, TReturn> some, Func<TReturn> none) {
			return (TReturn)this.apply(
				v => some(v)!,
				() => none()!
			);
		}
	}

	[Test]
	public void UseWrappedApplySome() {
		var wrapped = new MockWrapped<int> { apply = (some, _) => some(42), };
		var reference = new EventReference<MockWrapped<int>, int>(wrapped);

		var value = reference.Switch(v => v, () => -1);
		Assert.That(value, Is.EqualTo(42));
	}

	[Test]
	public void UseWrappedApplyNone() {
		var wrapped = new MockWrapped<int> { apply = (_, none) => none(), };
		var reference = new EventReference<MockWrapped<int>, int>(wrapped);

		var value = reference.Switch(v => v, () => -1);
		Assert.That(value, Is.EqualTo(-1));
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
