namespace ProjectZyheeda;

using System;
using Stride.Core.Mathematics;
using Stride.Engine;
using Stride.Physics;

public class GetMousePosition : ProjectZyheedaStartupScript, IGetTarget {
	public static readonly string invalidTarget = "Invalid target";

	public CameraComponent? camera;
	public CollisionFilterGroupFlags collideWith = CollisionFilterGroupFlags.DefaultFilter;
	public bool continuousRaycast;

	private Result<Simulation> simulation = Result.SystemError("NOT STARTED");

	private Result<Func<Result<Vector3>>> WorldPosition(
		Simulation simulation,
		CameraComponent camera
	) {
		return this.EssentialServices.inputWrapper.MousePosition.FlatMap(
			mousePos => {
				var invViewProj = Matrix.Invert(camera.ViewProjectionMatrix);
				var nearPos = new Vector3((mousePos.X * 2f) - 1f, 1f - (mousePos.Y * 2f), 0f);
				var farPos = nearPos + Vector3.UnitZ;

				var nearVector = Vector3.Transform(nearPos, invViewProj);
				nearVector /= nearVector.W;

				var farVector = Vector3.Transform(farPos, invViewProj);
				farVector /= farVector.W;

				var hit = simulation.Raycast(nearVector.XYZ(), farVector.XYZ(), filterFlags: this.collideWith);
				return hit.Succeeded
					? Result.Ok(() => Result.Ok(hit.Point))
					: Result.PlayerError("Invalid target");
			}
		);
	}

	private Result<Func<Result<Vector3>>> WorldPositionLazy(
		Simulation simulation,
		CameraComponent camera
	) {
		return Result.Ok(
			() => this
				.WorldPosition(simulation, camera)
				.FlatMap(getTarget => getTarget())
		);
	}

	public override void Start() {
		this.simulation = this
			.GetSimulation()
			.OkOrSystemError(this.MissingService<Simulation>());
	}

	public Result<Func<Result<Vector3>>> GetTarget() {
		var getTarget =
			(Simulation simulation) =>
			(CameraComponent camera) =>
				this.continuousRaycast
					? this.WorldPositionLazy(simulation, camera)
					: this.WorldPosition(simulation, camera);

		return getTarget
			.Apply(this.simulation)
			.Apply(this.camera.OkOrSystemError(this.MissingField(nameof(this.camera))))
			.Flatten();
	}
}
