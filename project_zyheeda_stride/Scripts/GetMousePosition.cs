namespace ProjectZyheeda;

using System;
using Stride.Core.Mathematics;
using Stride.Engine;
using Stride.Physics;
using GetTargetFn = System.Func<
	Stride.Physics.Simulation,
	System.Func<
		Stride.Engine.CameraComponent,
		System.Func<
			IInputWrapper,
			Result<System.Func<Stride.Core.Mathematics.Vector3>>
		>
	>
>;

public class GetMousePosition : ProjectZyheedaStartupScript, IGetTarget {
	public static readonly string invalidTarget = "Invalid target";

	public CameraComponent? camera;

	private Result<Simulation> simulation = Result.SystemError("NOT STARTED");

	private static readonly GetTargetFn getTarget =
		(Simulation simulation) =>
		(CameraComponent camera) =>
		(IInputWrapper inputManagerWrapper) => {
			var invViewProj = Matrix.Invert(camera.ViewProjectionMatrix);
			var mousePos = inputManagerWrapper.MousePosition;
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
		};

	public override void Start() {
		this.simulation = this
			.GetSimulation()
			.OkOrSystemError(this.MissingService<Simulation>());
	}

	public Result<Func<Vector3>> GetTarget() {
		return GetMousePosition.getTarget
			.Apply(this.simulation)
			.Apply(this.camera.OkOrSystemError(this.MissingField(nameof(this.camera))))
			.Map(func => func(this.EssentialServices.inputWrapper))
			.Flatten();
	}
}
