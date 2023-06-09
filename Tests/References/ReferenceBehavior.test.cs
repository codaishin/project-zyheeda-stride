namespace Tests;

using System;
using System.Collections.Generic;
using Moq;
using ProjectZyheeda;
using Stride.Core.Mathematics;
using Xunit;

public class ReferenceBehaviorTests {
	private class SimpleReference : ReferenceBehavior<IBehavior> { }

	[Fact]
	public void OkOld() {
		var routine = (Mock.Of<Func<IEnumerable<Result<IWait>>>>(), Mock.Of<Cancel>());
		var getTarget = Mock.Of<Func<Vector3>>();
		var reference = new SimpleReference { Target = Mock.Of<IBehavior>() };

		Mock
			.Get(reference.Target)
			.SetReturnsDefault<Result<(Func<IEnumerable<Result<IWait>>>, Cancel)>>(Result.Ok(routine));

		var result = reference.GetCoroutine(getTarget);

		Assert.Equal(routine, result.UnpackOr((Mock.Of<Func<IEnumerable<Result<IWait>>>>(), Mock.Of<Cancel>())));
		Mock
			.Get(reference.Target)
			.Verify(e => e.GetCoroutine(getTarget), Times.Once);
	}

	[Fact]
	public void Ok() {
		var routine = (Mock.Of<Func<IEnumerable<Result<IWait>>>>(), Mock.Of<Cancel>());
		var reference = new SimpleReference { Target = Mock.Of<IBehavior>() };

		Mock
			.Get(reference.Target)
			.SetReturnsDefault<Result<(Func<IEnumerable<Result<IWait>>>, Cancel)>>(Result.Ok(routine));

		var result = reference.GetCoroutine();

		Assert.Equal(routine, result.UnpackOr((Mock.Of<Func<IEnumerable<Result<IWait>>>>(), Mock.Of<Cancel>())));
		Mock
			.Get(reference.Target)
			.Verify(e => e.GetCoroutine(), Times.Once);
	}
}
