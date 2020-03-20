﻿namespace Manatee.Json.Serialization
{
	/// <summary>
	/// Defines a custom serializer.
	/// </summary>
	public interface ISerializer
	{
		/// <summary>
		/// Determines if this serializer should maintain referential integrity.
		/// </summary>
		bool ShouldMaintainReferences { get; }

		/// <summary>
		/// Determines whether the serializer handles a specific type or JSON value given the current options.
		/// </summary>
		/// <param name="context"></param>
		/// <returns>true if the serializer is up to the task; false otherwise.</returns>
		bool Handles(SerializationContextBase context);
		/// <summary>
		/// Serializes a value.
		/// </summary>
		/// <param name="context"></param>
		/// <returns>A <see cref="JsonValue"/> that represents the value.</returns>
		JsonValue Serialize(SerializationContext context);
		/// <summary>
		/// Deserializes a <see cref="JsonValue"/> into a value.
		/// </summary>
		/// <param name="context"></param>
		/// <returns>The typed value represented by the JSON data.</returns>
		object Deserialize(DeserializationContext context);
	}
}