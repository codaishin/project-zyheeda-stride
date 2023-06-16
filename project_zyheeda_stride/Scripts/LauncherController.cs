namespace ProjectZyheeda;

using System;
using System.Linq;
using Stride.Core;
using Stride.Core.Mathematics;
using Stride.Engine;

public class LauncherController : ProjectZyheedaStartupScript, IEquipment {
	public static readonly string fallbackAnimationKey = "default";

	[DataMember(0)] public TransformComponent? spawnProjectileAt;
	[DataMember(1)] public IMagazineEditor? magazine;
	[DataMember(2)] public string animationKey = "";
	[DataMember(3)] public float rangeModifier = 1f;
	[DataMember(3)] public int preCastMilliseconds;
	[DataMember(4)] public int afterCastMilliseconds;

	private static WaitMilliSeconds Delay(int milliSeconds) {
		return new WaitMilliSeconds(milliSeconds);
	}

	private static IWait NoDelay(IPlayingAnimation _) {
		return new NoWait();
	}

	private static IWait NoDelay() {
		return new NoWait();
	}

	private static Vector3 LookDirection(TransformComponent agent, Func<Vector3> target) {
		var targetPosition = target();
		var lookTarget = new Vector3(targetPosition.X, agent.Position.Y, targetPosition.Z);
		var lookDirection = lookTarget - agent.Position;
		lookDirection.Normalize();
		return lookDirection;
	}

	private FGetCoroutine PrepareCoroutine(
		TransformComponent agent,
		AnimationComponent agentAnimator,
		IMagazine magazine,
		TransformComponent spawnTransform
	) {
		return target => {
			Coroutine Run() {
				var lookDirection = LauncherController.LookDirection(agent, target);
				agent.Rotation = Quaternion.LookRotation(lookDirection, Vector3.UnitY);
				yield return this.EssentialServices.animation
					.Play(agentAnimator, this.animationKey)
					.Map(LauncherController.NoDelay);

				yield return LauncherController
					.Delay(this.preCastMilliseconds);

				var spawn = spawnTransform.WorldMatrix.TranslationVector;
				yield return magazine
					.GetProjectile()
					.FlatMap(projectile => projectile.Follow(spawn, target, this.rangeModifier))
					.Map(LauncherController.NoDelay);

				yield return LauncherController
					.Delay(this.afterCastMilliseconds);

				yield return this.EssentialServices.animation
					.Play(agentAnimator, LauncherController.fallbackAnimationKey)
					.Map(LauncherController.NoDelay);
			}

			Result Cancel() {
				return this.EssentialServices.animation
					.Play(agentAnimator, LauncherController.fallbackAnimationKey);
			}

			return (Run, Cancel);
		};
	}

	public Result<FGetCoroutine> PrepareCoroutineFor(Entity agent) {
		var prepareCoroutine =
			(AnimationComponent agentAnimator) =>
			(IMagazineEditor magazine) =>
			(TransformComponent spawnTransform) =>
				this.PrepareCoroutine(agent.Transform, agentAnimator, magazine, spawnTransform);

		var agentAnimator = agent.GetChildren()
			.Select(c => c.Get<AnimationComponent>())
			.FirstOrDefault(e => e is not null)
			.OkOrSystemError(agent.MissingComponent(nameof(AnimationComponent)));
		var magazine = this.magazine
			.OkOrSystemError(this.MissingField(nameof(this.magazine)));
		var spawnTransform = this.spawnProjectileAt
			.OkOrSystemError(this.MissingField(nameof(this.spawnProjectileAt)));

		return prepareCoroutine
			.Apply(agentAnimator)
			.Apply(magazine)
			.Apply(spawnTransform);
	}
}
