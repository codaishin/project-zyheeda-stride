namespace Tests;

using System.Linq;
using Moq;
using ProjectZyheeda;
using Stride.Core.Mathematics;
using Stride.Engine;
using Stride.Physics;
using Xunit;

public class TestGetMousePosition : GameTestCollection {
	private readonly CameraComponent cameraComponent;
	private readonly GetMousePosition getMousePosition;
	private readonly IInputWrapper inputManagerWrapper;
	private readonly StaticColliderComponent collisionTarget;

	public TestGetMousePosition(GameFixture fixture) : base(fixture) {
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

		this.collisionTarget = new StaticColliderComponent { CanCollideWith = CollisionFilterGroupFlags.AllFilter };
		this.collisionTarget
			.ColliderShapes
			.Add(new BoxColliderShapeDesc { Size = new(5, 5, 5) });
		var box = new Entity { this.collisionTarget };
		box.Transform.Position = new Vector3(0, 0, -10);

		this.scene.Entities.Add(box);

		this.game.WaitFrames(2);

		this.scene.Entities.Add(new Entity { this.cameraComponent });
		this.scene.Entities.Add(new Entity { this.getMousePosition });
	}

	[Fact]
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
			Assert.True(hit.Succeeded);
			Assert.Equal(new Vector3(0, 0, -7.5f), hit.Point);
		});
	}

	[Fact]
	public void Get00N5() {
		_ = Mock.Get(this.game.Services.GetService<IInputWrapper>())
			.SetupGet(i => i.MousePosition)
			.Returns(new Vector2(0.5f, 0.5f));

		this.game.WaitFrames(2);

		this.getMousePosition
			.GetTarget()
			.Switch(
				errors => Assert.Fail(string.Join(", ", errors)),
				getTarget => Assert.Equal(new Vector3(0, 0, -7.5f), getTarget())
			);
	}

	[Fact]
	public void GetN55N5() {
		_ = Mock.Get(this.game.Services.GetService<IInputWrapper>())
			.SetupGet(i => i.MousePosition)
			.Returns(new Vector2(0.3f, 0.3f));

		this.game.WaitFrames(2);

		this.getMousePosition
			.GetTarget()
			.Switch(
				errors => Assert.Fail(string.Join(", ", errors)),
				getTarget => Assert.Equal(
					new Vector3(-2, 2, -7.5f),
					getTarget(),
					new VectorTolerance(0.0001f)
				)
			);
	}

	[Fact]
	public void NoHit() {
		_ = Mock.Get(this.game.Services.GetService<IInputWrapper>())
			.SetupGet(i => i.MousePosition)
			.Returns(new Vector2(0, 0));

		this.game.WaitFrames(2);

		this.getMousePosition
			.GetTarget()
			.Switch(
				errors => {
					Assert.Multiple(() => {
						Assert.Empty(errors.system);
						Assert.Contains(GetMousePosition.invalidTarget, errors.player);
					});
				},
				target => Assert.Fail($"Should not have hit something, but hit {target}")
			);
	}

	[Fact]
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

		Assert.Equal("AAA, aaa", errors);
	}

	[Fact]
	public void MissingCamera() {
		this.getMousePosition.camera = null;

		this.game.WaitFrames(2);

		var result = this.getMousePosition.GetTarget();

		var error = result.Switch(
			errors => (string)errors.system.First(),
			_ => "okay"
		);
		Assert.Equal(this.getMousePosition.MissingField(nameof(this.getMousePosition.camera)), error);
	}

	[Fact]
	public void NoHitOnFilterMismatch() {
		_ = Mock.Get(this.game.Services.GetService<IInputWrapper>())
			.SetupGet(i => i.MousePosition)
			.Returns(new Vector2(0.5f, 0.5f));

		this.getMousePosition.collideWith = CollisionFilterGroupFlags.CustomFilter2;

		this.game.WaitFrames(2);

		this.getMousePosition
			.GetTarget()
			.Switch(
				errors => Assert.Equal(GetMousePosition.invalidTarget, (string)errors.player.FirstOrDefault()),
				getTarget => Assert.Fail("hit something, should have hit nothing")
			);
	}
}
