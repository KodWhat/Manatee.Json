﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Manatee.Json.Internal;

namespace Manatee.Json
{
	/// <summary>
	/// Represents a collection of JSON values.
	/// </summary>
	/// <remarks>
	/// A value can consist of a string, a numeric value, a boolean (true or false), a null placeholder,
	/// a JSON array of values, or a nested JSON object.
	/// </remarks>
	public class JsonArray : List<JsonValue>
	{
		/// <summary>
		/// Defines how this <see cref="JsonArray"/> evaluates equality.
		/// </summary>
		public ArrayEquality EqualityStandard { get; set; } = JsonOptions.DefaultArrayEquality;

		/// <summary>
		/// Creates an empty instance of a JSON array.
		/// </summary>
		public JsonArray() {}
		/// <summary>
		/// Creates an instance of a JSON array and initializes it with the
		/// supplied JSON values.
		/// </summary>
		/// <param name="collection">A collection of <see cref="JsonValue"/>s.</param>
		public JsonArray(IEnumerable<JsonValue> collection)
			: base(collection.Select(jv => jv ?? JsonValue.Null)) { }

		/// <summary>
		/// Creates a string representation of the JSON data.
		/// </summary>
		/// <param name="indentLevel">The indention level for the array.</param>
		/// <returns>A string.</returns>
		public string GetIndentedString(int indentLevel = 0)
		{
			if (Count == 0) return "[]";

			var builder = new StringBuilder();
			AppendIndentedString(builder, indentLevel);
			return builder.ToString();
		}
		internal void AppendIndentedString(StringBuilder builder, int indentLevel)
		{
			if (Count == 0)
			{
				builder.Append("[]");
				return;
			}

			string tab0 = JsonOptions.PrettyPrintIndent.Repeat(indentLevel),
				   tab1 = tab0 + JsonOptions.PrettyPrintIndent;

			builder.Append("[\n");
			bool comma = false;
			foreach (var value in this)
			{
				if (comma)
					builder.Append(",\n");

				builder.Append(tab1);

				value.AppendIndentedString(builder, indentLevel + 1);

				comma = true;
			}
			builder.Append('\n');
			builder.Append(tab0);
			builder.Append(']');
		}
		/// <summary>
		/// Adds an object to the end of the <see cref="JsonArray"/>.
		/// </summary>
		/// <param name="item">The object to be added to the end of the <see cref="JsonArray"/>. If the value is null, it will be replaced by <see cref="JsonValue.Null"/>.</param>
		public new void Add(JsonValue item)
		{
			base.Add(item ?? JsonValue.Null);
		}
		/// <summary>
		/// Adds the elements of the specified collection to the end of the <see cref="JsonArray"/>.
		/// </summary>
		/// <param name="collection">The collection whose elements should be added to the end of the <see cref="JsonArray"/>. The collection itself cannot be null, but it can contain elements that are null.  These elements will be replaced by <see cref="JsonValue.Null"/></param>
		/// <exception cref="ArgumentNullException"><paramref name="collection"/> is null.</exception>
		public new void AddRange(IEnumerable<JsonValue> collection)
		{
			base.AddRange(collection.Select(v => v ?? JsonValue.Null));
		}
		/// <summary>
		/// Creates a string representation of the JSON data.
		/// </summary>
		/// <returns>A string.</returns>
		/// <remarks>
		/// Passing the returned string back into the parser will result in a copy of this Json array.
		/// </remarks>
		public override string ToString()
		{
			if (Count == 0) return "[]";

			var builder = new StringBuilder();
			AppendString(builder);
			return builder.ToString();
		}

		internal void AppendString(StringBuilder builder)
		{
			if (Count == 0)
			{
				builder.Append("[]");
				return;
			}

			builder.Append('[');
			bool comma = false;
			foreach (var value in this)
			{
				if (comma)
					builder.Append(',');

				value.AppendString(builder);

				comma = true;
			}
			builder.Append(']');
		}

		/// <summary>
		/// Determines whether the specified <see cref="object"/> is equal to the current <see cref="object"/>.
		/// </summary>
		/// <returns>
		/// true if the specified <see cref="object"/> is equal to the current <see cref="object"/>; otherwise, false.
		/// </returns>
		/// <param name="obj">The <see cref="object"/> to compare with the current <see cref="object"/>.</param>
		public override bool Equals(object? obj)
		{
			if (!(obj is JsonArray json)) return false;

			return EqualityStandard == ArrayEquality.SequenceEqual
				       ? this.SequenceEqual(json)
				       : this.ContentsEqual(json);
		}

		/// <summary>
		/// Serves as a hash function for a particular type. 
		/// </summary>
		/// <returns>
		/// A hash code for the current <see cref="object"/>.
		/// </returns>
		public override int GetHashCode()
		{
			// ReSharper disable once BaseObjectGetHashCodeCallInGetHashCode
			return this.GetCollectionHashCode();
		}
	}
}
