namespace ProjectZyheeda;

using System;


public class MissingField : Exception {
	public static string GetMessageFor(params string[] fieldNames) {
		return $"missing fields: [{string.Join(", ", fieldNames)}]";
	}

	public MissingField(params string[] fieldNames)
		: base(MissingField.GetMessageFor(fieldNames)) { }
}
