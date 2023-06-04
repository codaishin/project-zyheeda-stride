namespace Tests;

using System;
using System.Threading.Tasks;
using ProjectZyheeda;
using Stride.Engine;
using Xunit;

public class GameFixture : IDisposable {
	public readonly Game game;
	public Scene RootScene => this.game.SceneSystem.SceneInstance.RootScene;

	public GameFixture() {
		this.game = new Game();
		_ = Task.Run(() => this.game.Run());
		this.game.WaitFrames(2);
	}

	public void Dispose() {
		if (this.game is null) {
			return;
		}
		if (this.game.IsRunning) {
			this.game.Exit();
		}
		this.game.Dispose();
		GC.SuppressFinalize(this);
	}
}

[CollectionDefinition("Game test collection")]
public class GameTestCollectionDefinition : ICollectionFixture<GameFixture> { }

[Collection("Game test collection")]
public class GameTestCollection : IDisposable {
	private readonly GameFixture fixture;
	public readonly Game game;
	public readonly Scene scene;
	public readonly Runner tasks;

	public class Runner : StartupScript {
		public void AddTask(Func<Task> microThreadFunction) {
			_ = this.Script.AddTask(microThreadFunction);
		}
	}

	public GameTestCollection(GameFixture fixture) {
		this.fixture = fixture;
		this.game = fixture.game;

		this.scene = new Scene();
		this.fixture.RootScene.Children.Add(this.scene);

		this.tasks = new();
		this.scene.Entities.Add(new Entity { this.tasks });
	}

	public void RemoveScene() {
		_ = this.fixture.RootScene.Children.Remove(this.scene);
	}

	public void RemoveEssentialServices() {
		this.game.Services.RemoveService<IInputWrapper>();
		this.game.Services.RemoveService<IAnimation>();
		this.game.Services.RemoveService<ISystemMessage>();
		this.game.Services.RemoveService<IPlayerMessage>();
		this.game.Services.RemoveService<IPrefabLoader>();
		this.game.WaitFrames(1);
	}

	public void Dispose() {
		GC.SuppressFinalize(this);
		this.RemoveScene();
		this.RemoveEssentialServices();
	}
}

public static class TestTools {
	public static void WaitFrames(this Game game, int count) {
		var token = new TaskCompletionSource();
		_ = game.Script.AddTask(async () => {
			for (var frames = 1; frames < count; ++frames) {
				_ = await game.Script.NextFrame();
			}
			token.SetResult();
		});
		token.Task.Wait();
	}
}

public class TestGameTestCollection : GameTestCollection {
	public TestGameTestCollection(GameFixture fixture) : base(fixture) { }

	[Fact]
	public void GameRuns() {
		Assert.True(this.game.IsRunning);
	}

	[Fact]
	public void RunUpdate() {
		var frame = this.game.UpdateTime.FrameCount;

		this.game.WaitFrames(1);

		Assert.Equal(frame + 1, this.game.UpdateTime.FrameCount);
	}

	[Fact]
	public void RunTasks() {
		var counter = 0;
		var incrementCounterOnePerFrame = async () => {
			while (true) {
				_ = await this.game.Script.NextFrame();
				++counter;
			}
		};

		this.tasks.AddTask(incrementCounterOnePerFrame);
		this.game.WaitFrames(10);

		Assert.Equal(9, counter);
	}
}
