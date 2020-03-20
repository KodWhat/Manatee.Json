﻿using System;
using System.Collections.Generic;
using Manatee.Json.Pointer;
using Manatee.Json.Serialization;
using System.Linq;

namespace Manatee.Json.Patch
{
	/// <summary>
	/// Defines an action that can be applied within a JSON Patch document.
	/// </summary>
	public class JsonPatchAction : IJsonSerializable
	{
		/// <summary>
		/// Gets or sets the operation.
		/// </summary>
		public JsonPatchOperation Operation { get; set; }
		/// <summary>
		/// Gets or sets the path.
		/// </summary>
		public string Path { get; set; } = default!;
		/// <summary>
		/// Gets or sets the source for a value.
		/// </summary>
		public string? From { get; set; }
		/// <summary>
		/// Gets or sets a discrete value to be used.
		/// </summary>
		public JsonValue? Value { get; set; }

		internal JsonPatchResult TryApply(JsonValue json)
		{
			switch (Operation)
			{
				case JsonPatchOperation.Add:
					return _Add(json);
				case JsonPatchOperation.Remove:
					return _Remove(json);
				case JsonPatchOperation.Replace:
					return _Replace(json);
				case JsonPatchOperation.Move:
					return _Move(json);
				case JsonPatchOperation.Copy:
					return _Copy(json);
				case JsonPatchOperation.Test:
					return _Test(json);
				default:
					throw new ArgumentOutOfRangeException();
			}
		}

		void IJsonSerializable.FromJson(JsonValue json, JsonSerializer serializer)
		{
			var obj = json.Object;
			Operation = serializer.Deserialize<JsonPatchOperation>(obj["op"]);
			Path = obj.TryGetString("path")!;
			From = obj.TryGetString("from");
			obj.TryGetValue("value", out var jsonValue);
			Value = jsonValue;

			_Validate();
		}
		JsonValue IJsonSerializable.ToJson(JsonSerializer serializer)
		{
			var json = new JsonObject
				{
					["op"] = serializer.Serialize(Operation),
					["path"] = Path
				};
			if (From != null)
				json["from"] = From;
			if (Value != null)
				json["value"] = Value;

			return json;
		}

		private JsonPatchResult _Add(JsonValue json)
		{
			var (result, success) = JsonPointerFunctions.InsertValue(json, Path, Value!, false);

			if (!success) return new JsonPatchResult(json, "Could not add the value");
			
			return new JsonPatchResult(result!);
		}

		private JsonPatchResult _Remove(JsonValue json)
		{
			return _RemoveAtPath(json, Path);
		}

		private JsonPatchResult _Replace(JsonValue json)
		{
			// TODO: This isn't the most efficient way to do this, but it'll get the job done.
			var remove = _Remove(json);
			return remove.Success ? _Add(json) : remove;
		}

		private JsonPatchResult _Move(JsonValue json)
		{
			// TODO: This isn't the most efficient way to do this, but it'll get the job done.
			if(JsonPointer.Parse(From!).Equals(JsonPointer.Parse(Path)))
				return new JsonPatchResult(json);

			var copy = _Copy(json);
			return !copy.Success ? copy : _RemoveAtPath(json, From!);
		}

		private JsonPatchResult _Copy(JsonValue json)
		{
			var results = JsonPointer.Parse(From!).Evaluate(json);
			if (results.Error != null) return new JsonPatchResult(json, results.Error);
			
			var (result, success) = JsonPointerFunctions.InsertValue(json, Path, results.Result!, true);
			if (!success) return new JsonPatchResult(json, "Could not add the value");
			
			return new JsonPatchResult(result!);
		}

		private JsonPatchResult _Test(JsonValue json)
		{
			var results = JsonPointer.Parse(Path).Evaluate(json);
			if (results.Error != null) return new JsonPatchResult(json, results.Error);

			if (results.Result != Value) return new JsonPatchResult(json, $"The value at '{Path}' is not the expected value.");
			
			return new JsonPatchResult(json);
		}

		private JsonPatchResult _RemoveAtPath(JsonValue json, string path)
		{
			if (string.IsNullOrEmpty(Path))
			{
				json.Object.Clear();
				return new JsonPatchResult(json);
			}

			var (target, key, index, found) = JsonPointerFunctions.ResolvePointer(json, path);
			if (!found) return new JsonPatchResult(json, $"Path '{path}' not found.");

			switch (target!.Type)
			{
				case JsonValueType.Object:
					target.Object.Remove(key!);
					break;
				case JsonValueType.Array:
					target.Array.RemoveAt(index);
					break;
				default:
					return new JsonPatchResult(json, $"Cannot remove a value from a '{target.Type}'");
			}

			return new JsonPatchResult(json);
		}
		private void _Validate()
		{
			var errors = new List<string>();
			switch (Operation)
			{
				case JsonPatchOperation.Add:
				case JsonPatchOperation.Replace:
				case JsonPatchOperation.Test:
					_CheckProperty(Path, Operation, "path", errors);
					_CheckProperty(Value!, Operation, "value", errors);
					break;
				case JsonPatchOperation.Remove:
					_CheckProperty(Path, Operation, "path", errors);
					break;
				case JsonPatchOperation.Move:
				case JsonPatchOperation.Copy:
					_CheckProperty(Path, Operation, "path", errors);
					_CheckProperty(From!, Operation, "from", errors);
					break;
				default:
					throw new ArgumentOutOfRangeException();
			}
		}
		private static void _CheckProperty(object obj, JsonPatchOperation operation, string propertyName, List<string> errors)
		{
			if (obj != null) return;
			errors.Add($"Operation '{operation}' requires a {propertyName}.");
		}
	}
}