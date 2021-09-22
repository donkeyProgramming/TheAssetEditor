using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CommonControls.Editors.Sound
{
    public class VisualEventOutputNode
    {
        public string Data { get; set; } = "";
        public List<VisualEventOutputNode> Children { get; set; } = new List<VisualEventOutputNode>();

        public VisualEventOutputNode(string data)
        {
            Data = data;
        }

        public VisualEventOutputNode AddChild(string data)
        {
            var child = new VisualEventOutputNode(data);
            Children.Add(child);
            return child;
        }
    }

    public class VisualEventSerializer
    {
        StringBuilder _builder;
        public string Start(VisualEventOutputNode root)
        {
            _builder = new StringBuilder();
            HandleNode(root, 0);
            return GetStr();
        }

        void HandleNode(VisualEventOutputNode node, int indentation)
        {
            var indentStr = string.Concat(Enumerable.Repeat('\t', indentation));
            _builder.AppendLine(indentStr + node.Data);

            foreach (var item in node.Children)
            {
                HandleNode(item, indentation + 1);
            }
        }

        string GetStr()
        {
            return _builder.ToString();
        }

    }
}
