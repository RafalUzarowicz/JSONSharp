using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

public class JSONScanner
{
    private readonly JSONSource _source;
    private JSONToken _token;
    private StringBuilder _stringBuilder;
    public JSONScanner(JSONSource source)
    {
        _stringBuilder = new StringBuilder();
        _source = source;
        Next();
        _token ??= new JSONToken(JSONTokenType.EOF);
    }
    
    public JSONToken Get()
    {
        JSONToken token = _token;
        Next();
        return token;
    }

    public JSONToken Peek()
    {
        return _token;
    }

    public void Next()
    {
        if (TryEof())
        {
            return;
        }
        
        IgnoreWhiteSpaces();

        if (TryKeyWords())
        {
            return;
        }
        if (TrySingleCharacter())
        {
            return;
        } 
        if (TryString())
        {
            return;
        } 
        if (TryNumber())
        {
            return;
        }

        throw new Exception("Unknown symbol from source: " + _source.Peek());
    }
    
    private void IgnoreWhiteSpaces(){
        while (char.IsWhiteSpace(Convert.ToChar(_source.Peek())))
        {
            _source.Next();
        }
    }

    private bool TryEof()
    {
        if (_source.Peek() == JSONSource.EOF)
        {
            _token = new JSONToken(JSONTokenType.EOF);
            return true;
        }
        return false;
    }

    private static Dictionary<string, JSONTokenType> JSON_KEYWORDS = new ()
    {
        ["true"] = JSONTokenType.True, 
        ["false"] = JSONTokenType.False, 
        ["null"] = JSONTokenType.Null
    };
    
    private bool TryKeyWords()
    {
        string keyword = JSON_KEYWORDS.Keys.FirstOrDefault(x => x.First() == _source.Peek());
        if (!string.IsNullOrEmpty(keyword))
        {
            for (int i = 0; i < keyword.Length - 1; ++i)
            {
                if (IsNextEOF || _source.Get() != keyword[i])
                {
                    throw new Exception("Error writing \"true\" keyword.");
                }
            }

            if (_source.Get() == keyword.Last())
            {
                _token = new JSONToken(keyword, JSON_KEYWORDS[keyword]);
                return true;
            }
            
            throw new Exception("Wrong keyword.");
        }
        return false;
    }
    
    private bool TrySingleCharacter()
    {
        if (JSONToken.JSON_SINGLE_CHARS.Keys.Contains(_source.Peek()))
        {
            _token = new JSONToken(JSONToken.JSON_SINGLE_CHARS[_source.Get()]);
            return true;
        }
        return false;
    }

    private static Dictionary<string, char> JSON_ESCAPE_KEYWORDS = new()
    {
        ["\\\\"] = '\\',
        ["\\n"] = '\n',
        ["\\r"] = '\r',
        ["\\t"] = '\t',
        ["\\\""] = '\"',
        ["\\f"] = '\f',
        ["\\b"] = '\b',
    };

    private bool TryString()
    {
        if (_source.Peek() == '\"')
        {
            _source.Next();
            _token = new JSONToken(JSONTokenType.String);
            _stringBuilder.Clear();
            int prevChar = -1;
            int currChar = _source.Peek();
            
            while (currChar != '\"')
            {
                if (IsNextEOF)
                {
                    throw new Exception("End of source while creating string Token.");
                }
                prevChar = _source.Get();
                currChar = _source.Peek();

                var doubleChar =  "" + Convert.ToChar(prevChar) + Convert.ToChar(currChar);

                if (JSON_ESCAPE_KEYWORDS.ContainsKey(doubleChar))
                {
                    _stringBuilder.Append(JSON_ESCAPE_KEYWORDS[doubleChar]);
                    _source.Next();
                    currChar = _source.Peek();
                }
                else
                {
                    _stringBuilder.Append(Convert.ToChar(prevChar));
                }
                
            }
            
            _token.Value = _stringBuilder.ToString();
            _source.Next();
            return true;
        }
        return false;
    }

    private bool TryNumber()
    {
        if (_source.Peek() == '-' || IsNextDigit)
        {
            _stringBuilder.Clear();
            _stringBuilder.Append(Convert.ToChar(_source.Get()));
            if(_stringBuilder[^1] == '-' && !IsNextDigit)
            {
                throw new Exception("Wrong number format.");
            }

            while (IsNextDigit && !IsNextExponentCharacter && !IsNextFractionCharacter)
            {
                _stringBuilder.Append(Convert.ToChar(_source.Get()));
            }

            if (IsNextFractionCharacter)
            {
                _stringBuilder.Append(Convert.ToChar(_source.Get()));
                while (IsNextDigit && !IsNextExponentCharacter)
                {
                    _stringBuilder.Append(Convert.ToChar(_source.Get()));
                }
            }
            else if(!IsNextExponentCharacter)
            {
                _token = new JSONToken(_stringBuilder.ToString(), JSONTokenType.Int);
                return true;
            }

            if (IsNextExponentCharacter)
            {
                _stringBuilder.Append(Convert.ToChar(_source.Get()));
                if (IsNextDigit || IsNextSign)
                {
                    _stringBuilder.Append(Convert.ToChar(_source.Get()));
                }

                while (IsNextDigit)
                {
                    _stringBuilder.Append(Convert.ToChar(_source.Get()));
                }
            }
            _token = new JSONToken(_stringBuilder.ToString(), JSONTokenType.Float);
            return true;
        }
        return false;
    }

    private bool IsNextEOF => _source.Peek() == JSONSource.EOF;
    private bool IsNextDigit => _source.Peek() >= '0' && _source.Peek() <= '9';
    private bool IsNextFractionCharacter => _source.Peek() == '.';
    private bool IsNextExponentCharacter => _source.Peek() == 'E' || _source.Peek() == 'e';
    private bool IsNextSign => _source.Peek() == '-' || _source.Peek() == '+';
}