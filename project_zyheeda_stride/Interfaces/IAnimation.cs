namespace ProjectZyheeda;

using Stride.Engine;

public interface IAnimation {
	Result<IPlayingAnimation> Play(AnimationComponent animations, string key);
	Result<bool> IsPlaying(AnimationComponent animations, string key);
}
