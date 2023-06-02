namespace Tests;

using System;
using System.Linq;
using Moq;
using NUnit.Framework;
using ProjectZyheeda;
using Stride.Core.Mathematics;
using Stride.Engine;
using Stride.Physics;

public class TestGetMousePosition : GameTestCollection, System.IDisposable {
	private CameraComponent cameraComponent = new();
	private GetMousePosition getMousePosition = new();
	private IInputWrapper inputManagerWrapper = Mock.Of<IInputWrapper>();

	public void Dispose() {
		GC.SuppressFinalize(this);
	}

	[SetUp]
	public void SetUp() {
		// Uses the following view matrix (copied from a running game) to mock
		// standardized camera view port:
		// 	[M11:0.2 M12:0 M13:0 M14:0]
		// 	[M21:0 M22:0.2 M23:0 M24:0]
		// 	[M31:0 M32:0 M33:-0.0010000999 M34:0]
		// 	[M41:-0 M42:-0 M43: -0.0010000999 M44:1]
		this.inputManagerWrapper = Mock.Of<IInputWrapper>();
		this.game.Services.RemoveService<IInputWrapper>();
		this.game.Services.AddService<IInputWrapper>(this.inputManagerWrapper);

		Mock
			.Get(this.game.Services.GetService<IInputWrapper>())
			.SetReturnsDefault<Result<Vector2>>(Vector2.Zero);

		this.game.WaitFrames(2);

		var viewProjection = new Matrix() {
			Row1 = new Vector4(0.2f, 0, 0, 0),
			Row2 = new Vector4(0, 0.2f, 0, 0),
			Row3 = new Vector4(0, 0, -0.0010000999f, 0),
			Row4 = new Vector4(0, 0, -0.0010000999f, 1),
		};
		this.cameraComponent = new CameraComponent {
			UseCustomProjectionMatrix = true,
			ViewProjectionMatrix = viewProjection
		};


		this.getMousePosition = new GetMousePosition { camera = this.cameraComponent };

		var colliderComponent = new StaticColliderComponent();
		colliderComponent
			.ColliderShapes
			.Add(new BoxColliderShapeDesc { Size = new(5, 5, 5) });
		var box = new Entity { colliderComponent };
		box.Transform.Position = new Vector3(0, 0, -10);

		this.Scene.Entities.Add(box);

		this.game.WaitFrames(2);

		this.Scene.Entities.Add(new Entity { this.cameraComponent });
		this.Scene.Entities.Add(new Entity { this.getMousePosition });
	}

	[Test]
	public void ProofOfConcept() {
		var invViewProj = Matrix.Invert(this.cameraComponent.ViewProjectionMatrix);

		// Implementation adapted from stride documentation
		// see: https://doc.stride3d.net/4.1/en/manual/physics/raycasting.html#example-code
		var mousePos = new Vector2(0.5f, 0.5f);
		var nearPos = new Vector3((mousePos.X * 2f) - 1f, 1f - (mousePos.Y * 2f), 0f);
		var farPos = nearPos + Vector3.UnitZ;

		var nearVector = Vector3.Transform(nearPos, invViewProj);
		nearVector /= nearVector.W;

		var farVector = Vector3.Transform(farPos, invViewProj);
		farVector /= farVector.W;

		this.game.WaitFrames(2);

		var hit = this.game
			.SceneSystem
			.SceneInstance
			.GetProcessor<PhysicsProcessor>()
			.Simulation
			.Raycast(nearVector.XYZ(), farVector.XYZ());

		Assert.Multiple(() => {
			Assert.That(hit.Succeeded, Is.True);
			Assert.That(hit.Point, Is.EqualTo(new Vector3(0, 0, -7.5f)));
		});
	}

	[Test]
	public void Get00N5() {
		_ = Mock.Get(this.game.Services.GetService<IInputWrapper>())
			.SetupGet(i => i.MousePosition)
			.Returns(new Vector2(0.5f, 0.5f));

		this.game.WaitFrames(2);

		this.getMousePosition
			.GetTarget()
			.Switch(
				errors => Assert.Fail(string.Join(", ", errors)),
				getTarget => Assert.That(
					getTarget(),
					Is.EqualTo(new Vector3(0, 0, -7.5f))
				)
			);
	}

	[Test]
	public void GetN55N5() {
		_ = Mock.Get(this.game.Services.GetService<IInputWrapper>())
			.SetupGet(i => i.MousePosition)
			.Returns(new Vector2(0.3f, 0.3f));

		this.game.WaitFrames(2);

		this.getMousePosition
			.GetTarget()
			.Switch(
				errors => Assert.Fail(string.Join(", ", errors)),
				getTarget => Assert.That(
					getTarget(),
					Is.EqualTo(new Vector3(-2, 2, -7.5f)).Using(new VectorTolerance(0.0001f))
				)
			);
	}

	[Test]
	public void NoHit() {
		_ = Mock.Get(this.game.Services.GetService<IInputWrapper>())
			.SetupGet(i => i.MousePosition)
			.Returns(new Vector2(0, 0));

		this.game.WaitFrames(2);

		this.getMousePosition
			.GetTarget()
			.Switch(
				errors => Assert.That(errors, Is.EqualTo((Enumerable.Empty<SystemError>(), new PlayerError[] { GetMousePosition.invalidTarget }))),
				target => Assert.Fail($"Should not have hit something, but hit {target}")
			);
	}

	[Test]
	public void MousePositionError() {
		_ = Mock.Get(this.game.Services.GetService<IInputWrapper>())
			.SetupGet(i => i.MousePosition)
			.Returns(Result.Errors((new SystemError[] { "AAA" }, new PlayerError[] { "aaa" })));

		this.game.WaitFrames(2);

		var errors = this.getMousePosition
			.GetTarget()
			.Switch(
				errors => $"{(string)errors.system.First()}, {(string)errors.player.First()}",
				target => "no errors"
			);

		Assert.That(errors, Is.EqualTo("AAA, aaa"));
	}

	[Test]
	public void MissingCamera() {
		this.getMousePosition.camera = null;

		this.game.WaitFrames(2);

		var result = this.getMousePosition.GetTarget();

		var error = result.Switch(
			errors => (string)errors.system.First(),
			_ => "okay"
		);
		Assert.That(error, Is.EqualTo(this.getMousePosition.MissingField(nameof(this.getMousePosition.camera))));
	}
}
