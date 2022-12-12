namespace Tests;

using System;
using Moq;
using NUnit.Framework;
using ProjectZyheeda;
using Stride.Engine;

public class EventReferenceTests : GameTestCollection {
	public interface IMockReference<T> : IReference, IMaybe<T> { }

	[Test]
	public void UseWrappedApplySome() {
		var getSome = (int v) => v;
		var wrapped = Mock.Of<IMockReference<int>>(
			r => r.Switch(getSome, It.IsAny<Func<int>>()) == 42
		);
		var reference = new EventReference<IMockReference<int>, int>(wrapped);

		var value = reference.Switch(getSome, () => -1);
		Assert.That(value, Is.EqualTo(42));
	}

	[Test]
	public void UseWrappedApplyNone() {
		var getNone = () => 42;
		var wrapped = Mock.Of<IMockReference<int>>(
			r => r.Switch(It.IsAny<Func<int, int>>(), getNone) == 42
		);
		var reference = new EventReference<IMockReference<int>, int>(wrapped);

		var value = reference.Switch(v => -1, getNone);
		Assert.That(value, Is.EqualTo(42));
	}

	[Test]
	public void UseWrappedEntitySet() {
		var entity = new Entity();
		var wrapped = Mock.Of<IMockReference<int>>();
		var reference = new EventReference<IMockReference<int>, int>(wrapped) {
			Entity = entity
		};

		Mock.Get(wrapped).VerifySet(r => r.Entity = entity);
	}

	[Test]
	public void UseWrappedEntityGet() {
		var entity = new Entity();
		var wrapped = Mock.Of<IMockReference<int>>();
		var reference = new EventReference<IMockReference<int>, int>(wrapped);

		_ = reference.Entity;
		Mock.Get(wrapped).VerifyGet(r => r.Entity);
	}

	[Test]
	public void SendWrappedToEvents() {
		var wrapped = Mock.Of<IMockReference<int>>();
		var action = Mock.Of<Action>();
		var reference = new EventReference<IMockReference<int>, int>(wrapped, action) {
			Entity = new Entity()
		};

		Mock.Get(action).Verify(a => a(), Times.Once());
	}
}
