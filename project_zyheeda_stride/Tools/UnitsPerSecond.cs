namespace ProjectZyheeda;

using Stride.Core;

[DataContract]
[Display(Expand = ExpandRule.Always)]
public struct UnitsPerSecond : ISpeedEditor {

	public float units;

	public UnitsPerSecond() {
		this.units = 0;
	}

	public UnitsPerSecond(float units) {
		this.units = units;
	}

	public float ToUnitsPerSecond() {
		return this.units;
	}
}
