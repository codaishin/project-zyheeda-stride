namespace Tests;

using ProjectZyheeda;
using Xunit;

public class TestUnitsPerSecond {
	[Theory]
	[InlineData(200)]
	[InlineData(300)]
	public void ToUnitsPerMillisecond(float units) {
		var speed = new UnitsPerSecond(units);
		Assert.Equal(units, speed.ToUnitsPerSecond());
	}
}
