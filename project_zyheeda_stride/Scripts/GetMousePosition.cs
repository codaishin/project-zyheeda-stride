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
			IInputManagerWrapper,
			IMaybe<U<Stride.Core.Mathematics.Vector3, Stride.Engine.Entity>>
		>
	>
>;

public class GetMousePosition : ProjectZyheedaStartupScript, IGetTarget {
	public CameraComponent? camera;

	private IMaybe<Simulation> simulation = Maybe.None<Simulation>();

	private static readonly GetTargetFn getTarget =
		(Simulation simulation) =>
		(CameraComponent camera) =>
		(IInputManagerWrapper inputManagerWrapper) => {
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
				? Maybe.Some((U<Vector3, Entity>)hit.Point)
				: Maybe.None<U<Vector3, Entity>>();
		};

	public override void Start() {
		this.simulation = this
			.GetSimulation()
			.ToMaybe();
	}

	public IMaybe<U<Vector3, Entity>> GetTarget() {
		return GetMousePosition.getTarget
			.Apply(this.simulation.MaybeToEither(this.NoService<Simulation>()))
			.Apply(this.camera.ToEither(this.NoField(nameof(this.camera))))
			.Switch(
				error => throw error,
				func => func(this.EssentialServices.inputManager)
			);
	}

	private Exception NoField(string fieldName) {
		return new MissingField(this, fieldName);
	}

	private Exception NoService<T>() {
		return new MissingService<T>(
			$"{typeof(T)} service missing. Needed by: {this.Entity.Name} ({this.GetType().Name})"
		);
	}
}
