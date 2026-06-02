using UnityEngine;
using System;

namespace BFunCoreKit
{
    [AttributeUsage(AttributeTargets.Field)]
    public class LocalizeAttribute : Attribute
    {
        // Chỉ là một marker, không cần logic gì cả
    }
}