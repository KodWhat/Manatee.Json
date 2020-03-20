﻿using System;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Manatee.Json.Internal;

namespace Manatee.Json.Parsing
{
	internal class NullParser : IJsonParser
	{
		private const string _unexpectedEndOfInput = "Unexpected end of input.";

		public bool Handles(char c)
		{
			return c == 'n' || c == 'N';
		}

		public bool TryParse(string source, ref int index, [NotNullWhen(true)] out JsonValue? value, [NotNullWhen(false)] out string? errorMessage, bool allowExtraChars)
		{
			value = null;

			if (index + 4 > source.Length)
			{
				errorMessage = _unexpectedEndOfInput;
				return false;
			}

			if (source.IndexOf("null", index, 4, StringComparison.OrdinalIgnoreCase) != index)
			{
				errorMessage = $"Value not recognized: '{source.Substring(index, 4)}'.";
				return false;
			}

			index += 4;
			value = JsonValue.Null;
			errorMessage = null;
			return true;
		}

		public bool TryParse(TextReader stream, [NotNullWhen(true)] out JsonValue? value, [NotNullWhen(false)] out string? errorMessage)
		{
			value = null;

			var buffer = SmallBufferCache.Acquire(4);
			var charsRead = stream.ReadBlock(buffer, 0, 4);
			if (charsRead != 4)
			{
				SmallBufferCache.Release(buffer);
				errorMessage = _unexpectedEndOfInput;
				return false;
			}

			if ((buffer[0] == 'n' || buffer[0] == 'N') &&
			    (buffer[1] == 'u' || buffer[1] == 'U') &&
			    (buffer[2] == 'l' || buffer[2] == 'L') &&
			    (buffer[3] == 'l' || buffer[3] == 'L'))
				value = JsonValue.Null;
			else
			{
				errorMessage = $"Value not recognized: '{new string(buffer).Trim('\0')}'.";
				return false;
			}

			SmallBufferCache.Release(buffer);
			errorMessage = null;
			return true;
		}
		public async Task<(string? errorMessage, JsonValue? value)> TryParseAsync(TextReader stream, CancellationToken token)
		{
			var buffer = SmallBufferCache.Acquire(4);
			var count = await stream.ReadBlockAsync(buffer, 0, 4);
			if (count < 4)
			{
				SmallBufferCache.Release(buffer);
				return ("Unexpected end of input.", null);
			}

			JsonValue? value = null;
			string? errorMessage = null;
			if ((buffer[0] == 'n' || buffer[0] == 'N') &&
				(buffer[1] == 'u' || buffer[1] == 'U') &&
				(buffer[2] == 'l' || buffer[2] == 'L') &&
				(buffer[3] == 'l' || buffer[3] == 'L'))
				value = JsonValue.Null;
			else
				errorMessage = $"Value not recognized: '{new string(buffer).Trim('\0')}'.";

			SmallBufferCache.Release(buffer);
			return (errorMessage, value);
		}
	}
}