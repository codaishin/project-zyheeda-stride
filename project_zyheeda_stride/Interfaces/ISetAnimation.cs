global using OldAnimationKey = System.String;

namespace ProjectZyheeda;

public interface ISetAnimation {
	Result<OldAnimationKey> SetAnimation(string animationKey);
}
