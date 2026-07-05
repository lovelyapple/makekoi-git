using System.Collections.Generic;
using System.IO;
using System.Text;

public static class EnumFileGenerator
{
    public static void GenerateEnumFile(
        List<string> nameList,
        string enumName,
        string outputPath,
        string nameSpace = null)
    {
        var sb = new StringBuilder();

        if (!string.IsNullOrEmpty(nameSpace))
        {
            sb.AppendLine($"namespace {nameSpace}");
            sb.AppendLine("{");
        }

        sb.AppendLine($"public enum {enumName}");
        sb.AppendLine("{");

        for (int i = 0; i < nameList.Count; i++)
        {
            var value = KeyId.ToId(nameList[i]);
            var name = SanitizeEnumName(nameList[i]);
            sb.AppendLine($"    {name} = {value},");
        }

        sb.AppendLine("}");

        if (!string.IsNullOrEmpty(nameSpace))
        {
            sb.AppendLine("}");
        }

        File.WriteAllText(outputPath, sb.ToString(), Encoding.UTF8);
    }

    private static string SanitizeEnumName(string raw)
    {
        var name = raw.Replace(" ", "_").Replace("-", "_");
        if (char.IsDigit(name[0]))
        {
            name = "_" + name;
        }
        return name;
    }
}

public static class KeyId
{
    public static int ToId(string key)
    {
        var normalized = Normalize(key);
        return Fnv1a32("DataEnum:" + normalized);
    }

    static string Normalize(string s)
        => s.Trim().ToUpperInvariant().Replace("-", "_").Replace(" ", "");

    static int Fnv1a32(string s)
    {
        unchecked
        {
            const uint offset = 2166136261;
            const uint prime = 16777619;

            uint hash = offset;
            for (int i = 0; i < s.Length; i++)
            {
                hash ^= s[i];
                hash *= prime;
            }

            return (int)hash & 0x7fffffff;
        }
    }
}
