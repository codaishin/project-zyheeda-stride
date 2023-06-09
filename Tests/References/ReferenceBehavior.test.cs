namespace Tests;

using System;
using System.Collections.Generic;
using Moq;
using ProjectZyheeda;
using Xunit;

public class ReferenceBehaviorTests {
	private class SimpleReference : ReferenceBehavior<IBehavior> { }

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
