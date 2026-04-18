using System;
using System.Collections.Generic;
using System.Globalization;

namespace xParLib
{
    /// <summary>
    /// Флаги классов символов для Charset.
    /// </summary>
    /// <remarks>Аналог CS_* флагов из charset.c оригинального par (Par 1.53.0).</remarks>
    [Flags]
    public enum CharsetFlag
    {
        UpperCase = 1,   // Все uppercase буквы (_A)
        LowerCase = 2,   // Все lowercase буквы (_a)
        NoCase = 4,      // Все neither-case буквы (_@)
        Digit = 8,       // Все десятичные цифры (_0)
        Space = 16,      // Все пробельные символы (_S)
    }

    /// <summary>
    /// Класс для работы с наборами символов (charset), соответствующий оригинальному par.
    /// Работает с grapheme clusters (строками C#), а не с отдельными char/байтами.
    /// </summary>
    /// <remarks>Аналог реализации из charset.c оригинального par (Par 1.53.0).</remarks>
    public class Charset
    {
        private readonly HashSet<string> _inList = new HashSet<string>(StringComparer.Ordinal);
        private readonly HashSet<string> _outList = new HashSet<string>(StringComparer.Ordinal);
        private readonly HashSet<CharsetFlag> _flags = new HashSet<CharsetFlag>();

        /// <summary>
        /// Парсит строку в формате charset syntax и возвращает новый Charset.
        /// </summary>
        public static Charset Parse(string str)
        {
            if (str == null) throw new ArgumentNullException(nameof(str));

            var charset = new Charset();
            var inList = new List<string>();

            for (int i = 0; i < str.Length; i++)
            {
                char c = str[i];

                if (c == '_')
                {
                    // Escape sequence
                    i++;
                    if (i >= str.Length)
                        throw new ArgumentException($"Bad charset syntax: {str}");

                    char esc = str[i];

                    switch (esc)
                    {
                        // Single character escapes
                        case '_': inList.Add("_"); break;
                        case 's': inList.Add(" "); break;
                        case 'b': inList.Add("\\"); break;
                        case 'q': inList.Add("'"); break;
                        case 'Q': inList.Add("\""); break;

                        // Hex character: _xHH (2 hex digits)
                        case 'x':
                            if (i + 2 >= str.Length)
                                throw new ArgumentException($"Bad charset syntax: {str}");

                            string hex2 = str.Substring(i + 1, 2);
                            if (!IsHexDigit(hex2[0]) || !IsHexDigit(hex2[1]))
                                throw new ArgumentException($"Bad charset syntax: {str}");

                            int codePoint = HexDigitToInt(hex2[0]) * 16 + HexDigitToInt(hex2[1]);
                            inList.Add(char.ConvertFromUtf32(codePoint));
                            i += 2;
                            break;

                        // Extended hex character: _XHHHH (4 hex digits)
                        case 'X':
                            if (i + 4 >= str.Length)
                                throw new ArgumentException($"Bad charset syntax: {str}");

                            string hex4 = str.Substring(i + 1, 4);
                            for (int h = 0; h < 4; h++)
                            {
                                if (!IsHexDigit(hex4[h]))
                                    throw new ArgumentException($"Bad charset syntax: {str}");
                            }

                            int codePoint4 = 
                                HexDigitToInt(hex4[0]) * 4096 +
                                HexDigitToInt(hex4[1]) * 256 +
                                HexDigitToInt(hex4[2]) * 16 +
                                HexDigitToInt(hex4[3]);
                            inList.Add(char.ConvertFromUtf32(codePoint4));
                            i += 4;
                            break;

                        // Character class flags
                        case 'A': charset._flags.Add(CharsetFlag.UpperCase); break;
                        case 'a': charset._flags.Add(CharsetFlag.LowerCase); break;
                        case '@': charset._flags.Add(CharsetFlag.NoCase); break;
                        case '0': charset._flags.Add(CharsetFlag.Digit); break;
                        case 'S': charset._flags.Add(CharsetFlag.Space); break;

                        default:
                            throw new ArgumentException($"Bad charset syntax: {str}");
                    }
                }
                else
                {
                    // Regular character — добавляем как односимвольную строку
                    // (если это начало surrogate pair, захватываем оба char)
                    if (char.IsHighSurrogate(c) && i + 1 < str.Length && char.IsLowSurrogate(str[i + 1]))
                    {
                        inList.Add(new string(new[] { c, str[i + 1] }));
                        i++;
                    }
                    else
                    {
                        inList.Add(c.ToString());
                    }
                }
            }

            // Add all collected characters to inList
            foreach (var s in inList)
            {
                charset._inList.Add(s);
            }

            return charset;
        }

        /// <summary>
        /// Проверяет, принадлежит ли графем-кластер этому набору.
        /// </summary>
        public bool IsMember(string grapheme)
        {
            if (grapheme == null) throw new ArgumentNullException(nameof(grapheme));
            if (grapheme.Length == 0) return false;

            // Если строка целиком в inList — она в множестве
            if (_inList.Contains(grapheme))
                return true;

            // Если строка целиком в outList — она НЕ в множестве
            if (_outList.Contains(grapheme))
                return false;

            // Проверяем по флагам — используем первый code point
            if (_flags.Count > 0)
            {
                return IsMemberByFlags(grapheme);
            }

            return false;
        }

        /// <summary>
        /// Возвращает объединение двух множеств.
        /// </summary>
        public Charset Union(Charset other)
        {
            if (other == null) throw new ArgumentNullException(nameof(other));

            var result = new Charset();

            // Объединяем флаги
            result._flags.UnionWith(_flags);
            result._flags.UnionWith(other._flags);

            // Собираем все уникальные символы из обоих множеств
            var allStrings = new HashSet<string>();
            allStrings.UnionWith(_inList);
            allStrings.UnionWith(_outList);
            allStrings.UnionWith(other._inList);
            allStrings.UnionWith(other._outList);

            foreach (var s in allStrings)
            {
                bool inThis = IsMember(s);
                bool inOther = other.IsMember(s);

                if (inThis || inOther)
                {
                    if (!result.IsMember(s))
                        result._inList.Add(s);
                }
                else
                {
                    if (result.IsMember(s))
                        result._outList.Add(s);
                }
            }

            return result;
        }

        /// <summary>
        /// Возвращает разность множеств (this - other).
        /// </summary>
        public Charset Difference(Charset other)
        {
            if (other == null) throw new ArgumentNullException(nameof(other));

            var result = new Charset();

            // Флаги: this & ~other
            foreach (var flag in _flags)
            {
                if (!other._flags.Contains(flag))
                    result._flags.Add(flag);
            }

            // Собираем все уникальные символы
            var allStrings = new HashSet<string>();
            allStrings.UnionWith(_inList);
            allStrings.UnionWith(_outList);
            allStrings.UnionWith(other._inList);
            allStrings.UnionWith(other._outList);

            foreach (var s in allStrings)
            {
                bool inThis = IsMember(s);
                bool inOther = other.IsMember(s);

                if (inThis && !inOther)
                {
                    if (!result.IsMember(s))
                        result._inList.Add(s);
                }
                else
                {
                    if (result.IsMember(s))
                        result._outList.Add(s);
                }
            }

            return result;
        }

        /// <summary>
        /// Добавляет символы из другого множества в текущее.
        /// </summary>
        public void Add(Charset other)
        {
            if (other == null) throw new ArgumentNullException(nameof(other));

            var union = Union(other);
            CopyFrom(union);
        }

        /// <summary>
        /// Удаляет символы из текущего множества.
        /// </summary>
        public void Remove(Charset other)
        {
            if (other == null) throw new ArgumentNullException(nameof(other));

            var diff = Difference(other);
            CopyFrom(diff);
        }

        /// <summary>
        /// Создаёт копию текущего множества.
        /// </summary>
        public Charset Clone()
        {
            var clone = new Charset();
            CopyTo(clone);
            return clone;
        }

        /// <summary>
        /// Заменяет содержимое текущего множества содержимым другого.
        /// Используется для операции '=' в charset-опциях (B=, P=, и т.д.).
        /// </summary>
        public void Replace(Charset source)
        {
            if (source == null) throw new ArgumentNullException(nameof(source));
            CopyFrom(source);
        }

        private void CopyFrom(Charset source)
        {
            _inList.Clear();
            _outList.Clear();
            _flags.Clear();

            foreach (var s in source._inList)
                _inList.Add(s);
            foreach (var s in source._outList)
                _outList.Add(s);
            foreach (var flag in source._flags)
                _flags.Add(flag);
        }

        private void CopyTo(Charset target)
        {
            foreach (var s in _inList)
                target._inList.Add(s);
            foreach (var s in _outList)
                target._outList.Add(s);
            foreach (var flag in _flags)
                target._flags.Add(flag);
        }

        private bool IsMemberByFlags(string grapheme)
        {
            // Берём первый code point графемы
            int codePoint = char.ConvertToUtf32(grapheme, 0);
            char ch = char.IsSurrogatePair(grapheme, 0) 
                ? char.ConvertFromUtf32(codePoint)[0] 
                : grapheme[0];

            bool isLetter = char.IsLetter(ch);
            bool isLower = char.IsLower(ch);
            bool isUpper = char.IsUpper(ch);

            // Логика для case-sensitive символов (как в оригинальном charset.c)
            if (_flags.Contains(CharsetFlag.NoCase))
            {
                // NoCase включает все буквы, если не исключены флагами
                if (isLetter)
                {
                    if ((_flags.Contains(CharsetFlag.LowerCase) || !isLower) &&
                        (_flags.Contains(CharsetFlag.UpperCase) || !isUpper))
                        return true;
                }
            }
            else
            {
                if ((_flags.Contains(CharsetFlag.LowerCase) && isLower) ||
                    (_flags.Contains(CharsetFlag.UpperCase) && isUpper))
                    return true;
            }

            if (_flags.Contains(CharsetFlag.Digit) && char.IsDigit(ch))
                return true;

            if (_flags.Contains(CharsetFlag.Space) && char.IsWhiteSpace(ch))
                return true;

            return false;
        }

        private static bool IsHexDigit(char c)
        {
            return (c >= '0' && c <= '9') ||
                   (c >= 'A' && c <= 'F') ||
                   (c >= 'a' && c <= 'f');
        }

        private static int HexDigitToInt(char c)
        {
            if (c >= '0' && c <= '9') return c - '0';
            if (c >= 'A' && c <= 'F') return c - 'A' + 10;
            if (c >= 'a' && c <= 'f') return c - 'a' + 10;
            return 0;
        }
    }
}
