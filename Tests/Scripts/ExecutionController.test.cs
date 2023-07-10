namespace Tests;

using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Moq;
using ProjectZyheeda;
using Stride.Engine;
using Xunit;

public class TestExecutionController : GameTestCollection {
	private readonly ExecutionController controller;
	private readonly IExecutionStreamEditor inputStream;
	private readonly IBehaviorEditor behavior;
	private readonly List<TaskCompletionSource<Result<FExecute>>> newActionFnTaskTokens;
	private readonly ISystemMessage systemMessage;
	private readonly IPlayerMessage playerMessage;
	private readonly IInputDispatcher dispatcher;

	public TestExecutionController(GameFixture fixture) : base(fixture) {
		var newActionCallCount = 0;
		this.controller = new ExecutionController {
			input = this.inputStream = Mock.Of<IExecutionStreamEditor>(),
			behavior = this.behavior = Mock.Of<IBehaviorEditor>(),
		};

		this.newActionFnTaskTokens = new List<TaskCompletionSource<Result<FExecute>>> {
			new TaskCompletionSource<Result<FExecute>>(),
			new TaskCompletionSource<Result<FExecute>>(),
			new TaskCompletionSource<Result<FExecute>>(),
			new TaskCompletionSource<Result<FExecute>>(),
			new TaskCompletionSource<Result<FExecute>>(),
		};

		_ = Mock
			.Get(this.controller.input)
			.Setup(s => s.ProcessEvent(It.IsAny<InputKeys>(), It.IsAny<bool>()))
			.Returns(Result.Ok());

		_ = Mock
			.Get(this.controller.input)
			.Setup(i => i.NewExecute())
			.Returns(() => this.newActionFnTaskTokens[newActionCallCount++].Task);

		_ = Mock
			.Get(this.controller.behavior)
			.Setup(b => b.GetExecution())
			.Returns(
				Result.Ok<(IEnumerable<Result<IWait>>, Cancel)>(
					(Enumerable.Empty<Result<IWait>>(), () => Result.Ok())
				)
			);

		this.game.Services.RemoveService<ISystemMessage>();
		this.game.Services.AddService(this.systemMessage = Mock.Of<ISystemMessage>());
		this.game.Services.RemoveService<IPlayerMessage>();
		this.game.Services.AddService(this.playerMessage = Mock.Of<IPlayerMessage>());
		this.game.Services.RemoveService<IInputDispatcher>();
		this.game.Services.AddService(this.dispatcher = Mock.Of<IInputDispatcher>());

		Mock
			.Get(this.dispatcher)
			.SetReturnsDefault<Result>(Result.Ok());

		this.scene.Entities.Add(new Entity { this.controller });

		this.game.Frames(2).Wait();
	}

	[Fact]
	public void AddInputStreamToDispatcher() {
		Mock
			.Get(this.game.Services.GetService<IInputDispatcher>())
			.Verify(d => d.Add(this.inputStream), Times.Once);
	}

	[Fact]
	public async void RemoveInputStreamFromDispatcher() {
		_ = this.scene.Entities.Remove(this.controller.Entity);

		await this.game.Frames(1);

		this.scene.Entities.Add(this.controller.Entity);

		await this.game.Frames(1);

		_ = this.scene.Entities.Remove(this.controller.Entity);

		await this.game.Frames(1);

		Mock
			.Get(this.game.Services.GetService<IInputDispatcher>())
			.Verify(d => d.Remove(this.inputStream), Times.Exactly(2));
	}

	[Fact]
	public async void RunBehavior() {
		(IEnumerable<Result<IWait>>, Cancel) execution = (
			Enumerable.Empty<Result<IWait>>(),
			() => Result.Ok()
		);
		var execute = Mock.Of<FExecute>();

		_ = Mock.Get(this.behavior)
			.Setup(c => c.GetExecution())
			.Returns(execution);

		Mock
			.Get(execute)
			.SetReturnsDefault<Result>(Result.Ok());

		this.newActionFnTaskTokens[0].SetResult(execute);

		await this.game.Frames(1);

		Mock
			.Get(this.behavior)
			.Verify(b => b.GetExecution(), Times.Once);
	}

	[Fact]
	public async void RunBehaviorTwice() {
		(IEnumerable<Result<IWait>> coroutine, Cancel cancel) execution = (
			Enumerable.Empty<Result<IWait>>(),
			() => Result.Ok()
		);
		var executeA = Mock.Of<FExecute>();
		var executeB = Mock.Of<FExecute>();

		Mock
			.Get(executeA)
			.SetReturnsDefault<Result>(Result.Ok());
		Mock
			.Get(executeB)
			.SetReturnsDefault<Result>(Result.Ok());

		_ = Mock
			.Get(this.behavior)
			.Setup(c => c.GetExecution())
			.Returns(execution);

		this.newActionFnTaskTokens[0].SetResult(executeA);
		this.newActionFnTaskTokens[1].SetResult(executeB);

		await this.game.Frames(2);

		Assert.Multiple(
			() => Mock.Get(executeA).Verify(e => e(execution.coroutine, execution.cancel), Times.Once),
			() => Mock.Get(executeB).Verify(e => e(execution.coroutine, execution.cancel), Times.Once)
		);
	}

	[Fact]
	public async void MissingBehavior() {
		_ = this.scene.Entities.Remove(this.controller.Entity);

		this.controller.behavior = null;

		this.scene.Entities.Add(this.controller.Entity);

		await this.game.Frames(2);

		Mock
			.Get(this.systemMessage)
			.Verify(s => s.Log(this.controller.MissingField(nameof(this.controller.behavior))), Times.Once);
	}

	[Fact]
	public async void InputNotSet() {
		_ = this.scene.Entities.Remove(this.controller.Entity);

		this.controller.input = null;

		this.scene.Entities.Add(this.controller.Entity);

		await this.game.Frames(2);

		Mock
			.Get(this.systemMessage)
			.Verify(s => s.Log(this.controller.MissingField(nameof(this.controller.input))), Times.Once);
	}

	[Fact]
	public async void AllFieldsMissing() {
		_ = this.scene.Entities.Remove(this.controller.Entity);

		this.controller.input = null;
		this.controller.behavior = null;

		this.scene.Entities.Add(this.controller.Entity);

		await this.game.Frames(2);

		Mock
			.Get(this.systemMessage)
			.Verify(s => s.Log(
				this.controller.MissingField(nameof(this.controller.behavior)),
				this.controller.MissingField(nameof(this.controller.input))
			), Times.Once);
	}

	[Fact]
	public async void LogCoroutineError() {
		_ = Mock
			.Get(this.behavior)
			.Setup(b => b.GetExecution())
			.Returns(Result.Errors((new SystemError[] { "AAA" }, new PlayerError[] { "aaa" })));

		this.newActionFnTaskTokens[0].SetResult(Mock.Of<FExecute>());

		await this.game.Frames(2);

		Mock
			.Get(this.systemMessage)
			.Verify(m => m.Log("AAA"), Times.Once);
		Mock
			.Get(this.playerMessage)
			.Verify(m => m.Log("aaa"), Times.Once);
	}

	[Fact]
	public async void LogActionError() {
		_ = Mock
			.Get(this.behavior)
			.Setup(b => b.GetExecution())
			.Returns((Enumerable.Empty<Result<IWait>>(), () => Result.Ok()));

		this.newActionFnTaskTokens[0].SetResult(Result.Errors((new SystemError[] { "AAA" }, new PlayerError[] { "aaa" })));

		await this.game.Frames(2);

		Mock
			.Get(this.systemMessage)
			.Verify(m => m.Log("AAA"), Times.Once);
		Mock
			.Get(this.playerMessage)
			.Verify(m => m.Log("aaa"), Times.Once);
	}

	[Fact]
	public async void LogCoroutineRunError() {
		_ = Mock
			.Get(this.behavior)
			.Setup(b => b.GetExecution())
			.Returns((Enumerable.Empty<Result<IWait>>(), () => Result.Ok()));

		var errors = Result.Errors((new SystemError[] { "AAA" }, new PlayerError[] { "aaa" }));
		this.newActionFnTaskTokens[0].SetResult(errors);

		await this.game.Frames(2);

		Mock
			.Get(this.systemMessage)
			.Verify(m => m.Log("AAA"), Times.Once);
		Mock
			.Get(this.playerMessage)
			.Verify(m => m.Log("aaa"), Times.Once);
	}

	[Fact]
	public async void LogInputDispatcherAddErrors() {
		_ = Mock
			.Get(this.dispatcher)
			.Setup(d => d.Add(It.IsAny<IExecutionStreamEditor>()))
			.Returns(Result.Errors((new SystemError[] { "AAA" }, new PlayerError[] { "aaa" })));

		_ = this.scene.Entities.Remove(this.controller.Entity);
		this.scene.Entities.Add(this.controller.Entity);

		await this.game.Frames(2);

		Mock
			.Get(this.systemMessage)
			.Verify(m => m.Log("AAA"), Times.Once);
		Mock
			.Get(this.playerMessage)
			.Verify(m => m.Log("aaa"), Times.Once);
	}

	[Fact]
	public async void LogInputDispatcherRemoveErrors() {
		_ = Mock
			.Get(this.dispatcher)
			.Setup(d => d.Remove(It.IsAny<IExecutionStreamEditor>()))
			.Returns(Result.Errors((new SystemError[] { "AAA" }, new PlayerError[] { "aaa" })));

		await this.game.Frames(1);

		_ = this.scene.Entities.Remove(this.controller.Entity);

		await this.game.Frames(2);

		Mock
			.Get(this.systemMessage)
			.Verify(m => m.Log("AAA"), Times.Once);
		Mock
			.Get(this.playerMessage)
			.Verify(m => m.Log("aaa"), Times.Once);
	}
}
