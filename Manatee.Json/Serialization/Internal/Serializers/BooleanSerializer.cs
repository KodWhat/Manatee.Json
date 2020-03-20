﻿using JetBrains.Annotations;

namespace Manatee.Json.Serialization.Internal.Serializers
{
	[UsedImplicitly]
	internal class BooleanSerializer : IPrioritizedSerializer
	{
		public int Priority => 1;

		public bool ShouldMaintainReferences => false;

		public bool Handles(SerializationContextBase context)
		{
			return context.InferredType == typeof(bool);
		}
		public JsonValue Serialize(SerializationContext context)
		{
			return (bool) context.Source!;
		}
		public object Deserialize(DeserializationContext context)
		{
			return context.LocalValue.Boolean;
		}
	}
}