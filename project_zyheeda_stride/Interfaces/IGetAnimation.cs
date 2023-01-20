namespace ProjectZyheeda;

using Stride.Engine;

public interface IGetAnimation {
	IMaybe<IPlayingAnimation> Play(AnimationComponent animations, string key);
	bool IsPlaying(AnimationComponent animations, string key);
}
