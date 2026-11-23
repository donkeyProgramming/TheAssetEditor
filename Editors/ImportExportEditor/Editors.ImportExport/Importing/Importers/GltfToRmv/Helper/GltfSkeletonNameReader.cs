using System.IO;
using System.Text.Json;

namespace Editors.ImportExport.Importing.Importers.GltfToRmv.Helper;

internal static class GltfSkeletonNameReader
{
    private const uint GlbMagic = 0x46546C67;
    private const uint JsonChunkType = 0x4E4F534A;

    public static string GetDefaultName(string inputFile)
    {
        var fallback = Path.GetFileNameWithoutExtension(inputFile);
        try
        {
            using var document = LoadJsonDocument(inputFile);
            if (!document.RootElement.TryGetProperty("skins", out var skins) ||
                skins.GetArrayLength() == 0)
            {
                return fallback;
            }

            var skin = skins[0];
            if (skin.TryGetProperty("name", out var skinName) &&
                !string.IsNullOrWhiteSpace(skinName.GetString()))
            {
                return skinName.GetString()!;
            }

            if (skin.TryGetProperty("skeleton", out var skeletonIndex) &&
                skeletonIndex.TryGetInt32(out var nodeIndex) &&
                document.RootElement.TryGetProperty("nodes", out var nodes) &&
                nodeIndex >= 0 &&
                nodeIndex < nodes.GetArrayLength() &&
                nodes[nodeIndex].TryGetProperty("name", out var nodeName) &&
                !string.IsNullOrWhiteSpace(nodeName.GetString()))
            {
                return nodeName.GetString()!;
            }

            var ancestorName = GetNamedJointAncestor(document.RootElement, skin);
            if (!string.IsNullOrWhiteSpace(ancestorName))
                return ancestorName;
        }
        catch (Exception)
        {
            return fallback;
        }

        return fallback;
    }

    private static string? GetNamedJointAncestor(
        JsonElement root,
        JsonElement skin)
    {
        if (!root.TryGetProperty("nodes", out var nodes) ||
            !skin.TryGetProperty("joints", out var joints))
        {
            return null;
        }

        var parentIndexes = Enumerable.Repeat(-1, nodes.GetArrayLength()).ToArray();
        for (var parentIndex = 0; parentIndex < nodes.GetArrayLength(); parentIndex++)
        {
            if (!nodes[parentIndex].TryGetProperty("children", out var children))
                continue;

            foreach (var child in children.EnumerateArray())
            {
                if (child.TryGetInt32(out var childIndex) &&
                    childIndex >= 0 &&
                    childIndex < parentIndexes.Length)
                {
                    parentIndexes[childIndex] = parentIndex;
                }
            }
        }

        var jointIndexes = joints.EnumerateArray()
            .Where(joint => joint.TryGetInt32(out _))
            .Select(joint => joint.GetInt32())
            .Where(index => index >= 0 && index < parentIndexes.Length)
            .Distinct()
            .ToList();
        var jointSet = jointIndexes.ToHashSet();
        var rootJoints = jointIndexes
            .Where(index => FindJointParent(index, parentIndexes, jointSet) < 0)
            .ToList();
        if (rootJoints.Count == 0)
            return null;

        var candidate = parentIndexes[rootJoints[0]];
        var visitedCandidates = new HashSet<int>();
        while (candidate >= 0 && visitedCandidates.Add(candidate))
        {
            if (rootJoints.All(rootJoint =>
                    IsAncestor(candidate, rootJoint, parentIndexes)) &&
                nodes[candidate].TryGetProperty("name", out var candidateName) &&
                !string.IsNullOrWhiteSpace(candidateName.GetString()))
            {
                return candidateName.GetString();
            }

            candidate = parentIndexes[candidate];
        }

        return null;
    }

    private static int FindJointParent(
        int jointIndex,
        IReadOnlyList<int> parentIndexes,
        HashSet<int> jointSet)
    {
        var parent = parentIndexes[jointIndex];
        var visited = new HashSet<int>();
        while (parent >= 0 &&
               !jointSet.Contains(parent) &&
               visited.Add(parent))
        {
            parent = parentIndexes[parent];
        }

        return parent >= 0 && jointSet.Contains(parent) ? parent : -1;
    }

    private static bool IsAncestor(
        int candidate,
        int nodeIndex,
        IReadOnlyList<int> parentIndexes)
    {
        var parent = parentIndexes[nodeIndex];
        var visited = new HashSet<int>();
        while (parent >= 0 && visited.Add(parent))
        {
            if (parent == candidate)
                return true;

            parent = parentIndexes[parent];
        }

        return false;
    }

    private static JsonDocument LoadJsonDocument(string inputFile)
    {
        if (Path.GetExtension(inputFile).Equals(".gltf", StringComparison.OrdinalIgnoreCase))
        {
            using var stream = File.OpenRead(inputFile);
            return JsonDocument.Parse(stream);
        }

        using var reader = new BinaryReader(File.OpenRead(inputFile));
        if (reader.ReadUInt32() != GlbMagic || reader.ReadUInt32() != 2)
            throw new InvalidDataException("Not a valid glTF 2.0 GLB file.");

        reader.ReadUInt32();
        var chunkLength = reader.ReadUInt32();
        var chunkType = reader.ReadUInt32();
        if (chunkType != JsonChunkType || chunkLength > int.MaxValue)
            throw new InvalidDataException("The GLB has no valid JSON chunk.");

        var json = reader.ReadBytes((int)chunkLength);
        if (json.Length != chunkLength)
            throw new EndOfStreamException("The GLB JSON chunk is incomplete.");

        return JsonDocument.Parse(json);
    }
}
