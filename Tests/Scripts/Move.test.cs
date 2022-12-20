namespace Tests;

using System;
using System.Collections.Generic;
using System.Linq;
using Moq;
using NUnit.Framework;
using ProjectZyheeda;
using Stride.Core.Mathematics;
using Stride.Engine;
using Stride.Games;

public class TestMove : GameTestCollection {
	private readonly VectorTolerance tolerance = new(0.001f);

	private static IBehaviorStateMachine GetBehaviorFail(U<Requirement, Type[]> error) {
		Assert.Fail($"Error: {error}");
		return Mock.Of<IBehaviorStateMachine>();
	}

	[Test]
	public void MoveTowardsTarget_1_0_0() {
		var moveComponent = new Move { speed = 1 };
		var move = new Entity { moveComponent };
		var agent = new Entity();

		this.scene.Entities.Add(agent);
		this.scene.Entities.Add(move);

		var behavior = moveComponent
			.GetBehaviorFor(agent)
			.Switch(TestMove.GetBehaviorFail, b => b);

		behavior.ExecuteNext(new Vector3(1, 0, 0));

		var start = (float)this.game.UpdateTime.Total.TotalSeconds;
		this.game.WaitFrames(1);
		var distance = (float)this.game.UpdateTime.Total.TotalSeconds - start;

		Assert.That(
			agent.Transform.Position,
			Is.EqualTo(new Vector3(distance, 0, 0)).Using(this.tolerance)
		);
	}

	[Test]
	public void MoveTowardsTargetEntity() {
		var target = new Entity();
		var moveComponent = new Move { speed = 1 };
		var move = new Entity { moveComponent };
		var agent = new Entity();

		this.scene.Entities.Add(agent);
		this.scene.Entities.Add(move);

		var behavior = moveComponent
			.GetBehaviorFor(agent)
			.Switch(TestMove.GetBehaviorFail, b => b);

		target.Transform.Position = new Vector3(1, 0, 0);
		behavior.ExecuteNext(target);

		var start = (float)this.game.UpdateTime.Total.TotalSeconds;
		this.game.WaitFrames(1);
		var distance = (float)this.game.UpdateTime.Total.TotalSeconds - start;

		Assert.That(
			agent.Transform.Position,
			Is.EqualTo(new Vector3(distance, 0, 0)).Using(this.tolerance)
		);
	}

	[Test]
	public void MoveTowardsTargetEntityAfterChangingTargetPosition() {
		var target = new Entity();
		var moveComponent = new Move { speed = 1 };
		var move = new Entity { moveComponent };
		var agent = new Entity();

		this.scene.Entities.Add(agent);
		this.scene.Entities.Add(move);

		var behavior = moveComponent
			.GetBehaviorFor(agent)
			.Switch(TestMove.GetBehaviorFail, b => b);

		target.Transform.Position = new Vector3(1, 0, 0);
		behavior.ExecuteNext(target);
		target.Transform.Position = new Vector3(0, 1, 0);

		var start = (float)this.game.UpdateTime.Total.TotalSeconds;
		this.game.WaitFrames(1);
		var distance = (float)this.game.UpdateTime.Total.TotalSeconds - start;

		Assert.That(
			agent.Transform.Position,
			Is.EqualTo(new Vector3(0, distance, 0)).Using(this.tolerance)
		);
	}

	[Test]
	public void MoveTowardsTargetFor5Frames() {
		var moveComponent = new Move { speed = 1 };
		var move = new Entity { moveComponent };
		var agent = new Entity();

		this.scene.Entities.Add(agent);
		this.scene.Entities.Add(move);

		var behavior = moveComponent
			.GetBehaviorFor(agent)
			.Switch(TestMove.GetBehaviorFail, b => b);

		behavior.ExecuteNext(new Vector3(1, 0, 0));

		var start = (float)this.game.UpdateTime.Total.TotalSeconds;
		this.game.WaitFrames(5);
		var distance = (float)this.game.UpdateTime.Total.TotalSeconds - start;

		Assert.That(
			agent.Transform.Position,
			Is.EqualTo(new Vector3(distance, 0, 0)).Using(this.tolerance)
		);
	}

	[Test]
	public void MoveTowardsTargetFaster() {
		var moveComponent = new Move { speed = 42 };
		var move = new Entity { moveComponent };
		var agent = new Entity();

		this.scene.Entities.Add(agent);
		this.scene.Entities.Add(move);

		var behavior = moveComponent
			.GetBehaviorFor(agent)
			.Switch(TestMove.GetBehaviorFail, b => b);

		behavior.ExecuteNext(new Vector3(100, 0, 0));

		var start = (float)this.game.UpdateTime.Total.TotalSeconds;
		this.game.WaitFrames(5);
		var distance = (float)this.game.UpdateTime.Total.TotalSeconds - start;
		distance *= 42f;

		Assert.That(
			agent.Transform.Position,
			Is.EqualTo(new Vector3(distance, 0, 0)).Using(this.tolerance)
		);
	}

	[Test]
	public void MoveTowardsTargetWithChangingSpeed() {
		var moveComponent = new Move { speed = 1 };
		var move = new Entity { moveComponent };
		var agent = new Entity();

		this.scene.Entities.Add(agent);
		this.scene.Entities.Add(move);

		var behavior = moveComponent
			.GetBehaviorFor(agent)
			.Switch(TestMove.GetBehaviorFail, b => b);

		var start = (float)this.game.UpdateTime.Total.TotalSeconds;
		behavior.ExecuteNext(new Vector3(1, 0, 0));
		this.game.WaitFrames(1);
		var distance = (float)this.game.UpdateTime.Total.TotalSeconds - start;

		start = (float)this.game.UpdateTime.Total.TotalSeconds;
		moveComponent.speed = 0.5f;
		this.game.WaitFrames(1);
		distance += ((float)this.game.UpdateTime.Total.TotalSeconds - start) * 0.5f;

		Assert.That(
			agent.Transform.Position,
			Is.EqualTo(new Vector3(distance, 0, 0)).Using(this.tolerance)
		);
	}

	[Test]
	public void MoveTowardsTarget_0_neg1_0() {
		var moveComponent = new Move { speed = 1 };
		var move = new Entity { moveComponent };
		var agent = new Entity();

		this.scene.Entities.Add(agent);
		this.scene.Entities.Add(move);

		var behavior = moveComponent
			.GetBehaviorFor(agent)
			.Switch(TestMove.GetBehaviorFail, b => b);

		behavior.ExecuteNext(new Vector3(0, -1, 0));

		var start = (float)this.game.UpdateTime.Total.TotalSeconds;
		this.game.WaitFrames(5);
		var distance = (float)this.game.UpdateTime.Total.TotalSeconds - start;

		Assert.That(
			agent.Transform.Position,
			Is.EqualTo(new Vector3(0, -distance, 0)).Using(this.tolerance)
		);
	}

	private class TrackPosition : SyncScript {
		private readonly List<Vector3> positions;

		public TrackPosition(List<Vector3> positions) {
			this.positions = positions;
		}

		public override void Update() {
			this.positions.Add(this.Entity.Transform.Position);
		}
	}

	[Test]
	public void UseMultipleTargetsAsMoveWaypoints() {
		var waypoints = new List<Vector3>();
		var moveComponent = new Move { speed = 100 };
		var move = new Entity { moveComponent };
		var agent = new Entity { new TrackPosition(waypoints) };

		this.scene.Entities.Add(agent);
		this.scene.Entities.Add(move);

		var behavior = moveComponent
			.GetBehaviorFor(agent)
			.Switch(TestMove.GetBehaviorFail, b => b);

		behavior.ExecuteNext(new Vector3(1, 0, 0), new Vector3(1, 1, 0));
		this.game.WaitFrames(2);

		Assert.That(
			waypoints.Take(2),
			Is.EqualTo(new[] { new Vector3(1, 0, 0), new Vector3(1, 1, 0) })
		);
	}

	[Test]
	public void MoveTowardsTargetFromOffsetPosition() {
		var moveComponent = new Move { speed = 1 };
		var move = new Entity { moveComponent };
		var agent = new Entity();

		this.scene.Entities.Add(agent);
		this.scene.Entities.Add(move);

		var behavior = moveComponent
			.GetBehaviorFor(agent)
			.Switch(TestMove.GetBehaviorFail, b => b);

		agent.Transform.Position = new Vector3(1, 0, 0);
		behavior.ExecuteNext(new Vector3(1, 1, 0));

		var start = (float)this.game.UpdateTime.Total.TotalSeconds;
		this.game.WaitFrames(5);
		var distance = (float)this.game.UpdateTime.Total.TotalSeconds - start;

		Assert.That(
			agent.Transform.Position,
			Is.EqualTo(new Vector3(1, distance, 0)).Using(this.tolerance)
		);
	}

	[Test]
	public void MoveTowardsTargetWithNotNormalizedInitialDistance() {
		var moveComponent = new Move { speed = 1 };
		var move = new Entity { moveComponent };
		var agent = new Entity();

		this.scene.Entities.Add(agent);
		this.scene.Entities.Add(move);

		var behavior = moveComponent
			.GetBehaviorFor(agent)
			.Switch(TestMove.GetBehaviorFail, b => b);

		behavior.ExecuteNext(new Vector3(1, 1, 0));

		var start = (float)this.game.UpdateTime.Total.TotalSeconds;
		this.game.WaitFrames(5);
		var distance = (float)this.game.UpdateTime.Total.TotalSeconds - start;

		var target = new Vector3(1, 1, 0);
		target.Normalize();

		Assert.That(
			agent.Transform.Position,
			Is.EqualTo(target * distance).Using(this.tolerance)
		);
	}

	[Test]
	public void DoNotOvershoot() {
		var moveComponent = new Move { speed = 100_000 };
		var move = new Entity { moveComponent };
		var agent = new Entity();

		this.scene.Entities.Add(agent);
		this.scene.Entities.Add(move);

		var behavior = moveComponent
			.GetBehaviorFor(agent)
			.Switch(TestMove.GetBehaviorFail, b => b);

		behavior.ExecuteNext(new Vector3(1, 0, 0));

		this.game.WaitFrames(1);

		Assert.That(agent.Transform.Position, Is.EqualTo(new Vector3(1, 0, 0)));
	}

	[Test]
	public void SuspendBehaviorWhenResetAndIdleCalled() {
		var moveComponent = new Move { speed = 1 };
		var move = new Entity { moveComponent };
		var agent = new Entity();

		this.scene.Entities.Add(agent);
		this.scene.Entities.Add(move);

		var behavior = moveComponent
			.GetBehaviorFor(agent)
			.Switch(TestMove.GetBehaviorFail, b => b);

		behavior.ExecuteNext(new Vector3(1, 0, 0));

		var start = (float)this.game.UpdateTime.Total.TotalSeconds;
		this.game.WaitFrames(1);
		var distance = (float)this.game.UpdateTime.Total.TotalSeconds - start;

		behavior.ResetAndIdle();

		this.game.WaitFrames(1);

		Assert.That(
			agent.Transform.Position,
			Is.EqualTo(new Vector3(distance, 0, 0)).Using(this.tolerance)
		);
	}

	[Test]
	public void ExecuteNextAfterResetAndIdle() {
		var moveComponent = new Move { speed = 1 };
		var move = new Entity { moveComponent };
		var agent = new Entity();

		this.scene.Entities.Add(agent);
		this.scene.Entities.Add(move);

		var behavior = moveComponent
			.GetBehaviorFor(agent)
			.Switch(TestMove.GetBehaviorFail, b => b);

		behavior.ResetAndIdle();
		behavior.ExecuteNext(new Vector3(1, 0, 0));

		var start = (float)this.game.UpdateTime.Total.TotalSeconds;
		this.game.WaitFrames(1);
		var distance = (float)this.game.UpdateTime.Total.TotalSeconds - start;

		Assert.That(
			agent.Transform.Position,
			Is.EqualTo(new Vector3(distance, 0, 0)).Using(this.tolerance)
		);
	}

	[Test]
	public void ExecuteNextOverridesLastExecution() {
		var moveComponent = new Move { speed = 1 };
		var move = new Entity { moveComponent };
		var agent = new Entity();

		this.scene.Entities.Add(agent);
		this.scene.Entities.Add(move);

		var behavior = moveComponent
			.GetBehaviorFor(agent)
			.Switch(TestMove.GetBehaviorFail, b => b);

		behavior.ExecuteNext(new Vector3(1, 0, 0));
		behavior.ExecuteNext(new Vector3(-1, 0, 0));

		var start = (float)this.game.UpdateTime.Total.TotalSeconds;
		this.game.WaitFrames(1);
		var distance = (float)this.game.UpdateTime.Total.TotalSeconds - start;

		Assert.That(
			agent.Transform.Position,
			Is.EqualTo(new Vector3(-distance, 0, 0)).Using(this.tolerance)
		);
	}
}