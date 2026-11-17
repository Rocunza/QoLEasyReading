// PORT17-V110: inspect PE metadata only. Never loads or executes the mod DLL.
using System.Reflection.Metadata;
using System.Reflection.PortableExecutable;
using System.Text;

static class Program
{
    private static int Main(string[] args)
    {
        try
        {
            if (args.Length != 4 || !int.TryParse(args[1], out var expectedActions))
                throw new ArgumentException("Usage: DevActionVerifier <dll> <expectedDebugActions> <expectedVersion> <buildId>");
            string dll = Path.GetFullPath(args[0]);
            string expectedVersion = args[2], expectedBuildId = args[3];
            using var stream = File.OpenRead(dll);
            using var pe = new PEReader(stream, PEStreamOptions.LeaveOpen);
            if (!pe.HasMetadata) throw new InvalidDataException("DLL does not have CLR metadata");
            MetadataReader md = pe.GetMetadataReader();
            if (!md.IsAssembly) throw new InvalidDataException("Not an assembly");
            var assembly = md.GetAssemblyDefinition();
            if (assembly.Version != new Version(expectedVersion + ".0"))
                throw new InvalidDataException($"AssemblyVersion mismatch: {assembly.Version}");

            var observed = new List<string>();
            foreach (var typeHandle in md.TypeDefinitions)
            {
                var type = md.GetTypeDefinition(typeHandle);
                string typeName = md.GetString(type.Namespace) + "." + md.GetString(type.Name);
                foreach (var methodHandle in type.GetMethods())
                {
                    var method = md.GetMethodDefinition(methodHandle);
                    foreach (var attributeHandle in method.GetCustomAttributes())
                    {
                        var attr = md.GetCustomAttribute(attributeHandle);
                        if (ConstructorType(md, attr.Constructor) == "Verse.DebugActionAttribute")
                            observed.Add(typeName + "::" + md.GetString(method.Name));
                    }
                }
            }
            if (observed.Count != expectedActions)
                throw new InvalidDataException($"DebugActionAttribute expected={expectedActions}; actual={observed.Count}; methods={string.Join(",", observed)}");

            string? informationalVersion = null;
            foreach (var h in assembly.GetCustomAttributes())
            {
                var attr = md.GetCustomAttribute(h);
                if (ConstructorType(md, attr.Constructor) != "System.Reflection.AssemblyInformationalVersionAttribute")
                    continue;
                var reader = md.GetBlobReader(attr.Value);
                if (reader.ReadUInt16() != 1) throw new InvalidDataException("Malformed informational version attribute");
                informationalVersion = reader.ReadSerializedString();
            }
            if (informationalVersion != expectedVersion)
                throw new InvalidDataException($"InformationalVersion mismatch: {informationalVersion ?? "(missing)"}");

            // C# ldstr constants are stored as UTF-16 user-string entries.
            byte[] data = File.ReadAllBytes(dll);
            if (data.AsSpan().IndexOf(Encoding.Unicode.GetBytes(expectedBuildId)) < 0)
                throw new InvalidDataException("BUILD_ID absent from binary (stale or incorrect DLL)");

            Console.WriteLine($"[PORT17-V110][METADATA] status=PASS; file={Path.GetFileName(dll)}; AssemblyVersion={assembly.Version}; InformationalVersion={informationalVersion}; DebugActions={observed.Count}; expected={expectedActions}; BuildId={expectedBuildId}; execution=NONE");
            return 0;
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine("[PORT17-V110][METADATA] status=FAIL; " + ex.Message);
            return 1;
        }
    }

    private static string ConstructorType(MetadataReader md, EntityHandle ctor)
    {
        EntityHandle declaring;
        if (ctor.Kind == HandleKind.MemberReference)
        {
            var member = md.GetMemberReference((MemberReferenceHandle)ctor);
            declaring = member.Parent;
        }
        else if (ctor.Kind == HandleKind.MethodDefinition)
        {
            declaring = md.GetMethodDefinition((MethodDefinitionHandle)ctor).GetDeclaringType();
        }
        else return "";

        if (declaring.Kind == HandleKind.TypeReference)
        {
            var type = md.GetTypeReference((TypeReferenceHandle)declaring);
            return md.GetString(type.Namespace) + "." + md.GetString(type.Name);
        }
        if (declaring.Kind == HandleKind.TypeDefinition)
        {
            var type = md.GetTypeDefinition((TypeDefinitionHandle)declaring);
            return md.GetString(type.Namespace) + "." + md.GetString(type.Name);
        }
        return "";
    }
}
