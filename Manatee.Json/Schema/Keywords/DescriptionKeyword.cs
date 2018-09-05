﻿using Manatee.Json.Serialization;

namespace Manatee.Json.Schema
{
	public class DescriptionKeyword : IJsonSchemaKeyword
	{
		public string Name => "description";
		public JsonSchemaVersion SupportedVersions { get; } = JsonSchemaVersion.All;

		public string Value { get; private set; }

		public DescriptionKeyword(string value)
		{
			Value = value;
		}

		public SchemaValidationResults Validate(SchemaValidationContext context)
		{
			return SchemaValidationResults.Valid;
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