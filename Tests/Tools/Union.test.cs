namespace Tests;

using NUnit.Framework;
using ProjectZyheeda;

public class TestUnion : GameTestCollection {
	[Test]
	public void Union1of2() {
		var union = Union.New<int, float>(42);
		var value = union.Switch(
			(int v) => v,
			(float _) => -1
		);
		Assert.That(value, Is.EqualTo(42));
	}

	[Test]
	public void Union2of2() {
		var union = Union.New<int, float>(4.2f);
		var value = union.Switch(
			(int _) => -1f,
			(float v) => v
		);
		Assert.That(value, Is.EqualTo(4.2f));
	}

	[Test]
	public void Union1of3() {
		var union = Union.New<int, float, string>(42);
		var value = union.Switch(
			(int v) => v,
			(float _) => -1,
			(string _) => -1
		);
		Assert.That(value, Is.EqualTo(42));
	}

	[Test]
	public void Union2of3() {
		var union = Union.New<int, float, string>(4.2f);
		var value = union.Switch(
			(int _) => -1f,
			(float v) => v,
			(string _) => -1f
		);
		Assert.That(value, Is.EqualTo(4.2f));
	}

	[Test]
	public void Union3of3() {
		var union = Union.New<int, float, string>("42");
		var value = union.Switch(
			(int _) => "",
			(float _) => "",
			(string v) => v
		);
		Assert.That(value, Is.EqualTo("42"));
	}
}
