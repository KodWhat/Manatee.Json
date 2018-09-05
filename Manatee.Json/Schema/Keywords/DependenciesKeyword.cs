﻿using System.Collections.Generic;
using System.Linq;
using Manatee.Json.Serialization;

namespace Manatee.Json.Schema
{
	public class DependenciesKeyword : List<IJsonSchemaDependency>, IJsonSchemaKeyword
	{
		public virtual string Name => "dependencies";
		public virtual JsonSchemaVersion SupportedVersions { get; } = JsonSchemaVersion.All;
		public int ValidationSequence => 1;

		public SchemaValidationResults Validate(SchemaValidationContext context)
		{
			return new SchemaValidationResults(this.SelectMany(d => d.Validate(context).Errors));
		}
		public void FromJson(JsonValue json, JsonSerializer serializer)
		{
			foreach (var kvp in json.Object)
			{
				if (kvp.Value.Type == JsonValueType.Array)
					Add(new PropertyDependency(kvp.Key, kvp.Value.Array.Select(jv => jv.String)));
				else
					Add(new SchemaDependency(kvp.Key, serializer.Deserialize<JsonSchema>(kvp.Value)));
			}
		}
		public JsonValue ToJson(JsonSerializer serializer)
		{
			return this.ToDictionary(d => d.PropertyName,
			                         d => d.GetJsonData())
			           .ToJson();
		}
	}
}