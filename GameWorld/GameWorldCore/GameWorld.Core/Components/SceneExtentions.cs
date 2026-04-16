using System;
using System.Collections.Generic;

namespace GameWorld.Core.Components
{
    public static class SceneExtentions
    {
        public static IEnumerable<T> Search<T>(this T node, Func<T, IEnumerable<T>> childs, Func<T, bool> condition, GraphTraversal mode = GraphTraversal.DepthFirst)
        {
            if (node == null || childs == null || condition == null)
                throw new ArgumentNullException();
            if (mode == GraphTraversal.DepthFirst)
                return node.depthFirstTraversal(childs, condition);
            else if (mode == GraphTraversal.DepthFirstNoStackOverflow)
                return node.depthFirstTraversalWithoutStackoverflow(childs, condition);
            else
                return node.breadthFirstTraversal(childs, condition);
        }

        private static IEnumerable<T> depthFirstTraversal<T>(this T node, Func<T, IEnumerable<T>> childs, Func<T, bool> condition)
        {
            var childrens = childs(node);
            if (childrens == null)
                yield break;
            if (condition(node))
                yield return node;
            foreach (var i in childrens)
            {
                foreach (var j in i.depthFirstTraversal(childs, condition))
                {
                    if (condition(j))
                        yield return j;
                }
            }
        }

        private static IEnumerable<T> breadthFirstTraversal<T>(this T node, Func<T, IEnumerable<T>> childs, Func<T, bool> condition)
        {
            var queue = new Queue<T>();
            queue.Enqueue(node);
            while (queue.Count > 0)
            {
                var currentnode = queue.Dequeue();
                if (condition(currentnode))
                    yield return currentnode;
                var childrens = childs(currentnode);
                if (childrens != null)
                {
                    foreach (var child in childrens)
                        queue.Enqueue(child);
                }
            }
        }

        private static IEnumerable<T> depthFirstTraversalWithoutStackoverflow<T>(this T node, Func<T, IEnumerable<T>> childs, Func<T, bool> condition)
        {
            var stack = new Stack<T>();
            stack.Push(node);
            while (stack.Count > 0)
            {
                var currentnode = stack.Pop();
                if (condition(currentnode))
                    yield return currentnode;
                var childrens = childs(currentnode);
                if (childrens != null)
                {
                    foreach (var child in childrens)
                        stack.Push(child);
                }
            }
        }

        public enum GraphTraversal { DepthFirst, DepthFirstNoStackOverflow, BreadthFirst }
    }


}
