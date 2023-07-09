namespace Tests;

using Moq;
using ProjectZyheeda;
using Xunit;

public class TestReferenceExtensions {
	[Fact]
	public void MissingTarget() {
		var reference = Mock.Of<IReference<int>>();
		Assert.Equal(reference.MissingField(nameof(reference.Target)), reference.MissingTarget());
	}
}
