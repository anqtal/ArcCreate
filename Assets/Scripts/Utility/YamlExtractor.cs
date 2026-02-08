using System.Collections.Generic;
using System.IO;
using YamlDotNet.RepresentationModel;

namespace ArcCreate.Utility
{
    public static class YamlExtractor
    {
        public static void ExtractTo(Dictionary<string, string> dict, TextReader input)
        {
            var yaml = new YamlStream();
            yaml.Load(input);
            var mapping = (YamlMappingNode)yaml.Documents[0].RootNode;

            ExtractTo(dict, mapping, "");
        }

        public static void ExtractListsTo(Dictionary<string, List<string>> dict, TextReader input)
        {
            var yaml = new YamlStream();
            yaml.Load(input);
            var mapping = (YamlMappingNode)yaml.Documents[0].RootNode;

            ExtractListsTo(dict, mapping, "");
        }

        public static void ExtractTo(Dictionary<string, string> dict, YamlMappingNode node, string key)
        {
            foreach (var child in node.Children)
            {
                var nodeKey = (child.Key as YamlScalarNode).Value;
                var newKey = string.IsNullOrEmpty(key) ? nodeKey : $"{key}.{nodeKey}";

                var value = child.Value;
                if (value is YamlScalarNode scalar)
                {
                    var leaf = scalar.Value;
                    dict.Add(newKey, leaf);
                }
                else
                {
                    ExtractTo(dict, value as YamlMappingNode, newKey);
                }
            }
        }

        public static void ExtractListsTo(Dictionary<string, List<string>> dict, YamlMappingNode node, string key)
        {
            foreach (var child in node.Children)
            {
                var nodeKey = (child.Key as YamlScalarNode).Value;
                var newKey = string.IsNullOrEmpty(key) ? nodeKey : $"{key}.{nodeKey}";

                var value = child.Value;
                if (value is YamlSequenceNode sequence)
                {
                    var list = new List<string>();
                    foreach (var children in sequence)
                        if (children is YamlScalarNode scalar)
                            list.Add(scalar.Value);

                    dict.Add(newKey, list);
                }
                else if (value is YamlScalarNode scalar)
                {
                    var list = new List<string> { scalar.Value };
                    dict.Add(newKey, list);
                }
                else if (value is YamlMappingNode map)
                {
                    ExtractListsTo(dict, map, newKey);
                }
            }
        }
    }
}