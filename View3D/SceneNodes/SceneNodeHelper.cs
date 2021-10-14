using System;
using System.Collections.Generic;
using System.Text;

namespace View3D.SceneNodes
{
    public static class SceneNodeHelper
    {
        public static List<T> GetChildrenOfType<T>(ISceneNode root, Func<ISceneNode, bool> filterFunction = null) where T : ISceneNode
        {
            var output = new List<T>();
            foreach (var child in root.Children)
            {
                if (filterFunction != null)
                {
                    var filterResult = filterFunction(child);
                    if (filterResult == false)
                        continue;
                }

                var res = GetChildrenOfType<T>(child, filterFunction);
                output.AddRange(res);
                if (child is T)
                    output.Add((T)child);
            }
            return output;
        }

        public static T CloneNode<T>(T target) where T : ISceneNode
        {
            var clone = (T)target.CreateCopyInstance();
            target.CopyInto(clone);
            return clone;
        }


        public static T DeepCopy<T>(T target) where T : ISceneNode
        {
            var clone = (T)target.CreateCopyInstance();
            target.CopyInto(clone);

            foreach (var child in target.Children)
            {
                var childClone = DeepCopy(child);
                clone.Children.Add(childClone);
            }
            return clone;
        }
    }
}
