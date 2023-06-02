namespace ProjectZyheeda;

using System;
using Stride.Core.Mathematics;
using Stride.Engine;
using Stride.Physics;

public class GetMousePosition : ProjectZyheedaStartupScript, IGetTarget {
	public static readonly string invalidTarget = "Invalid target";

	public CameraComponent? camera;

	private Result<Simulation> simulation = Result.SystemError("NOT STARTED");

	private static Result<Func<Vector3>> WorldPosition(
		Vector2 mousePos,
		Simulation simulation,
		CameraComponent camera
	) {
		var invViewProj = Matrix.Invert(camera.ViewProjectionMatrix);
		var nearPos = new Vector3((mousePos.X * 2f) - 1f, 1f - (mousePos.Y * 2f), 0f);
		var farPos = nearPos + Vector3.UnitZ;

		var nearVector = Vector3.Transform(nearPos, invViewProj);
		nearVector /= nearVector.W;

		var farVector = Vector3.Transform(farPos, invViewProj);
		farVector /= farVector.W;

		var hit = simulation.Raycast(nearVector.XYZ(), farVector.XYZ());

		return hit.Succeeded
			? Result.Ok(() => hit.Point)
			: Result.PlayerError("Invalid target");
	}

	public override void Start() {
		this.simulation = this
			.GetSimulation()
			.OkOrSystemError(this.MissingService<Simulation>());
	}

	public Result<Func<Vector3>> GetTarget() {
		var getTarget =
			(Simulation simulation) =>
			(CameraComponent camera) =>
			(Vector2 mousePos) => GetMousePosition.WorldPosition(mousePos, simulation, camera);

		return getTarget
			.Apply(this.simulation)
			.Apply(this.camera.OkOrSystemError(this.MissingField(nameof(this.camera))))
			.Apply(this.EssentialServices.inputWrapper.MousePosition)
			.Flatten();
	}
}
