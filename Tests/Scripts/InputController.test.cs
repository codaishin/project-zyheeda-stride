namespace Tests;

using System;
using Moq;
using NUnit.Framework;
using ProjectZyheeda;
using Stride.Core.Mathematics;
using Stride.Engine;
using Stride.Input;

public abstract class TestInputController<T> : GameTestCollection, IDisposable
	where T : EntityComponent, new() {
	protected Mock<IInputWrapper> mInputWrapper = new();
	protected Mock<IGetTargets> mGetTargets = new();
	protected Mock<IBehavior> mBehavior = new();
	protected Entity behaviorEntity = new();
	protected Entity getTargetEntity = new();
	protected T inputController = new();

	protected Func<bool> TrueInFrame(int frame) {
		return () => this.game.DrawTime.FrameCount == frame;
	}

	[SetUp]
	public void Setup() {
		this.mInputWrapper = new Mock<IInputWrapper>();
		this.mInputWrapper.SetReturnsDefault<bool>(false);
		this.game.Services.AddService<IInputWrapper>(this.mInputWrapper.Object);

		this.mGetTargets = new Mock<EntityComponent>().As<IGetTargets>();
		this.mBehavior = new Mock<EntityComponent>().As<IBehavior>();
		this.inputController = new T();

		this.scene.Entities.Add(new Entity { this.inputController });
		this.scene.Entities.Add(
			this.behaviorEntity = new Entity { (EntityComponent)this.mBehavior.Object }
		);
		this.scene.Entities.Add(
			this.getTargetEntity = new Entity { (EntityComponent)this.mGetTargets.Object }
		);
	}

	[TearDown]
	public void RemoveInputWrapper() {
		this.game.Services.RemoveService<IInputWrapper>();
	}

	public void Dispose() {
		this.getTargetEntity.Dispose();
		this.behaviorEntity.Dispose();
		GC.SuppressFinalize(this);
	}
}

public class TestKeyInputController : TestInputController<KeyInputController> {
	[Test]
	public void RequireIInputWrapperService() {
		this.game.Services.RemoveService<IInputWrapper>();

		_ = Assert
			.Throws<MissingService<IInputWrapper>>(() => this.inputController.Start());
	}

	[Test]
	public void RunBehaviorForKeyPressed() {
		this.inputController.button = Keys.Space;
		this.inputController.mode = InputMode.OnPress;
		this.inputController.behavior.Entity = this.behaviorEntity;
		this.inputController.getTarget.Entity = this.getTargetEntity;

		_ = this.mGetTargets
			.Setup(g => g.GetTargets())
			.Returns(System.Array.Empty<U<Vector3, Entity>>());

		_ = this.mInputWrapper
			.Setup(i => i.IsKeyPressed(Keys.Space))
			.Returns(this.TrueInFrame(this.game.DrawTime.FrameCount));

		this.game.WaitFrames(1);

		this.mBehavior
			.Verify(b => b.Run(System.Array.Empty<U<Vector3, Entity>>()), Times.Once);
	}

	[Test]
	public void RunBehaviorForKeyReleased() {
		this.inputController.button = Keys.D7;
		this.inputController.mode = InputMode.OnRelease;
		this.inputController.behavior.Entity = this.behaviorEntity;
		this.inputController.getTarget.Entity = this.getTargetEntity;

		_ = this.mGetTargets
			.Setup(g => g.GetTargets())
			.Returns(System.Array.Empty<U<Vector3, Entity>>());

		_ = this.mInputWrapper
			.Setup(i => i.IsKeyReleased(Keys.D7))
			.Returns(this.TrueInFrame(this.game.DrawTime.FrameCount));

		this.game.WaitFrames(1);

		this.mBehavior
			.Verify(b => b.Run(System.Array.Empty<U<Vector3, Entity>>()), Times.Once);
	}

	[Test]
	public void MatchModeOnPress() {
		this.inputController.button = Keys.Space;
		this.inputController.mode = InputMode.OnRelease;
		this.inputController.behavior.Entity = this.behaviorEntity;
		this.inputController.getTarget.Entity = this.getTargetEntity;

		_ = this.mGetTargets
			.Setup(g => g.GetTargets())
			.Returns(System.Array.Empty<U<Vector3, Entity>>());

		_ = this.mInputWrapper
			.Setup(i => i.IsKeyPressed(Keys.Space))
			.Returns(this.TrueInFrame(this.game.DrawTime.FrameCount));

		this.game.WaitFrames(1);

		this.mBehavior
			.Verify(b => b.Run(System.Array.Empty<U<Vector3, Entity>>()), Times.Never);
	}

	[Test]
	public void MatchModeOnRelease() {
		this.inputController.button = Keys.Space;
		this.inputController.mode = InputMode.OnPress;
		this.inputController.behavior.Entity = this.behaviorEntity;
		this.inputController.getTarget.Entity = this.getTargetEntity;

		_ = this.mGetTargets
			.Setup(g => g.GetTargets())
			.Returns(System.Array.Empty<U<Vector3, Entity>>());

		_ = this.mInputWrapper
			.Setup(i => i.IsKeyReleased(Keys.Space))
			.Returns(this.TrueInFrame(this.game.DrawTime.FrameCount));

		this.game.WaitFrames(1);

		this.mBehavior
			.Verify(b => b.Run(System.Array.Empty<U<Vector3, Entity>>()), Times.Never);
	}

	[Test]
	public void OnPressPassOnTargets() {
		this.inputController.button = Keys.Space;
		this.inputController.mode = InputMode.OnPress;
		this.inputController.behavior.Entity = this.behaviorEntity;
		this.inputController.getTarget.Entity = this.getTargetEntity;

		var targets = new U<Vector3, Entity>[] { new Vector3(1, 2, 3), new Entity() };
		_ = this.mGetTargets
			.Setup(g => g.GetTargets())
			.Returns(targets);

		_ = this.mInputWrapper
			.Setup(i => i.IsKeyPressed(Keys.Space))
			.Returns(this.TrueInFrame(this.game.DrawTime.FrameCount));

		this.game.WaitFrames(1);

		this.mBehavior.Verify(b => b.Run(targets), Times.Once);
	}

	[Test]
	public void OnReleasePassOnTargets() {
		this.inputController.button = Keys.Space;
		this.inputController.mode = InputMode.OnRelease;
		this.inputController.behavior.Entity = this.behaviorEntity;
		this.inputController.getTarget.Entity = this.getTargetEntity;

		var targets = new U<Vector3, Entity>[] { new Vector3(1, 2, 3), new Entity() };
		_ = this.mGetTargets
			.Setup(g => g.GetTargets())
			.Returns(targets);

		_ = this.mInputWrapper
			.Setup(i => i.IsKeyReleased(Keys.Space))
			.Returns(this.TrueInFrame(this.game.DrawTime.FrameCount));

		this.game.WaitFrames(1);

		this.mBehavior.Verify(b => b.Run(targets), Times.Once);
	}

	[Test]
	public void NoGetTargetError() {
		this.inputController.button = Keys.Space;
		this.inputController.mode = InputMode.OnPress;
		this.inputController.behavior.Entity = this.behaviorEntity;

		this.inputController.Start();

		_ = this.mInputWrapper
			.Setup(i => i.IsKeyPressed(Keys.Space))
			.Returns(true);

		var fieldName = nameof(this.inputController.getTarget);
		var exception = Assert.Throws<MissingField>(this.inputController.Update);
		Assert.That(
			exception!.Message,
			Is.EqualTo(MissingField.GetMessageFor(this.inputController, fieldName))
		);

		this.inputController.mode = InputMode.OnRelease;
		_ = this.mInputWrapper
			.Setup(i => i.IsKeyReleased(Keys.Space))
			.Returns(true);

		exception = Assert.Throws<MissingField>(this.inputController.Update);
		Assert.That(
			exception!.Message,
			Is.EqualTo(MissingField.GetMessageFor(this.inputController, fieldName))
		);
	}

	[Test]
	public void NoBehaviorError() {
		this.inputController.button = Keys.Space;
		this.inputController.mode = InputMode.OnPress;
		this.inputController.getTarget.Entity = this.getTargetEntity;

		this.inputController.Start();

		_ = this.mGetTargets
			.Setup(g => g.GetTargets())
			.Returns(System.Array.Empty<U<Vector3, Entity>>());

		_ = this.mInputWrapper
			.Setup(i => i.IsKeyPressed(Keys.Space))
			.Returns(true);

		var fieldName = nameof(this.inputController.behavior);
		var exception = Assert.Throws<MissingField>(this.inputController.Update);
		Assert.That(
			exception!.Message,
			Is.EqualTo(MissingField.GetMessageFor(this.inputController, fieldName))
		);

		this.inputController.mode = InputMode.OnRelease;
		_ = this.mInputWrapper
			.Setup(i => i.IsKeyReleased(Keys.Space))
			.Returns(true);

		exception = Assert.Throws<MissingField>(this.inputController.Update);
		Assert.That(
			exception!.Message,
			Is.EqualTo(MissingField.GetMessageFor(this.inputController, fieldName))
		);
	}

	[Test]
	public void NoBehaviorAndNoGetTargetErrors() {
		this.inputController.button = Keys.Space;
		this.inputController.mode = InputMode.OnPress;

		this.inputController.Start();

		_ = this.mInputWrapper
			.Setup(i => i.IsKeyPressed(Keys.Space))
			.Returns(true);

		var fieldNames = new[] {
			nameof(this.inputController.getTarget),
			nameof(this.inputController.behavior),
		};
		var exception = Assert.Throws<MissingField>(this.inputController.Update);
		Assert.That(
			exception!.Message,
			Is.EqualTo(MissingField.GetMessageFor(this.inputController, fieldNames))
		);

		this.inputController.mode = InputMode.OnRelease;
		_ = this.mInputWrapper
			.Setup(i => i.IsKeyReleased(Keys.Space))
			.Returns(true);

		exception = Assert.Throws<MissingField>(this.inputController.Update);
		Assert.That(
			exception!.Message,
			Is.EqualTo(MissingField.GetMessageFor(this.inputController, fieldNames))
		);
	}
}

public class TestMouseInputController : TestInputController<MouseInputController> {
	[Test]
	public void RunBehaviorForMousePressed() {
		this.inputController.button = MouseButton.Right;
		this.inputController.mode = InputMode.OnPress;
		this.inputController.behavior.Entity = this.behaviorEntity;
		this.inputController.getTarget.Entity = this.getTargetEntity;

		_ = this.mGetTargets
			.Setup(g => g.GetTargets())
			.Returns(System.Array.Empty<U<Vector3, Entity>>());

		_ = this.mInputWrapper
			.Setup(i => i.IsMouseButtonPressed(MouseButton.Right))
			.Returns(this.TrueInFrame(this.game.DrawTime.FrameCount));

		this.game.WaitFrames(1);

		this.mBehavior
			.Verify(b => b.Run(System.Array.Empty<U<Vector3, Entity>>()), Times.Once);
	}

	[Test]
	public void RunBehaviorForMouseReleased() {
		this.inputController.button = MouseButton.Middle;
		this.inputController.mode = InputMode.OnRelease;
		this.inputController.behavior.Entity = this.behaviorEntity;
		this.inputController.getTarget.Entity = this.getTargetEntity;

		_ = this.mGetTargets
			.Setup(g => g.GetTargets())
			.Returns(System.Array.Empty<U<Vector3, Entity>>());

		_ = this.mInputWrapper
			.Setup(i => i.IsMouseButtonReleased(MouseButton.Middle))
			.Returns(this.TrueInFrame(this.game.DrawTime.FrameCount));

		this.game.WaitFrames(1);

		this.mBehavior
			.Verify(b => b.Run(System.Array.Empty<U<Vector3, Entity>>()), Times.Once);
	}
}
