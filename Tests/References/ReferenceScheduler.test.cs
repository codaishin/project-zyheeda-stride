namespace Tests;

using System;
using System.Collections.Generic;
using Moq;
using ProjectZyheeda;
using Xunit;

public class ReferenceSchedulerTests {
	private class SimpleReference : ReferenceScheduler<IScheduler> { }

	[Fact]
	public void ClearOk() {
		var reference = new SimpleReference { Target = Mock.Of<IScheduler>() };

		Mock
			.Get(reference.Target)
			.SetReturnsDefault<Result>(Result.Ok());

		var result = reference.Clear();

		Assert.Equal(result, Result.Ok());
		Mock
			.Get(reference.Target)
			.Verify(e => e.Clear(), Times.Once);
	}

	[Fact]
	public void EnqueueOk() {
		var execution = (Mock.Of<Func<IEnumerable<Result<IWait>>>>(), Mock.Of<Cancel>());
		var reference = new SimpleReference { Target = Mock.Of<IScheduler>() };

		Mock
			.Get(reference.Target)
			.SetReturnsDefault<Result>(Result.Ok());

		var result = reference.Enqueue(execution);

		Assert.Equal(result, Result.Ok());
		Mock
			.Get(reference.Target)
			.Verify(e => e.Enqueue(execution), Times.Once);
	}

	[Fact]
	public void RunOk() {
		var execution = (Mock.Of<Func<IEnumerable<Result<IWait>>>>(), Mock.Of<Cancel>());
		var reference = new SimpleReference { Target = Mock.Of<IScheduler>() };

		Mock
			.Get(reference.Target)
			.SetReturnsDefault<Result>(Result.Ok());

		var result = reference.Run(execution);

		Assert.Equal(result, Result.Ok());
		Mock
			.Get(reference.Target)
			.Verify(e => e.Run(execution), Times.Once);
	}
}
