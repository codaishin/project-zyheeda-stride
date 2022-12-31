namespace ProjectZyheeda;

using System;
using Stride.Core.Mathematics;
using Stride.Engine;
using Stride.Physics;
using GetTargetFn = System.Func<
	IInputManagerWrapper,
	System.Func<
		Stride.Physics.Simulation,
		System.Func<
			Stride.Engine.CameraComponent,
			IMaybe<U<Stride.Core.Mathematics.Vector3, Stride.Engine.Entity>>
		>
	>
>;

public class GetMousePosition : StartupScript, IGetTarget {
	public CameraComponent? camera;

	private IMaybe<IInputManagerWrapper> inputManagerWrapper = Maybe.None<IInputManagerWrapper>();
	private IMaybe<Simulation> simulation = Maybe.None<Simulation>();

	private static readonly GetTargetFn getTarget =
		(IInputManagerWrapper inputManagerWrapper) =>
		(Simulation simulation) =>
		(CameraComponent camera) => {
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
		this.inputManagerWrapper = this.Game.Services
			.GetService<IInputManagerWrapper>()
			.ToMaybe();
		this.simulation = this
			.GetSimulation()
			.ToMaybe();
	}

	public IMaybe<U<Vector3, Entity>> GetTarget() {
		return GetMousePosition.getTarget
			.Apply(this.inputManagerWrapper.MaybeToEither(this.NoService<IInputManagerWrapper>()))
			.Apply(this.simulation.MaybeToEither(this.NoService<Simulation>()))
			.Apply(this.camera.ToEither(this.NoField(nameof(this.camera))))
			.Switch(
				error => throw error,
				value => value
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
