using System;
using System.Collections.Generic;
using System.Linq;
namespace MathParser;

internal record Keyword(string Word, int OriginalPosition, KeywordType Type, KeywordSubtype? Subtype);
