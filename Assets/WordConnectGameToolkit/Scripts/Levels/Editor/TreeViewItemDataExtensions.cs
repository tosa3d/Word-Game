using System.Collections.Generic;
using System.Linq;
using UnityEngine.UIElements;

namespace WordsToolkit.Scripts.Levels.Editor
{
    public static class TreeViewItemDataExtensions
    {
        public static TreeViewItemData<T> UpdateChildren<T>(this TreeViewItemData<T> item, List<TreeViewItemData<T>> newChildren)
        {
            return new TreeViewItemData<T>(item.id, item.data, newChildren);
        }
    }
}
