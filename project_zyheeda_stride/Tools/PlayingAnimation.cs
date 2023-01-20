namespace ProjectZyheeda;

public readonly struct PlayingAnimation : IPlayingAnimation {
	private readonly Stride.Animations.PlayingAnimation animation;

	public PlayingAnimation(Stride.Animations.PlayingAnimation animation) {
		this.animation = animation;
	}
}
