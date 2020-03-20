﻿using System.Collections;
using System.Linq;
using Manatee.Json.Schema;
using NUnit.Framework;

namespace Manatee.Json.Tests.Schema
{
	[TestFixture]
	public class NumberSchemaTest
	{
		[OneTimeSetUp]
		public void Setup()
		{
			JsonOptions.LogCategory = LogCategory.Schema;
		}

		public static IEnumerable TypeData
		{
			get
			{
				yield return new JsonSchema().Type(JsonSchemaType.Number);
			}
		}
		[TestCaseSource(nameof(TypeData))]
		public void ValidateReturnsErrorOnNonNumber(JsonSchema schema)
		{
			var json = new JsonObject();

			var results = schema.Validate(json);

			results.AssertInvalid();
		}
		
		public static IEnumerable MinimumData
		{
			get
			{
				yield return new JsonSchema().Type(JsonSchemaType.Number).Minimum(5);
			}
		}
		[TestCaseSource(nameof(MinimumData))]
		public void ValidateReturnsErrorOnLessThanMinimum(JsonSchema schema)
		{
			var json = (JsonValue) 4;

			var results = schema.Validate(json);

			results.AssertInvalid();
		}
		[TestCaseSource(nameof(MinimumData))]
		public void ValidateReturnsValidOnMoreThanMinimum(JsonSchema schema)
		{
			var json = (JsonValue) 10;

			var results = schema.Validate(json);

			results.AssertValid();
		}
		
		[Test]
		public void Draft04_ValidateReturnsErrorOnEqualsExclusiveMinimum()
		{
			var schema = new JsonSchema()
				.Type(JsonSchemaType.Number)
				.Minimum(5)
				.ExclusiveMinimumDraft04(true);
			var json = (JsonValue) 5;

			var results = schema.Validate(json);

			results.AssertInvalid();
		}
		[Test]
		public void Draft04_ValidateReturnsValidOnMoreThanExclusiveMinimum()
		{
			var schema = new JsonSchema()
				.Type(JsonSchemaType.Number)
				.Minimum(5)
				.ExclusiveMinimumDraft04(true);
			var json = (JsonValue) 10;

			var results = schema.Validate(json);

			results.AssertValid();
		}
		[Test]
		public void ValidateReturnsErrorOnEqualsExclusiveMinimum()
		{
			var schema = new JsonSchema()
				.Type(JsonSchemaType.Number)
				.ExclusiveMinimum(5);
			var json = (JsonValue) 5;

			var results = schema.Validate(json);

			results.AssertInvalid();
		}
		[Test]
		public void ValidateReturnsValidOnMoreThanExclusiveMinimum()
		{
			var schema = new JsonSchema()
				.Type(JsonSchemaType.Number)
				.ExclusiveMinimum(5);
			var json = (JsonValue) 10;

			var results = schema.Validate(json);

			results.AssertValid();
		}
		
		public static IEnumerable MaximumData
		{
			get
			{
				yield return new TestCaseData(new JsonSchema().Type(JsonSchemaType.Number).Maximum(5));
			}
		}
		[TestCaseSource(nameof(MaximumData))]
		public void ValidateReturnsErrorOnMoreThanMaximum(JsonSchema schema)
		{
			var json = (JsonValue) 10;

			var results = schema.Validate(json);

			results.AssertInvalid();
		}
		[TestCaseSource(nameof(MaximumData))]
		public void ValidateReturnsValidOnLessThanMaximum(JsonSchema schema)
		{
			var json = (JsonValue) 3;

			var results = schema.Validate(json);

			results.AssertValid();
		}
		
		[Test]
		public void Draft04_ValidateReturnsErrorOnEqualsExclusiveMaximum()
		{
			var schema = new JsonSchema()
				.Type(JsonSchemaType.Number)
				.Maximum(5)
				.ExclusiveMaximumDraft04(true);
			var json = (JsonValue) 5;

			var results = schema.Validate(json);

			results.AssertInvalid();
		}
		[Test]
		public void Draft04_ValidateReturnsValidOnLessThanExclusiveMaximum()
		{
			var schema = new JsonSchema()
				.Type(JsonSchemaType.Number)
				.Maximum(5)
				.ExclusiveMaximumDraft04(true);
			var json = (JsonValue) 3;

			var results = schema.Validate(json);

			results.AssertValid();
		}
		[Test]
		public void ValidateReturnsErrorOnEqualsExclusiveMaximum()
		{
			var schema = new JsonSchema()
				.Type(JsonSchemaType.Number)
				.ExclusiveMaximum(5);
			var json = (JsonValue) 5;

			var results = schema.Validate(json);

			results.AssertInvalid();
		}
		[Test]
		public void ValidateReturnsValidOnLessThanExclusiveMaximum()
		{
			var schema = new JsonSchema()
				.Type(JsonSchemaType.Number)
				.ExclusiveMaximum(5);
			var json = (JsonValue) 3;

			var results = schema.Validate(json);

			results.AssertValid();
		}
		
		public static IEnumerable MultipleOfData
		{
			get
			{
				yield return new TestCaseData(new JsonSchema().Type(JsonSchemaType.Number).MultipleOf(7.5));
			}
		}
		[TestCaseSource(nameof(MultipleOfData))]
		public void ValidateReturnsValidOnMultipleOf_Positive(JsonSchema schema)
		{
			var json = (JsonValue) 7.5;

			var results = schema.Validate(json);

			results.AssertValid();
		}
		[TestCaseSource(nameof(MultipleOfData))]
		public void ValidateReturnsValidOnMultipleOf_Negative(JsonSchema schema)
		{
			var json = (JsonValue) (-7.5);

			var results = schema.Validate(json);

			results.AssertValid();
		}
		[TestCaseSource(nameof(MultipleOfData))]
		public void ValidateReturnsValidOnMultipleOf_Zero(JsonSchema schema)
		{
			var json = (JsonValue) 0;

			var results = schema.Validate(json);

			results.AssertValid();
		}
		[TestCaseSource(nameof(MultipleOfData))]
		public void ValidateReturnsInvalidOnMultipleOf(JsonSchema schema)
		{
			var json = (JsonValue) 16;

			var results = schema.Validate(json);

			Assert.AreEqual(false, results.IsValid);
		}
	}
}
