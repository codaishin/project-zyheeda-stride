namespace Tests;

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
		var (coroutine, cancel) = (Mock.Of<IEnumerable<Result<IWait>>>(), Mock.Of<Cancel>());
		var reference = new SimpleReference { Target = Mock.Of<IScheduler>() };

		Mock
			.Get(reference.Target)
			.SetReturnsDefault<Result>(Result.Ok());

		var result = reference.Enqueue(coroutine, cancel);

		Assert.Equal(result, Result.Ok());
		Mock
			.Get(reference.Target)
			.Verify(e => e.Enqueue(coroutine, cancel), Times.Once);
	}

	[Fact]
	public void RunOk() {
		var (coroutine, cancel) = (Mock.Of<IEnumerable<Result<IWait>>>(), Mock.Of<Cancel>());
		var reference = new SimpleReference { Target = Mock.Of<IScheduler>() };

		Mock
			.Get(reference.Target)
			.SetReturnsDefault<Result>(Result.Ok());

		var result = reference.Run(coroutine, cancel);

		Assert.Equal(result, Result.Ok());
		Mock
			.Get(reference.Target)
			.Verify(e => e.Run(coroutine, cancel), Times.Once);
	}
}
