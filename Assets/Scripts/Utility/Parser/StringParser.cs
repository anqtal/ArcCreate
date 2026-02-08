namespace ArcCreate.Utility.Parser
{
    public class StringParser
    {
        private readonly string str;

        public StringParser(string str)
        {
            this.str = str;
        }

        public char Current => str[Pos];

        public int Pos { get; private set; }

        public bool HasEnded => Pos >= str.Length;

        public void Skip(int length)
        {
            Pos += length;
        }

        public Result<TextSpan<float>, ParsingError> ReadFloat(string terminator = null)
        {
            if (!ReadString(terminator).TryUnwrap(out var s, out var e)) return e;

            if (!Evaluator.TryFloat(s, out var value))
                return new ParsingError(s, s.StartPos, s.Length, ParsingError.Kind.InvalidConversionToFloat);

            return new TextSpan<float>(value, s.StartPos, s.Length);
        }

        public Result<TextSpan<int>, ParsingError> ReadInt(string terminator = null)
        {
            if (!ReadString(terminator).TryUnwrap(out var s, out var e)) return e;

            if (!Evaluator.TryInt(s, out var value))
                return new ParsingError(s, s.StartPos, s.Length, ParsingError.Kind.InvalidConversionToInt);

            return new TextSpan<int>(value, s.StartPos, s.Length);
        }

        public Result<TextSpan<bool>, ParsingError> ReadBool(string terminator = null)
        {
            if (!ReadString(terminator).TryUnwrap(out var s, out var e)) return e;

            if (!bool.TryParse(s, out var value))
                return new ParsingError(s, s.StartPos, s.Length, ParsingError.Kind.InvalidConversionToBool);

            return new TextSpan<bool>(value, s.StartPos, s.Length);
        }

        public Result<TextSpan<string>, ParsingError> ReadString(string terminator = null)
        {
            var end = terminator != null ? str.IndexOf(terminator, Pos) : str.Length;
            if (end == -1)
                return new ParsingError(terminator, Pos, str.Length - Pos, ParsingError.Kind.CharacterNotFound);

            var s = str.Substring(Pos, end - Pos);
            var result = new TextSpan<string>(s, Pos, s.Length);
            Pos = end + 1;
            return result;
        }
    }
}