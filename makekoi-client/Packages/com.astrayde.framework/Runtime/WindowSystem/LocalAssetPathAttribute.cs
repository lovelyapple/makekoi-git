using System;
using System.Linq;

namespace AstraydeFramework.System
{
    [AttributeUsage(AttributeTargets.Class, Inherited = false)]
    public class LocalAssetPathAttribute : Attribute
    {
        public string Path { get; }

        public LocalAssetPathAttribute(string path)
        {
            Path = path;
        }
    }
    public static class LocalAssetPathAttributeExtension
    {
        public static string GetAssetPath<T>()
        {
            var attr = typeof(T).GetCustomAttributes(typeof(LocalAssetPathAttribute), false)
                                .FirstOrDefault() as LocalAssetPathAttribute;

            if (attr == null)
                throw new Exception($"{typeof(T).Name} に AssetPathAttribute がありません");

            return attr.Path;
        }
    }
}