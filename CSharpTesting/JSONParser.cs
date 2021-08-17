using System;

public class JSONParser
{
    private JSONScanner _scanner;

    public JSONParser(JSONScanner scanner)
    {
        _scanner = scanner;
    }

    public JSONParser(JSONSource jsonSource) : this(new JSONScanner(jsonSource))
    {
            
    }

    public JSONParser(string jsonAsText) : this(new JSONScanner(new JSONSource(jsonAsText)))
    {
            
    }

    public JSONObject Parse()
    {
        return ParseJsonObject();
    }

    public JSONObject ParseJsonObject()
    {
        CheckCurrentToken(JSONTokenType.BracesLeft);
        return TryJsonObject();
    }

    private JSONObject TryJsonObject()
    {
        _scanner.Next();
        JSONObject jsonObject = new JSONObject();
        while (!CompareCurrentTokenType(JSONTokenType.BracesRight))
        {
            CheckCurrentToken(JSONTokenType.String);
            var name = _scanner.Get().Value;
            CheckCurrentToken(JSONTokenType.Colon);
            _scanner.Next();
            JSONValue value = TryJsonValue();
            jsonObject.Add(name, value);
            if (CompareCurrentTokenType(JSONTokenType.Comma))
            {
                _scanner.Next();
            }
        }
        _scanner.Next();
        return jsonObject;
    }

    private JSONValue TryJsonValue()
    {
        switch (_scanner.Peek().Type)
        {
            case JSONTokenType.BracesLeft:
                return TryJsonObject();
            case JSONTokenType.Int :
                return TryJsonInt();
            case JSONTokenType.Float:
                return TryJsonFloat();
            case JSONTokenType.String:
                return TryJsonString();
            case JSONTokenType.True:
                return TryJsonTrue();
            case JSONTokenType.False:
                return TryJsonFalse();
            case JSONTokenType.Null:
                return TryJsonNull();
            case JSONTokenType.BracketsLeft:
                return TryJsonArray();
        }
        throw new Exception("Wrong token while trying to parse value.");
    }

    public JSONArray ParseJsonArray()
    {
        CheckCurrentToken(JSONTokenType.BracketsLeft);
        return TryJsonArray();
    }
        
    private JSONArray TryJsonArray()
    {
        _scanner.Next();
        JSONArray jsonArray = new JSONArray();
        if (_scanner.Peek().Type != JSONTokenType.BracketsRight)
        {
            while (IsCurrentForValue)
            {
                jsonArray.Add(TryJsonValue());

                if (CompareCurrentTokenType(JSONTokenType.Comma))
                {
                    _scanner.Next();
                }
            }
                
            if (_scanner.Peek().Type != JSONTokenType.BracketsRight)
            {
                throw new Exception("Error parsing array.");
            }
        }
        _scanner.Next();
        return jsonArray;
    }

    public JSONString ParseJsonString()
    {
        CheckCurrentToken(JSONTokenType.String);
        return TryJsonString();
    }
        
    private JSONString TryJsonString()
    {
        return new JSONString(_scanner.Get().Value);
    }
        
    public JSONInt ParseJsonInt()
    {
        CheckCurrentToken(JSONTokenType.Int);
        return TryJsonInt();
    }

    private JSONInt TryJsonInt()
    {
        return new JSONInt(int.Parse(_scanner.Get().Value));
    }
        
    public JSONFloat ParseJsonFloat()
    {
        CheckCurrentToken(JSONTokenType.Float);
        return TryJsonFloat();
    }

    private JSONFloat TryJsonFloat()
    {
        return new JSONFloat(float.Parse(_scanner.Get().Value));
    }
        
    public JSONBool ParseJsonTrue()
    {
        CheckCurrentToken(JSONTokenType.True);
        return TryJsonTrue();
    }

    private JSONBool TryJsonTrue()
    {
        _scanner.Next();
        return new JSONBool(true);
    }

    public JSONBool ParseJsonFalse()
    {
        CheckCurrentToken(JSONTokenType.False);
        return TryJsonFalse();
    }
        
    private JSONBool TryJsonFalse()
    {
        _scanner.Next();
        return new JSONBool(false);
    }
        
    public JSONNull ParseJsonNull()
    {
        CheckCurrentToken(JSONTokenType.Null);
        return TryJsonNull();
    }
        
    private JSONNull TryJsonNull()
    {
        _scanner.Next();
        return new JSONNull();
    }

    private bool IsCurrentForValue => _scanner.Peek().Type == JSONTokenType.String ||
                                      _scanner.Peek().Type == JSONTokenType.Float ||
                                      _scanner.Peek().Type == JSONTokenType.BracketsLeft ||
                                      _scanner.Peek().Type == JSONTokenType.BracesLeft ||
                                      _scanner.Peek().Type == JSONTokenType.Int ||
                                      _scanner.Peek().Type == JSONTokenType.False ||
                                      _scanner.Peek().Type == JSONTokenType.True ||
                                      _scanner.Peek().Type == JSONTokenType.Null;

    private void CheckCurrentToken(JSONTokenType type)
    {
        if (!CompareCurrentTokenType(type))
        {
            throw new Exception("Wrong token type.");
        }
    }

    private bool CompareCurrentTokenType(JSONTokenType type)
    {
        return _scanner.Peek().Type == type;
    }
}