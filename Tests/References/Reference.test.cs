namespace Tests;

using NUnit.Framework;
using ProjectZyheeda;
using Stride.Engine;

public class ReferenceTest : GameTestCollection {
	public interface IMock { }

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
