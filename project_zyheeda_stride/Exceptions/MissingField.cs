namespace ProjectZyheeda;

using System;
using Stride.Engine;

public class MissingField : Exception {
	public static string GetMessageFor(EntityComponent on, params string[] fieldNames) {
		return $"{on.Entity.Name} ({on.GetType().Name}) has missing fields: [{string.Join(", ", fieldNames)}]";
	}

	public MissingField(EntityComponent on, params string[] fieldNames)
		: base(MissingField.GetMessageFor(on, fieldNames)) { }
}
