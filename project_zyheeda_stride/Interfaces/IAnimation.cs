namespace ProjectZyheeda;

using Stride.Engine;

public interface IAnimation {
	IMaybe<IPlayingAnimation> Play(AnimationComponent animations, string key);
	bool IsPlaying(AnimationComponent animations, string key);
}
