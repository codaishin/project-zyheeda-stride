using System;
using System.Threading.Tasks;
using NUnit.Framework;
using Stride.Engine;

namespace Tests;

[SetUpFixture]
public class TestGame {
	private static Game? game;

	public static Game Game =>
		TestGame.game ??
		throw new NullReferenceException();

	public static Scene RootScene =>
		TestGame
			.Game
			.SceneSystem
			.SceneInstance
			.RootScene;

	[OneTimeSetUp]
	public void StartGame() {
		var game = new Game();
		_ = Task.Run(() => game.Run());
		game.WaitFrames(1);
		TestGame.game = game;
	}

	[OneTimeTearDown]
	public void StopGame() {
		if (TestGame.game is null) {
			return;
		}
		if (TestGame.game.IsRunning) {
			TestGame.game.Exit();
		}
		TestGame.game.Dispose();
	}
}

[TestFixture]
public class GameTestCollection {
	public readonly Game game = TestGame.Game;
	public readonly Scene scene = TestGame.RootScene;

	[SetUp]
	public void RemoveEntities() {
		foreach (var entity in this.scene.Entities) {
			_ = this.scene.Entities.Remove(entity);
			entity.Dispose();
		}
	}
}

public static class TestTools {
	public static void WaitFrames(this Game game, int count) {
		var token = new TaskCompletionSource<bool>();
		_ = game.Script.AddTask(async () => {
			for (var frames = 0; frames < count; ++frames) {
				_ = await game.Script.NextFrame();
			}
			token.SetResult(true);
		});
		token.Task.Wait();
	}
}

[TestFixture]
public class TestGameTests : GameTestCollection {
	private class Component : SyncScript {
		public int frameCounter;

		public override void Update() {
			++this.frameCounter;
		}
	}

	[Test]
	public void TestHasGame() {
		Assert.DoesNotThrow(() => _ = TestGame.Game);
	}

	[Test]
	public void GameRuns() {
		Assert.That(TestGame.Game.IsRunning, Is.True);
	}

	[Test]
	public void RunUpdate() {
		var scene = this.scene;
		var entity = new Entity();
		var component = new Component();

		entity.Add(component);
		scene.Entities.Add(entity);

		this.game.WaitFrames(1);

		Assert.That(component.frameCounter, Is.GreaterThan(0));
	}

	[Test]
	public void OnlyOneEntityA() {
		var entity = new Entity();
		TestGame.RootScene.Entities.Add(entity);

		CollectionAssert.AreEqual(new[] { entity }, TestGame.RootScene.Entities);
	}

	[Test]
	public void OnlyOneEntityB() {
		var entity = new Entity();
		TestGame.RootScene.Entities.Add(entity);

		CollectionAssert.AreEqual(new[] { entity }, TestGame.RootScene.Entities);
	}
}
