namespace Tests;

using System.Collections.Generic;
using Moq;
using ProjectZyheeda;
using Xunit;

public class ReferenceBehaviorTests {
	private class SimpleReference : ReferenceBehavior<IBehavior> { }

	[Fact]
	public void Ok() {
		var routine = (Mock.Of<IEnumerable<Result<IWait>>>(), Mock.Of<Cancel>());
		var reference = new SimpleReference { Target = Mock.Of<IBehavior>() };

		Mock
			.Get(reference.Target)
			.SetReturnsDefault<Result<(IEnumerable<Result<IWait>>, Cancel)>>(Result.Ok(routine));

		var result = reference.GetExecution();

		Assert.Equal(routine, result.UnpackOr((Mock.Of<IEnumerable<Result<IWait>>>(), Mock.Of<Cancel>())));
		Mock
			.Get(reference.Target)
			.Verify(e => e.GetExecution(), Times.Once);
	}
}
