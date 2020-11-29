namespace ConsoleEngine.Models
{
    public class CharacterInfo
    {
        public char Glyph { get; set; } = ' ';
        public AnsiColor? FgColor { get; set; }
        public AnsiAttributes? Attributes { get; set; }
    }
}