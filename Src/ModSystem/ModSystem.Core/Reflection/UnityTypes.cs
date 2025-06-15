using System;
using System.Collections.Generic;

namespace ModSystem.Core.Reflection
{
    /// <summary>
    /// Unity类型信息
    /// </summary>
    public class UnityTypeInfo
    {
        public string TypeName { get; set; }
        public string AssemblyName { get; set; }

        public UnityTypeInfo(string typeName, string assemblyName = "UnityEngine")
        {
            TypeName = typeName;
            AssemblyName = assemblyName;
        }

        public string FullName => $"{TypeName}, {AssemblyName}";
    }

    /// <summary>
    /// 常用Unity类型
    /// </summary>
    public static class UnityTypes
    {
        public static readonly UnityTypeInfo GameObject = new UnityTypeInfo("UnityEngine.GameObject");
        public static readonly UnityTypeInfo Transform = new UnityTypeInfo("UnityEngine.Transform");
        public static readonly UnityTypeInfo Rigidbody = new UnityTypeInfo("UnityEngine.Rigidbody");
        public static readonly UnityTypeInfo BoxCollider = new UnityTypeInfo("UnityEngine.BoxCollider");
        public static readonly UnityTypeInfo MeshRenderer = new UnityTypeInfo("UnityEngine.MeshRenderer");
        public static readonly UnityTypeInfo MeshFilter = new UnityTypeInfo("UnityEngine.MeshFilter");
        public static readonly UnityTypeInfo Light = new UnityTypeInfo("UnityEngine.Light");
        public static readonly UnityTypeInfo Camera = new UnityTypeInfo("UnityEngine.Camera");
        public static readonly UnityTypeInfo AudioSource = new UnityTypeInfo("UnityEngine.AudioSource");

        // UI类型
        public static readonly UnityTypeInfo Canvas = new UnityTypeInfo("UnityEngine.Canvas", "UnityEngine.UI");
        public static readonly UnityTypeInfo Text = new UnityTypeInfo("UnityEngine.UI.Text", "UnityEngine.UI");
        public static readonly UnityTypeInfo Button = new UnityTypeInfo("UnityEngine.UI.Button", "UnityEngine.UI");
        public static readonly UnityTypeInfo Image = new UnityTypeInfo("UnityEngine.UI.Image", "UnityEngine.UI");
    }
}