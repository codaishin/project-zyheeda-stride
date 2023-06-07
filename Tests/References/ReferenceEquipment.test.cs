namespace Tests;

using Moq;
using ProjectZyheeda;
using Stride.Engine;
using Xunit;

public class ReferenceEquipmentTests {
	private class SimpleReference : ReferenceEquipment<IEquipment> { }

	[Fact]
	public void Ok() {
		var getCoroutine = Mock.Of<FGetCoroutine>();
		var reference = new SimpleReference { Target = Mock.Of<IEquipment>() };
		var agent = new Entity();

		Mock
			.Get(reference.Target)
			.SetReturnsDefault<Result<FGetCoroutine>>(Result.Ok(getCoroutine));

		var result = reference.PrepareCoroutineFor(agent);

		Assert.Same(getCoroutine, result.UnpackOr(Mock.Of<FGetCoroutine>()));
		Mock
			.Get(reference.Target)
			.Verify(e => e.PrepareCoroutineFor(agent), Times.Once);
	}
}
