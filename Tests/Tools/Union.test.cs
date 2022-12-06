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
	public void Union1of2Expand() {
		var union = Union.New<int, float>(42).Expand<int, float, string>();
		var value = union.Switch(
			(int v) => v,
			(float _) => -1,
			(string _) => -1
		);
		Assert.That(value, Is.EqualTo(42));
	}

	[Test]
	public void Union2of2Expand() {
		var union = Union.New<int, float>(4.2f).Expand<int, float, string>();
		var value = union.Switch(
			(int _) => -1f,
			(float v) => v,
			(string _) => -1f
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

public class TestU2 : GameTestCollection {
	[Test]
	public void SwitchFirst() {
		var union = new U<int, string>(42);
		var result = union.Switch<int?>(v => v, _ => null);
		Assert.That(result, Is.EqualTo(42));
	}

	[Test]
	public void SwitchSecond() {
		var union = new U<int, string>("42");
		var result = union.Switch<string?>(v => null, v => v);
		Assert.That(result, Is.EqualTo("42"));
	}

	[Test]
	public void CastFromFstValue() {
		U<int, string> union = 42;
		Assert.That(
			union.Switch<int?>(v => v, _ => null),
			Is.EqualTo(42)
		);
	}

	[Test]
	public void CastFromSndValue() {
		U<int, string> union = "42";
		Assert.That(
			union.Switch<string?>(_ => null, v => v),
			Is.EqualTo("42")
		);
	}

	[Test]
	public void CastSwap() {
		U<string, int> union = new U<int, string>(42);
		Assert.That(
			union.Switch<int?>(_ => null, v => v),
			Is.EqualTo(42)
		);

		union = new U<int, string>("42");
		Assert.That(
			union.Switch<string?>(v => v, _ => null),
			Is.EqualTo("42")
		);
	}
}

public class TestU3 : GameTestCollection {
	[Test]
	public void SwitchFirst() {
		var union = new U<int, string, float>(42);
		var result = union.Switch<int?>(v => v, _ => null, _ => null);
		Assert.That(result, Is.EqualTo(42));
	}

	[Test]
	public void SwitchSecond() {
		var union = new U<int, string, float>("42");
		var result = union.Switch<string?>(_ => null, v => v, _ => null);
		Assert.That(result, Is.EqualTo("42"));
	}


	[Test]
	public void SwitchThird() {
		var union = new U<int, string, float>(42f);
		var result = union.Switch<float?>(_ => null, _ => null, v => v);
		Assert.That(result, Is.EqualTo(42f));
	}

	[Test]
	public void CastFromFstValue() {
		U<int, string, float> union = 42;
		Assert.That(
			union.Switch<int?>(v => v, _ => null, _ => null),
			Is.EqualTo(42)
		);
	}

	[Test]
	public void CastFromSndValue() {
		U<int, string, float> union = "42";
		Assert.That(
			union.Switch<string?>(_ => null, v => v, _ => null),
			Is.EqualTo("42")
		);
	}

	[Test]
	public void CastFromTrdValue() {
		U<int, string, float> union = 42f;
		Assert.That(
			union.Switch<float?>(_ => null, _ => null, v => v),
			Is.EqualTo(42f)
		);
	}

	// U2 to U3 cast permutations
	// 1    2    3
	// 2 3  1 3  1 2
	// 3 2  3 1  2 1
	// x x  x x  x x

	[Test]
	public void Cast123FromU2() {
		U<int, string, float> union = new U<int, string>(42);
		Assert.That(
			union.Switch<int?>(v => v, _ => null, _ => null),
			Is.EqualTo(42)
		);

		union = new U<int, string>("42");
		Assert.That(
			union.Switch<string?>(_ => null, v => v, _ => null),
			Is.EqualTo("42")
		);
	}

	[Test]
	public void Cast132FromU2() {
		U<int, float, string> union = new U<int, string>(42);
		Assert.That(
			union.Switch<int?>(v => v, _ => null, _ => null),
			Is.EqualTo(42)
		);

		union = new U<int, string>("42");
		Assert.That(
			union.Switch<string?>(_ => null, _ => null, v => v),
			Is.EqualTo("42")
		);
	}



	[Test]
	public void Cast213FromU2() {
		U<string, int, float> union = new U<int, string>(42);
		Assert.That(
			union.Switch<int?>(_ => null, v => v, _ => null),
			Is.EqualTo(42)
		);

		union = new U<int, string>("42");
		Assert.That(
			union.Switch<string?>(v => v, _ => null, _ => null),
			Is.EqualTo("42")
		);
	}

	[Test]
	public void Cast231FromU2() {
		U<string, float, int> union = new U<int, string>(42);
		Assert.That(
			union.Switch<int?>(_ => null, _ => null, v => v),
			Is.EqualTo(42)
		);

		union = new U<int, string>("42");
		Assert.That(
			union.Switch<string?>(v => v, _ => null, _ => null),
			Is.EqualTo("42")
		);
	}

	[Test]
	public void Cast312FromU2() {
		U<float, int, string> union = new U<int, string>(42);
		Assert.That(
			union.Switch<int?>(_ => null, v => v, _ => null),
			Is.EqualTo(42)
		);

		union = new U<int, string>("42");
		Assert.That(
			union.Switch<string?>(_ => null, _ => null, v => v),
			Is.EqualTo("42")
		);
	}

	[Test]
	public void Cast321FromU2() {
		U<float, string, int> union = new U<int, string>(42);
		Assert.That(
			union.Switch<int?>(_ => null, _ => null, v => v),
			Is.EqualTo(42)
		);

		union = new U<int, string>("42");
		Assert.That(
			union.Switch<string?>(_ => null, v => v, _ => null),
			Is.EqualTo("42")
		);
	}

	// U3 to U3 cast permutations
	// 1    2    3
	// 2 3  1 3  2 1
	// 3 2  3 1  1 2
	// d x  x x  x x

	[Test]
	public void Cast132() {
		U<int, float, string> union = new U<int, string, float>(42);
		Assert.That(
			union.Switch<int?>(v => v, _ => null, _ => null),
			Is.EqualTo(42)
		);

		union = new U<int, string, float>("42");
		Assert.That(
			union.Switch<string?>(_ => null, _ => null, v => v),
			Is.EqualTo("42")
		);

		union = new U<int, string, float>(42f);
		Assert.That(
			union.Switch<float?>(_ => null, v => v, _ => null),
			Is.EqualTo(42f)
		);
	}


	[Test]
	public void Cast213() {
		U<string, int, float> union = new U<int, string, float>(42);
		Assert.That(
			union.Switch<int?>(_ => null, v => v, _ => null),
			Is.EqualTo(42)
		);

		union = new U<int, string, float>("42");
		Assert.That(
			union.Switch<string?>(v => v, _ => null, _ => null),
			Is.EqualTo("42")
		);

		union = new U<int, string, float>(42f);
		Assert.That(
			union.Switch<float?>(_ => null, _ => null, v => v),
			Is.EqualTo(42f)
		);
	}

	[Test]
	public void Cast231() {
		U<string, float, int> union = new U<int, string, float>(42);
		Assert.That(
			union.Switch<int?>(_ => null, _ => null, v => v),
			Is.EqualTo(42)
		);

		union = new U<int, string, float>("42");
		Assert.That(
			union.Switch<string?>(v => v, _ => null, _ => null),
			Is.EqualTo("42")
		);

		union = new U<int, string, float>(42f);
		Assert.That(
			union.Switch<float?>(_ => null, v => v, _ => null),
			Is.EqualTo(42f)
		);
	}

	[Test]
	public void Cast321() {
		U<float, string, int> union = new U<int, string, float>(42);
		Assert.That(
			union.Switch<int?>(_ => null, _ => null, v => v),
			Is.EqualTo(42)
		);

		union = new U<int, string, float>("42");
		Assert.That(
			union.Switch<string?>(_ => null, v => v, _ => null),
			Is.EqualTo("42")
		);

		union = new U<int, string, float>(42f);
		Assert.That(
			union.Switch<float?>(v => v, _ => null, _ => null),
			Is.EqualTo(42f)
		);
	}

	[Test]
	public void Cast312() {
		U<float, int, string> union = new U<int, string, float>(42);
		Assert.That(
			union.Switch<int?>(_ => null, v => v, _ => null),
			Is.EqualTo(42)
		);

		union = new U<int, string, float>("42");
		Assert.That(
			union.Switch<string?>(_ => null, _ => null, v => v),
			Is.EqualTo("42")
		);

		union = new U<int, string, float>(42f);
		Assert.That(
			union.Switch<float?>(v => v, _ => null, _ => null),
			Is.EqualTo(42f)
		);
	}
}
