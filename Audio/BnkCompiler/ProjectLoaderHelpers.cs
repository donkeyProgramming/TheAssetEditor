using Audio.BnkCompiler.ObjectConfiguration.Warhammer3;
using Shared.GameFormats.WWise.Hirc.Shared;
using System;

namespace Audio.BnkCompiler
{
    public class ProjectLoaderHelpers
    {
        private static readonly IVanillaObjectIds s_vanillaObjectIds = new IdProvider();

        public static int GenerateRandomNumber()
        {
            var rand = new Random();
            var min = 10000000;
            var max = 99999999;

            return rand.Next(min, max + 1);
        }

        public static int CountNodeDescendants(AkDecisionTree.Node node)
        {
            var count = node.Children.Count;

            foreach (var child in node.Children)
                count += CountNodeDescendants(child);

            return count;
        }

        public static void PrintNode(AkDecisionTree.Node node, int depth)
        {
            if (node == null)
                return;

            var indentation = new string(' ', depth * 4);
            Console.WriteLine($"{indentation}Key: {node.Key}");
            Console.WriteLine($"{indentation}AudioNodeId: {node.AudioNodeId}");
            Console.WriteLine($"{indentation}Children_uIdx: {node.Children_uIdx}");
            Console.WriteLine($"{indentation}Children_uCount: {node.Children_uCount}");
            Console.WriteLine($"{indentation}uWeight: {node.uWeight}");
            Console.WriteLine($"{indentation}uProbability: {node.uProbability}");

            foreach (var childNode in node.Children)
                PrintNode(childNode, depth + 1);
        }

        public static uint GetAttenuationId(string eventMixer)
        {
            var attenuationKey = $"{eventMixer}_attenuation";

            if (s_vanillaObjectIds.AttenuationIds.ContainsKey(attenuationKey))
                return s_vanillaObjectIds.AttenuationIds[attenuationKey];

            else
                return 0;
        }
    }
}
