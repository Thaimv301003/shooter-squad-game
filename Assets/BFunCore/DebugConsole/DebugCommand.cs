using System;

namespace BFunCoreKit
{
    // Attribute này chỉ có thể được dùng trên các phương thức (methods)
    [AttributeUsage(AttributeTargets.Method)]
    public class DebugCommand : Attribute
    {
        public string CommandId { get; }
        public string Description { get; }

        public DebugCommand(string commandId, string description)
        {
            CommandId = commandId;
            Description = description;
        }
    }
}