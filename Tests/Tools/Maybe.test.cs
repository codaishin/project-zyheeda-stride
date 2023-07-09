namespace Tests;

using System.Linq;
using ProjectZyheeda;
using Xunit;

public class TestMaybe {
	[Fact]
	public void SwitchSome() {
		var maybe = Maybe.Some(42);

		var value = maybe.Switch(
			some: v => v,
			none: () => -1
		);

		Assert.Equal(42, value);
	}

	[Fact]
	public void SwitchNone() {
		var maybe = Maybe.None<int>();

		var value = maybe.Switch(
			some: v => v,
			none: () => -1
		);

		Assert.Equal(-1, value);
	}

	[Fact]
	public void MapNoneToNone() {
		var func = (int v) => v.ToString();
		var result = Maybe.None<int>().Map(func);
		var value = result.Switch(
			some: v => v,
			none: () => ""
		);

		Assert.Empty(value);
	}

	[Fact]
	public void MapSomeToSome() {
		var func = (int v) => v.ToString();
		var result = Maybe.Some(42).Map(func);
		var value = result.Switch(
			some: v => v,
			none: () => ""
		);

		Assert.Equal("42", value);
	}

	[Fact]
	public void FlatMapNoneWithNoneToNone() {
		var func = (int v) => Maybe.None<string>();
		var result = Maybe.None<int>().FlatMap(func);
		var value = result.Switch(
			some: v => v,
			none: () => ""
		);

		Assert.Empty(value);
	}

	[Fact]
	public void FlatMapSomeWithSomeToSome() {
		var func = (int v) => Maybe.Some(v.ToString());
		var result = Maybe.Some(42).FlatMap(func);
		var value = result.Switch(
			some: v => v,
			none: () => ""
		);

		Assert.Equal("42", value);
	}

	[Fact]
	public void FlatMapSelf() {
		var some = Maybe.Some(42);
		var nested = Maybe.Some(some);

		Assert.Same(some, nested.Flatten());
	}

	[Fact]
	public void UnpackFallbackWhenNone() {
		var none = Maybe.None<float>();
		Assert.Equal(42f, none.UnpackOr(42f));
	}

	[Fact]
	public void UnpackSome() {
		var some = Maybe.Some(4.2f);
		Assert.Equal(4.2f, some.UnpackOr(42f));
	}

	[Fact]
	public void SwitchSomeAction() {
		var some = Maybe.Some(4.2f);
		var result = 0f;

		some.Switch(
			some: v => { result = v; },
			none: () => { result = -1f; }
		);
		Assert.Equal(4.2f, result);
	}

	[Fact]
	public void SwitchNoneAction() {
		var none = Maybe.None<int>();
		var result = 0f;

		none.Switch(
			some: v => { result = v; },
			none: () => { result = -1f; }
		);
		Assert.Equal(-1f, result);
	}

	[Fact]
	public void ApplyMultipleElements() {
		var fst = Maybe.Some(4);
		var snd = Maybe.Some(15);
		var trd = Maybe.Some(23);

		var sum = Maybe.Some((int a) => (int b) => (int c) => a + b + c);
		var result = sum
			.Apply(fst)
			.Apply(snd)
			.Apply(trd);

		Assert.Equal(42, result.UnpackOr(-1));
	}

	[Fact]
	public void ApplyMultipleElementsNone() {
		var fst = Maybe.Some(4);
		var snd = Maybe.None<int>();
		var trd = Maybe.Some(23);

		var sum = Maybe.Some((int a) => (int b) => (int c) => a + b + c);
		var result = sum
			.Apply(fst)
			.Apply(snd)
			.Apply(trd);

		Assert.Equal(-1, result.UnpackOr(-1));
	}

	[Fact]
	public void SomeToValue() {
		var some = Maybe.Some(42);
		var value = some.ToOkOrSystemError("ERROR");

		Assert.Equal(42, value.UnpackOr(-1));
	}

	[Fact]
	public void NoneToError() {
		var some = Maybe.None<int>();
		var value = some.ToOkOrSystemError("ERROR");

		var error = value.Switch<string>(
			errors => errors.system.First(),
			_ => "OKAY OR WRONG ERROR"
		);
		Assert.Equal("ERROR", error);
	}

	[Fact]
	public void ToMaybeSome() {
		var value = "Hello";
		value.ToMaybe().Switch(
			some: some => Assert.Same(value, some),
			none: () => Assert.Fail("Was None, but should have been some")
		);
	}

	[Fact]
	public void ToMaybeSomeValueType() {
		int? value = 42;
		value.ToMaybe().Switch(
			some: some => Assert.Equal(value, some),
			none: () => Assert.Fail("Was None, but should have been some")
		);
	}

	[Fact]
	public void ToMaybeNone() {
		var value = null as string;
		value.ToMaybe().Switch(
			some: some => Assert.Fail($"Was {some ?? "null"}, but should have been none"),
			none: () => { }
		);
	}

	[Fact]
	public void ToMaybeNoneValueType() {
		int? value = null;
		value.ToMaybe().Switch(
			some: some => Assert.Fail($"Was {some}, but should have been none"),
			none: () => { }
		);
	}
}
