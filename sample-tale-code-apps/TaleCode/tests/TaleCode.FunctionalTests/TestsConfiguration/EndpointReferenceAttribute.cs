using System;

namespace TaleCode.FunctionalTests.TestsConfiguration;

[AttributeUsage(AttributeTargets.Method)]
public class EndpointReferenceAttribute : Attribute
{
    public string Controller { get; }
    public string Action { get; }

    public EndpointReferenceAttribute(string controller, string action)
    {
        Controller = controller;
        Action = action;
    }
}
