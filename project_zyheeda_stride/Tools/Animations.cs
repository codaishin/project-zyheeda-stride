namespace ProjectZyheeda;

using System.Collections.Generic;
using Stride.Engine;

public class Animation : IAnimation {
	public Result<bool> IsPlaying(AnimationComponent animations, string key) {
		return animations.IsPlaying(key);
	}

	public Result<IPlayingAnimation> Play(AnimationComponent animations, string key) {
		try {
			var playingAnimation = animations.Play(key.ToString());
			return new PlayingAnimation(playingAnimation);
		} catch (KeyNotFoundException) {
			return Result.SystemError(animations.KeyNotFound(key));
		}
	}
}
