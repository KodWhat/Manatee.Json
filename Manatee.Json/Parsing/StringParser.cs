﻿using System;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Manatee.Json.Internal;

namespace Manatee.Json.Parsing
{
	internal class StringParser : IJsonParser
	{
		public bool Handles(char c)
		{
			return c == '\"';
		}

		public bool TryParse(string source, ref int index, [NotNullWhen(true)] out JsonValue? value, [NotNullWhen(false)] out string? errorMessage, bool allowExtraChars)
		{
			System.Diagnostics.Debug.Assert(index < source.Length && source[index] == '"');

			value = null;

			bool complete = false;
			bool mustInterpret = false;
			int originalIndex = ++index; // eat initial '"'
			while (index < source.Length)
			{
				if (source[index] == '\\')
				{
					mustInterpret = true;
					break;
				}
				if (source[index] == '"')
				{
					complete = true;
					break;
				}

				index++;
			}

			if (!mustInterpret)
			{
				if (!complete)
				{
					errorMessage = "Could not find end of string value.";
					return false;
				}

				value = source.Substring(originalIndex, (index - originalIndex));

				index += 1; // eat the trailing '"'
				errorMessage = null;
				return true;
			}

			index = originalIndex;
			return _TryParseInterpretedString(source, ref index, out value, out errorMessage);
		}

		public bool TryParse(TextReader stream, [NotNullWhen(true)] out JsonValue? value, [NotNullWhen(false)] out string? errorMessage)
		{
			value = null;

			System.Diagnostics.Debug.Assert(stream.Peek() == '"');
			stream.Read(); // waste the '"'

			var builder = StringBuilderCache.Acquire();

			var complete = false;
			bool mustInterpret = false;
			while (stream.Peek() != -1)
			{
				var c = (char)stream.Peek();
				if (c == '\\')
				{
					mustInterpret = true;
					break;
				}

				stream.Read(); // eat the character
				if (c == '"')
				{
					complete = true;
					break;
				}

				builder.Append(c);
			}

			// if there are not any escape sequences--most of a JSON's strings--just
			// return the string as-is.
			if (!mustInterpret)
			{
				if (!complete)
				{
					errorMessage = "Could not find end of string value.";
					return false;
				}

				value = StringBuilderCache.GetStringAndRelease(builder);
				errorMessage = null;
				return true;
			}
			
			// NOTE: TryParseInterpretedString is responsible for releasing builder
			// NOTE: TryParseInterpretedString assumes stream is sitting at the '\\'
			return _TryParseInterpretedString(builder, stream, out value, out errorMessage);
		}

		public async Task<(string? errorMessage, JsonValue? value)> TryParseAsync(TextReader stream, CancellationToken token)
		{
			var scratch = SmallBufferCache.Acquire(4);

			await stream.TryRead(scratch, 0, 1, token); // waste the '"'
			System.Diagnostics.Debug.Assert(scratch[0] == '"');

			var builder = StringBuilderCache.Acquire();

			var complete = false;
			bool mustInterpret = false;
			while (stream.Peek() != -1)
			{
				var c = (char)stream.Peek();
				if (c == '\\')
				{
					mustInterpret = true;
					break;
				}

				await stream.TryRead(scratch, 0, 1, token); // eat the character
				if (c == '"')
				{
					complete = true;
					break;
				}

				builder.Append(c);
			}

			// if there are not any escape sequences--most of a JSON's strings--just
			// return the string as-is.
			if (!mustInterpret)
			{
				SmallBufferCache.Release(scratch);

				string? errorMessage = null;
				JsonValue? value = null;
				if (!complete)
					errorMessage = "Could not find end of string value.";
				else
					value = StringBuilderCache.GetStringAndRelease(builder);

				return (errorMessage, value);
			}
			
			// NOTE: TryParseInterpretedString is responsible for releasing builder
			// NOTE: TryParseInterpretedString assumes stream is sitting at the '\\'
			// NOTE: TryParseInterpretedString assumes scratch can hold at least 4 chars
			return await _TryParseInterpretedStringAsync(builder, stream, scratch);
		}

		private static bool _TryParseInterpretedString(string source, ref int index, [NotNullWhen(true)] out JsonValue? value, [NotNullWhen(false)] out string? errorMessage)
		{
			value = null;
			errorMessage = null;

			var builder = StringBuilderCache.Acquire();
			var complete = false;
			while (index < source.Length)
			{
				var c = source[index++];
				if (c != '\\')
				{
					if (c == '"')
					{
						complete = true;
						break;
					}

					builder.Append(c);
				}
				else
				{
					if (index >= source.Length)
					{
						errorMessage = "Could not find end of string value.";
						return false;
					}

					string append = null!;
					c = source[index++];
					switch (c)
					{
						case 'b':
							append = "\b";
							break;
						case 'f':
							append = "\f";
							break;
						case 'n':
							append = "\n";
							break;
						case 'r':
							append = "\r";
							break;
						case 't':
							append = "\t";
							break;
						case 'u':
							var length = 4;
							if (index + length >= source.Length)
							{
								errorMessage = $"Invalid escape sequence: '\\{c}{source.Substring(index)}'.";
								break;
							}

							if (!_IsValidHex(source, index, 4) ||
							    !int.TryParse(source.Substring(index, 4), NumberStyles.HexNumber, CultureInfo.InvariantCulture, out int hex))
							{
								errorMessage = $"Invalid escape sequence: '\\{c}{source.Substring(index, length)}'.";
								break;
							}

							if (index + length + 2 < source.Length &&
							    source.IndexOf("\\u", index + length, 2, StringComparison.InvariantCulture) == index + length)
							{
								// +2 from \u
								// +4 from the next four hex chars
								length += 6;

								if (index + length >= source.Length)
								{
									errorMessage = $"Invalid escape sequence: '\\{c}{source.Substring(index)}'.";
									break;
								}

								if (!_IsValidHex(source, index + 6, 4) ||
								    !int.TryParse(source.Substring(index + 6, 4), NumberStyles.HexNumber, CultureInfo.InvariantCulture, out int hex2))
								{
									errorMessage = $"Invalid escape sequence: '\\{c}{source.Substring(index, length)}'.";
									break;
								}

								var surrogatePairHex = StringExtensions.CalculateUtf32(hex, hex2);

								if (surrogatePairHex.IsValidUtf32CodePoint())
									hex = surrogatePairHex;
								else
									length -= 6;
							}

							append = char.ConvertFromUtf32(hex);
							index += length;
							break;
						case '"':
							append = "\"";
							break;
						case '\\':
							append = "\\";
							break;
						// Is this correct?
						case '/':
							append = "/";
							break;
						default:
							complete = true;
							errorMessage = $"Invalid escape sequence: '\\{c}'.";
							break;
					}

					if (append == null) break;

					builder.Append(append);
				}
			}

			if (!complete || errorMessage != null)
			{
				value = null;
				StringBuilderCache.Release(builder);
				errorMessage ??= "Could not find end of string value.";
				return false;
			}
			value = StringBuilderCache.GetStringAndRelease(builder);
			return true;
		}

		private static bool _TryParseInterpretedString(StringBuilder builder, TextReader stream, [NotNullWhen(true)] out JsonValue? value, [NotNullWhen(false)] out string? errorMessage)
		{
			// NOTE: `builder` contains the portion of the string found in `stream`, up to the first
			//       (possible) escape sequence.
			System.Diagnostics.Debug.Assert('\\' == (char)stream.Peek());

			value = null;

			bool complete = false;

			int? previousHex = null;

			while (stream.Peek() != -1)
			{
				var c = (char)stream.Read();

				if (c == '\\')
				{
					if (stream.Peek() == -1)
					{
						StringBuilderCache.Release(builder);
						errorMessage = "Could not find end of string value.";
						return false;
					}

					// escape sequence
					var lookAhead = (char)stream.Peek();
					if (!_MustInterpretComplex(lookAhead))
					{
						stream.Read(); // eat the simple escape
						c = _InterpretSimpleEscapeSequence(lookAhead);
					}
					else
					{
						// NOTE: Currently we only handle 'u' here
						if (lookAhead != 'u')
						{
							StringBuilderCache.Release(builder);
							errorMessage = $"Invalid escape sequence: '\\{lookAhead}'.";
							return false;
						}

						var buffer = SmallBufferCache.Acquire(4);
						stream.Read(); // eat the 'u'
						if (4 != stream.Read(buffer, 0, 4))
						{
							StringBuilderCache.Release(builder);
							errorMessage = "Could not find end of string value.";
							return false;
						}

						var hexString = new string(buffer, 0, 4);
						if (!_IsValidHex(hexString, 0, 4) ||
						    !int.TryParse(hexString, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out var currentHex))
						{
							StringBuilderCache.Release(builder);
							errorMessage = $"Invalid escape sequence: '\\{lookAhead}{hexString}'.";
							return false;
						}

						if (previousHex != null)
						{
							// Our last character was \u, so combine and emit the UTF32 codepoint
							var surrogateHex = StringExtensions.CalculateUtf32(previousHex.Value, currentHex);
							if (surrogateHex.IsValidUtf32CodePoint())
								builder.Append(char.ConvertFromUtf32(surrogateHex));
							else
							{
								builder.Append((char) previousHex.Value);
								builder.Append((char) currentHex);
							}
							previousHex = null;
						}
						else
							previousHex = currentHex;

						SmallBufferCache.Release(buffer);
						continue;
					}
				}
				else if (c == '"')
				{
					complete = true;
					break;
				}

				// Check if last character was \u, and if so emit it as-is, because
				// this character is not a continuation of a UTF-32 escape sequence
				if (previousHex != null)
				{
					builder.Append(char.ConvertFromUtf32(previousHex.Value));
					previousHex = null;
				}

				// non-escape sequence
				builder.Append(c);
			}

			// if we had a hanging UTF32 escape sequence, apply it now
			if (previousHex != null)
				builder.Append(char.ConvertFromUtf32(previousHex.Value));

			if (!complete)
			{
				value = null;
				StringBuilderCache.Release(builder);
				errorMessage = "Could not find end of string value.";
				return false;
			}

			value = StringBuilderCache.GetStringAndRelease(builder);
			errorMessage = null;
			return true;
		}

		private static bool _IsValidHex(string source, int offset, int count)
		{
			for (int i = offset; i < offset + count; ++i)
			{
				// if not a hex digit
				if ((source[i] < '0' || source[i] > '9') &&
				    (source[i] < 'A' || source[i] > 'F') &&
				    (source[i] < 'a' || source[i] > 'f'))
					return false;
			}

			return true;
		}

		/// <summary>
		/// Indicates whether or not the lookahead character is a 
		/// complex escape code.
		/// </summary>
		/// <param name="lookAhead">Lookahead character.</param>
		/// <returns><c>true</c> if and only if <paramref name="lookAhead"/>
		/// would require complex interpretation.</returns>
		private static bool _MustInterpretComplex(char lookAhead)
		{
			switch (lookAhead)
			{
				// These escape 'as-is'
				case '"':
				case '/':
				case '\\':
					return false;
				// These escape as an escape char
				case 'b':
				case 'f':
				case 'n':
				case 'r':
				case 't':
					return false;
				// this requires more work...
				case 'u':
					return true;
					// this is an error, which our complex handler will report
				default:
					return true;
			}
		}

		private static char _InterpretSimpleEscapeSequence(char lookAhead)
		{
			return lookAhead switch
				{
					'b' => '\b',
					'f' => '\f',
					'n' => '\n',
					'r' => '\r',
					't' => '\t',
					_ => lookAhead
				};
		}

		private static async Task<(string? errorMessage, JsonValue? value)> _TryParseInterpretedStringAsync(StringBuilder builder, TextReader stream, char[] scratch)
		{
			// NOTE: `builder` contains the portion of the string found in `stream`, up to the first
			//       (possible) escape sequence.
			System.Diagnostics.Debug.Assert('\\' == (char)stream.Peek());
			System.Diagnostics.Debug.Assert(scratch.Length >= 4);

			bool complete = false;

			int? previousHex = null;

			while (stream.Peek() != -1)
			{
				await stream.TryRead(scratch, 0, 1); // eat this character

				var c = scratch[0];
				if (c == '\\')
				{
					if (stream.Peek() == -1)
					{
						StringBuilderCache.Release(builder);
						SmallBufferCache.Release(scratch);
						return ("Could not find end of string value.", null);
					}

					// escape sequence
					var lookAhead = (char)stream.Peek();
					if (!_MustInterpretComplex(lookAhead))
					{
						await stream.TryRead(scratch, 0, 1); // eat the simple escape
						c = _InterpretSimpleEscapeSequence(lookAhead);
					}
					else
					{
						// NOTE: Currently we only handle 'u' here
						if (lookAhead != 'u')
						{
							StringBuilderCache.Release(builder);
							SmallBufferCache.Release(scratch);
							return ($"Invalid escape sequence: '\\{lookAhead}'.", null);
						}

						await stream.TryRead(scratch, 0, 1); // eat the 'u'
						var charsRead = await stream.ReadAsync(scratch, 0, 4);
						if (charsRead < 4)
						{
							StringBuilderCache.Release(builder);
							SmallBufferCache.Release(scratch);
							return ("Could not find end of string value.", null);
						}

						var hexString = new string(scratch, 0, 4);
						var currentHex = int.Parse(hexString, NumberStyles.HexNumber);

						if (previousHex != null)
						{
							// Our last character was \u, so combine and emit the UTF32 codepoint
							var surrogateHex = StringExtensions.CalculateUtf32(previousHex.Value, currentHex);
							if (surrogateHex.IsValidUtf32CodePoint())
								builder.Append(char.ConvertFromUtf32(surrogateHex));
							else
							{
								builder.Append((char)previousHex.Value);
								builder.Append((char)currentHex);
							}
							previousHex = null;
						}
						else
							previousHex = currentHex;

						continue;
					}
				}
				else if (c == '"')
				{
					complete = true;
					break;
				}

				// Check if last character was \u, and if so emit it as-is, because
				// this character is not a continuation of a UTF-32 escape sequence
				if (previousHex != null)
				{
					builder.Append(char.ConvertFromUtf32(previousHex.Value));
					previousHex = null;
				}

				// non-escape sequence
				builder.Append(c);
			}

			SmallBufferCache.Release(scratch);

			// if we had a hanging UTF32 escape sequence, apply it now
			if (previousHex != null)
				builder.Append(char.ConvertFromUtf32(previousHex.Value));

			if (!complete)
			{
				StringBuilderCache.Release(builder);
				return ("Could not find end of string value.", null);
			}

			JsonValue value = StringBuilderCache.GetStringAndRelease(builder);
			return (null, value);
		}
	}
}
