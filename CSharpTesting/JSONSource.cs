public class JSONSource
{
    private readonly string _text;
    private int _currentIndex;
    private int _currentChar;

    public const int EOF = -1;

    public JSONSource(string text)
    {
        _text = text;
        _currentIndex = 0;
        _currentChar = _text.Length > 0 ? _text[0] : EOF;
    }


    public int Peek()
    {
        return _currentChar;
    }

    public int Get()
    {
        var tmpChar = _currentChar;
        Next();
        return tmpChar;
    }

    public void Next()
    {
        if (++_currentIndex < _text.Length) {
            _currentChar = _text[_currentIndex];
        } else {
            _currentIndex = _text.Length;
            _currentChar = EOF;
        }
    }
}