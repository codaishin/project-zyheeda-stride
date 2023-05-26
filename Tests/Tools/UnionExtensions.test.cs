namespace Tests;

using System;
using Moq;
using NUnit.Framework;
using ProjectZyheeda;

public class TestUnion2Extensions {
	private struct FakeUFst : IUnion<int, string> {
		public TOut Switch<TOut>(Func<int, TOut> fst, Func<string, TOut> snd) {
			return fst(42);
		}
	}

	private struct FakeUSnd : IUnion<int, string> {
		public TOut Switch<TOut>(Func<int, TOut> fst, Func<string, TOut> snd) {
			return snd("42");
		}
	}

	[Test]
	public void SwitchActionFirst() {
		var union = new TestUnion2Extensions.FakeUFst();
		var callback = Mock.Of<Action<int>>();

		union.Switch(callback, _ => { });
		Mock
			.Get(callback)
			.Verify(c => c.Invoke(42), Times.Once);
	}

	[Test]
	public void SwitchActionSecond() {
		var union = new TestUnion2Extensions.FakeUSnd();
		var callback = Mock.Of<Action<string>>();

		union.Switch(_ => { }, callback);
		Mock
			.Get(callback)
			.Verify(c => c.Invoke("42"), Times.Once);
	}

	[Test]
	public void UFstAsString() {
		var union = new TestUnion2Extensions.FakeUFst();
		Assert.That(union.UnionToString(), Is.EqualTo("IUnion<Int32, String>(fst: 42)"));
	}

	[Test]
	public void USndAsString() {
		var union = new TestUnion2Extensions.FakeUSnd();
		Assert.That(union.UnionToString(), Is.EqualTo("IUnion<Int32, String>(snd: 42)"));
	}
}

public class TestUnion3Extensions {
	private struct FakeUFst : IUnion<int, string, bool> {
		public TOut Switch<TOut>(Func<int, TOut> fst, Func<string, TOut> snd, Func<bool, TOut> trd) {
			return fst(42);
		}
	}

	private struct FakeUSnd : IUnion<int, string, bool> {
		public TOut Switch<TOut>(Func<int, TOut> fst, Func<string, TOut> snd, Func<bool, TOut> trd) {
			return snd("42");
		}
	}

	private struct FakeUTrd : IUnion<int, string, bool> {
		public TOut Switch<TOut>(Func<int, TOut> fst, Func<string, TOut> snd, Func<bool, TOut> trd) {
			return trd(true);
		}
	}

	[Test]
	public void SwitchActionFirst() {
		var union = new TestUnion3Extensions.FakeUFst();
		var callback = Mock.Of<Action<int>>();
		union.Switch(callback, _ => { }, _ => { });
		Mock
			.Get(callback)
			.Verify(c => c.Invoke(42), Times.Once);
	}

	[Test]
	public void SwitchActionSecond() {
		var union = new TestUnion3Extensions.FakeUSnd();
		var callback = Mock.Of<Action<string>>();
		union.Switch(_ => { }, callback, _ => { });
		Mock
			.Get(callback)
			.Verify(c => c.Invoke("42"), Times.Once);
	}

	[Test]
	public void SwitchActionThird() {
		var union = new TestUnion3Extensions.FakeUTrd();
		var callback = Mock.Of<Action<bool>>();
		union.Switch(_ => { }, _ => { }, callback);
		Mock
			.Get(callback)
			.Verify(c => c.Invoke(true), Times.Once);
	}

	[Test]
	public void UFstAsString() {
		var union = new TestUnion3Extensions.FakeUFst();
		Assert.That(union.UnionToString(), Is.EqualTo("IUnion<Int32, String, Boolean>(fst: 42)"));
	}

	[Test]
	public void USndAsString() {
		var union = new TestUnion3Extensions.FakeUSnd();
		Assert.That(union.UnionToString(), Is.EqualTo("IUnion<Int32, String, Boolean>(snd: 42)"));
	}

	[Test]
	public void UTrdAsString() {
		var union = new TestUnion3Extensions.FakeUTrd();
		Assert.That(union.UnionToString(), Is.EqualTo("IUnion<Int32, String, Boolean>(trd: True)"));
	}
}
