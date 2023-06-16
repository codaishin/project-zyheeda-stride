namespace Tests;

using Moq;
using ProjectZyheeda;
using Xunit;

public class ReferenceMagazineTests {
	private class SimpleReference : ReferenceMagazine<IMagazine> { }

	[Fact]
	public void Ok() {
		var projectile = Mock.Of<IProjectile>();
		var reference = new SimpleReference { Target = Mock.Of<IMagazine>() };

		Mock
			.Get(reference.Target)
			.SetReturnsDefault<Result<IProjectile>>(Result.Ok(projectile));

		var result = reference.GetProjectile();

		Assert.Same(projectile, result.UnpackOr(Mock.Of<IProjectile>()));
		Mock
			.Get(reference.Target)
			.Verify(e => e.GetProjectile(), Times.Once);
	}
}
