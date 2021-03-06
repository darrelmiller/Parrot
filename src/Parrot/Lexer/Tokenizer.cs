﻿using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;

namespace Parrot.Lexer
{
    public class Tokenizer
    {
        private readonly List<Token> _tokens = new List<Token>();
        private int _currentIndex;
        private readonly StreamReader _reader;

        public Tokenizer(string source) : this(new MemoryStream(Encoding.Default.GetBytes(source))) { }

        public Tokenizer(Stream source)
        {
            _reader = new StreamReader(source);
        }

        private bool HasAvailableTokens()
        {
            return _reader.Peek() != -1;
        }

        private int Consume()
        {
            _currentIndex += 1;
            var character = _reader.Read();

            if (character == -1)
            {
                throw new EndOfStreamException();
            }

            return character;
        }

        private Token GetNextToken()
        {
            int peek = _reader.Peek();
            var currentCharacter = peek == -1 ? '\0' : (char)peek;

            if (IsIdentifierHead(currentCharacter))
            {
                return new IdentifierToken
                {
                    Content = ConsumeIdentifier(),
                    Index = _currentIndex,
                    Type = TokenType.Identifier
                };
            }

            if (IsWhitespace(currentCharacter))
            {
                return new WhitespaceToken
                {
                    Content = ConsumeWhitespace(),
                    Index = _currentIndex,
                    Type = TokenType.Whitespace
                };
            }

            switch (currentCharacter)
            {
                case ',': //this is for the future
                    Consume();
                    return new CommaToken { Index = _currentIndex };
                case '(': //parameter list start
                    Consume();
                    return new OpenParenthesisToken { Index = _currentIndex };
                case ')': //parameter list end
                    Consume();
                    return new CloseParenthesisToken { Index = _currentIndex };
                case '[': //attribute list start
                    Consume();
                    return new OpenBracketToken { Index = _currentIndex };
                case ']': //attribute list end
                    Consume();
                    return new CloseBracketToken { Index = _currentIndex };
                case '=': //attribute assignment, raw output
                    Consume();
                    return new EqualToken { Index = _currentIndex };
                case '{': //child block start
                    Consume();
                    return new OpenBracesToken { Index = _currentIndex };
                case '}': //child block end
                    Consume();
                    return new CloseBracesToken { Index = _currentIndex };
                case '>': //child assignment
                    Consume();
                    return new GreaterThanToken { Index = _currentIndex };
                case '+': //sibling assignment
                    Consume();
                    return new PlusToken { Index = _currentIndex };
                case '|': //string literal pipe
                    return new StringLiteralPipeToken
                    {
                        Content = ConsumeUntilNewline(),
                        Type = TokenType.StringLiteralPipe,
                        Index = _currentIndex
                    };
                case '"': //quoted string literal
                    return new QuotedStringLiteralToken
                    {
                        Content = ConsumeQuotedStringLiteral('"'),
                        Type = TokenType.QuotedStringLiteral,
                        Index = _currentIndex
                    };
                case '\'': //quoted string literal
                    return new QuotedStringLiteralToken
                    {
                        Content = ConsumeQuotedStringLiteral('\''),
                        Type = TokenType.QuotedStringLiteral,
                        Index = _currentIndex
                    };
                case '@': //multilinestringliteral
                    //read next token
                    Consume();
                    int nextCharacter = _reader.Peek();
                    char quoteType = nextCharacter == -1 ? '\0' : (char)nextCharacter;
                    return new MultilineStringLiteralToken
                    {
                        //Content = (char)Consume() + ReadUntil(c => IsNewLine(c) || c == nextCharacter) + (char)Consume(),
                        Type = TokenType.StringLiteralPipe,
                        Index = _currentIndex
                    };
                case ':': //Encoded output
                    Consume();
                    return new ColonToken { Index = _currentIndex };
                case '\0':
                    return null;
                default:
                    throw new UnexpectedTokenException(string.Format("Unexpected token: {0}", currentCharacter));
            }
        }

        private string ConsumeUntilNewline()
        {
            //(char)Consume() + ReadUntil(IsNewLine)
            StringBuilder sb = new StringBuilder();
            int peek = _reader.Peek();
            var currentCharacter = peek == -1 ? '\0' : (char)peek;

            while (!IsNewLine(currentCharacter))
            {
                Consume();
                sb.Append(currentCharacter);
                peek = _reader.Peek();
                currentCharacter = peek == -1 ? '\0' : (char)peek;
            }

            return sb.ToString();
        }

        private string ConsumeIdentifier()
        {
            StringBuilder sb = new StringBuilder();
            int peek = _reader.Peek();
            var currentCharacter = peek == -1 ? '\0' : (char)peek;

            while ((IsIdTail(currentCharacter)))
            {
                Consume();
                sb.Append(currentCharacter);
                peek = _reader.Peek();
                currentCharacter = peek == -1 ? '\0' : (char)peek;
            }

            return sb.ToString();
        }

        private string ConsumeWhitespace()
        {
            StringBuilder sb = new StringBuilder();
            int peek = _reader.Peek();
            var currentCharacter = peek == -1 ? '\0' : (char)peek;

            while ((IsWhitespace(currentCharacter)))
            {
                Consume();
                sb.Append(currentCharacter);
                peek = _reader.Peek();
                currentCharacter = peek == -1 ? '\0' : (char)peek;
            }

            return sb.ToString();
        }

        private string ConsumeQuotedStringLiteral(char quote)
        {
            StringBuilder sb = new StringBuilder();
            sb.Append((char)Consume());
            int peek = _reader.Peek();
            var currentCharacter = peek == -1 ? '\0' : (char)peek;

            while ((!IsNewLine(currentCharacter) && currentCharacter != quote))
            {
                Consume();
                sb.Append(currentCharacter);
                peek = _reader.Peek();
                currentCharacter = peek == -1 ? '\0' : (char)peek;
            }

            sb.Append((char)Consume());
            return sb.ToString();
        }

        private bool IsWhitespace(char character)
        {
            return
                   character == '\r' ||
                   character == '\n' ||
                   character == ' ' ||
                   character == '\f' ||
                   character == '\t' ||
                   character == '\u000B' || // Vertical Tab
                   Char.GetUnicodeCategory(character) == UnicodeCategory.SpaceSeparator;
        }

        private bool IsIdentifierHead(char character)
        {
            return Char.IsLetter(character) ||
                   character == '_' ||
                   character == '#' ||
                   character == '.' ||
                   Char.GetUnicodeCategory(character) == UnicodeCategory.LetterNumber;
        }

        private bool IsIdTail(char character)
        {
            return Char.IsDigit(character) ||
                   IsIdentifierHead(character) ||
                   character == ':' ||
                   character == '-' ||
                   IsIdentifierUnicode(character);
        }

        private bool IsIdentifierUnicode(char character)
        {
            UnicodeCategory category = Char.GetUnicodeCategory(character);

            return category == UnicodeCategory.NonSpacingMark ||
                   category == UnicodeCategory.SpacingCombiningMark ||
                   category == UnicodeCategory.ConnectorPunctuation ||
                   category == UnicodeCategory.Format;
        }

        private bool IsNewLine(char character)
        {
            return character == '\r' // Carriage return
                || character == '\n' // Linefeed
                || character == '\u0085' // Next Line
                || character == '\u2028' // Line separator
                || character == '\u2029'; // Paragraph separator
        }

        public List<Token> Tokenize()
        {
            try
            {
                Token token;
                while ((token = GetNextToken()) != null)
                {
                    _tokens.Add(token);
                }
            }
            catch(EndOfStreamException)
            {
                throw new ParserException("");
            }

            return _tokens;
        }

        public IList<Token> Tokens()
        {
            return Tokenize();
        }
    }
}
