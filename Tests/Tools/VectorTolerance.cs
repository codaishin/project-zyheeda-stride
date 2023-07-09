namespace Tests;

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Stride.Core.Mathematics;
using Xunit;

public class VectorTolerance : IEqualityComparer<Vector3> {
	private readonly float tolerance;

	public VectorTolerance(float tolerance) {
		this.tolerance = tolerance;
	}

	public bool Equals(Vector3 x, Vector3 y) {
		if (x == y) {
			return true;
		}
		for (var i = 0; i < 3; ++i) {
			if (Math.Abs(x[i] - y[i]) > this.tolerance) {
				return false;
			}
		}
		return true;
	}

	public int GetHashCode([DisallowNull] Vector3 obj) {
		return obj.GetHashCode();
	}
}

public class TestCompareVectorsWithTolerance {
	[Fact]
	public void AreEqual() {
		var a = new Vector3(1, 0, 0);
		var b = new Vector3(1, 0, 0);

		Assert.Equal(a, b, new VectorTolerance(0f));
	}

	[Fact]
	public void AreNotEqual() {
		var a = new Vector3(1, 0, 0);
		var b = new Vector3(0, 0, 0);

		Assert.NotEqual(a, b, new VectorTolerance(0f));
	}

	[Fact]
	public void AreAlmostEqual() {
		var a = new Vector3(1, -0.06f, 0);
		var b = new Vector3(1.09f, 0, 0.0001f);

		Assert.Equal(a, b, new VectorTolerance(0.1f));
	}
}
