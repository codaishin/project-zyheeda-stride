namespace ProjectZyheeda;

using System.Collections.Generic;
using Stride.Engine;

public class GetAnimations : IGetAnimation {
	public bool IsPlaying(AnimationComponent animations, string key) {
		return animations.IsPlaying(key);
	}

	public IMaybe<IPlayingAnimation> Play(AnimationComponent animations, string key) {
		try {
			var playingAnimation = animations.Play(key.ToString());
			return Maybe.Some<IPlayingAnimation>(new PlayingAnimation(playingAnimation));
		} catch (KeyNotFoundException) {
			return Maybe.None<IPlayingAnimation>();
		}
	}
}
