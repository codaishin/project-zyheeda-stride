namespace Tests;

using ProjectZyheeda;
using Stride.Engine;
using Xunit;

public class ReferenceTest {
	public interface IMock { }

	private class MockComponent : StartupScript, IMock { }

	private class MockReference : Reference<MockComponent, IMock> { }

	[Fact]
	public void None() {
		var fallback = new MockComponent();
		var reference = new MockReference();
		Assert.Same(fallback, reference.UnpackOr(fallback));
	}

	[Fact]
	public void Some() {
		var fallback = new MockComponent();
		var reference = new MockReference {
			target = new(),
		};
		Assert.Same(reference.target, reference.UnpackOr(fallback));
	}
}
