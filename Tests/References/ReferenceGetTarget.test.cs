namespace Tests;

using System;
using Moq;
using ProjectZyheeda;
using Stride.Core.Mathematics;
using Xunit;

public class ReferenceGetTargetTests {
	private class SimpleReference : ReferenceGetTarget<IGetTarget> { }

	[Fact]
	public void Ok() {
		var getTarget = Mock.Of<Func<Result<Vector3>>>();
		var reference = new SimpleReference { Target = Mock.Of<IGetTarget>() };

		Mock
			.Get(reference.Target)
			.SetReturnsDefault<Result<Func<Result<Vector3>>>>(getTarget);

		var result = reference.GetTarget();

		Assert.Same(getTarget, result.UnpackOr(Mock.Of<Func<Result<Vector3>>>()));
		Mock
			.Get(reference.Target)
			.Verify(e => e.GetTarget(), Times.Once);
	}
}
