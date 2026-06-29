using System;

namespace StrmAssistant.Options
{
    [AttributeUsage(AttributeTargets.Property, AllowMultiple = false)]
    public class IgnoreOnPersistAttribute : Attribute
    {
    }
}
