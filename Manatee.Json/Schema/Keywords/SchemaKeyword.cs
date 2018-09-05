﻿using Manatee.Json.Serialization;

namespace Manatee.Json.Schema
{
	public class SchemaKeyword : IJsonSchemaKeyword
	{
		public virtual string Name => "$schema";
		public virtual JsonSchemaVersion SupportedVersions { get; } = JsonSchemaVersion.Draft06 | JsonSchemaVersion.Draft07 | JsonSchemaVersion.Draft08;
		public int ValidationSequence => 1;

		public string Value { get; private set; }

		public SchemaKeyword(string value)
		{
			Value = value;
		}

		public SchemaValidationResults Validate(SchemaValidationContext context)
		{
			return StringFormat.Uri.Validate(Value)
				       ? SchemaValidationResults.Valid
				       : new SchemaValidationResults(Name, "should be a valid URI");
		}
		public void FromJson(JsonValue json, JsonSerializer serializer)
		{
			Value = json.String;
		}
		public JsonValue ToJson(JsonSerializer serializer)
		{
			return Value;
		}
	}
}