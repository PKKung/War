using System;
using System.Collections.Generic;

[Serializable]
public class GeminiRequest
{
    public List<Content> contents;
}

[Serializable]
public class Content
{
    public List<Part> parts;
}

[Serializable]
public class Part
{
    public string text;
}

[Serializable]
public class GeminiResponse
{
    public List<Candidate> candidates;
}

[Serializable]
public class Candidate
{
    public Content content;
}