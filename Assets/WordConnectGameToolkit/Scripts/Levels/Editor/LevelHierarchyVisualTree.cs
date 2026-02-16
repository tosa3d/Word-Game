using UnityEngine;
using UnityEngine.UIElements;
using UnityEditor;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using WordsToolkit.Scripts.NLP;

namespace WordsToolkit.Scripts.Levels.Editor
{
    public class LevelHierarchyVisualTree : VisualElement
    {
        private IModelController ModelController => EditorScope.Resolve<IModelController>();

        private Dictionary<VisualElement, EventCallback<ClickEvent>> addButtonCallbacks = new Dictionary<VisualElement, EventCallback<ClickEvent>>();
        private Dictionary<VisualElement, EventCallback<ClickEvent>> deleteButtonCallbacks = new Dictionary<VisualElement, EventCallback<ClickEvent>>();

        // Events
        public event Action<LevelHierarchyItem> OnSelectionChanged;
        public event Action<LevelHierarchyItem> OnDeleteItem;
        public event Action<LevelHierarchyItem> OnCreateSubgroup;
        public event Action<LevelHierarchyItem> OnCreateLevel;
        public event Action OnHierarchyChanged;

        // UI Elements
        private TreeView treeView;
        private Dictionary<int, LevelHierarchyItem> idToItem = new Dictionary<int, LevelHierarchyItem>();
        private List<TreeViewItemData<LevelHierarchyItem>> rootItems = new List<TreeViewItemData<LevelHierarchyItem>>();
        private LevelHierarchyItem selectedItem;

        public LevelHierarchyVisualTree()
        {
            Init();
        }

        private void Init()
        {
            style.flexGrow = 1;

            // Create TreeView
            treeView = new TreeView();
            treeView.style.flexGrow = 1;
            treeView.selectionType = SelectionType.Single;
            // Configure data callbacks
            treeView.makeItem = MakeTreeItem;
            treeView.bindItem = (element, index) =>
            {
                var itemData = treeView.GetItemDataForIndex<TreeViewItemData<LevelHierarchyItem>>(index);
                BindTreeItem(element, itemData);
            };
            treeView.unbindItem = UnbindTreeItem;
            
            // Set up TreeView events
            treeView.selectionChanged += OnTreeSelectionChanged;
            treeView.RegisterCallback<KeyDownEvent>(OnKeyDown);
            treeView.RegisterCallback<ContextClickEvent>(OnContextClick);

            // Enable drag and drop
            treeView.reorderable = true;
            treeView.RegisterCallback<DragUpdatedEvent>(OnDragUpdated);
            treeView.RegisterCallback<DragPerformEvent>(OnDragPerform);

            Add(treeView);
            Reload();
        }

        private VisualElement MakeTreeItem()
        {
            var itemContainer = new VisualElement();
            itemContainer.style.flexDirection = FlexDirection.Row;
            itemContainer.style.alignItems = Align.Center;

            var label = new Label();
            label.style.flexGrow = 1;
            itemContainer.Add(label);

            // Buttons container for group items
            var buttonsContainer = new VisualElement();
            buttonsContainer.style.flexDirection = FlexDirection.Row;
            buttonsContainer.style.display = DisplayStyle.None;

            var addButton = new Button(() => { }) { text = "+" };
            addButton.AddToClassList("unity-button");
            addButton.style.width = 20;
            addButton.style.marginRight = 2;

            var deleteButton = new Button(() => { }) { text = "−" };
            deleteButton.AddToClassList("unity-button");
            deleteButton.style.width = 20;

            buttonsContainer.Add(addButton);
            buttonsContainer.Add(deleteButton);
            itemContainer.Add(buttonsContainer);

            return itemContainer;
        }

        private void BindTreeItem(VisualElement element, TreeViewItemData<LevelHierarchyItem> itemData)
        {
            var item = itemData.data;
            var label = element.Q<Label>();
            var buttonsContainer = element.Children().Last();
            var addButton = buttonsContainer.Q<Button>(null, "unity-button");
            var deleteButton = buttonsContainer.Children().Last() as Button;

            label.text = GetItemDisplayName(item);
            label.style.unityFontStyleAndWeight = item.type == LevelHierarchyItem.ItemType.Group ? 
                FontStyle.Bold : FontStyle.Normal;

            // Show/hide buttons based on item type
            buttonsContainer.style.display = item.type == LevelHierarchyItem.ItemType.Group ? 
                DisplayStyle.Flex : DisplayStyle.None;

            if (item.type == LevelHierarchyItem.ItemType.Group)
            {
                // Create callbacks and store them for cleanup
                var addCallback = new EventCallback<ClickEvent>((evt) => OnCreateSubgroup?.Invoke(item));
                var deleteCallback = new EventCallback<ClickEvent>((evt) =>
                {
                    if (EditorUtility.DisplayDialog(
                        "Delete Group",
                        "Are you sure you want to delete this group?",
                        "Delete",
                        "Cancel"))
                    {
                        OnDeleteItem?.Invoke(item);
                    }
                });

                addButton.RegisterCallback(addCallback);
                deleteButton.RegisterCallback(deleteCallback);

                addButtonCallbacks[element] = addCallback;
                deleteButtonCallbacks[element] = deleteCallback;
            }

            // Set icon
            if (item.icon != null)
            {
                var iconElement = element.Q<Image>("icon");
                if (iconElement == null)
                {
                    iconElement = new Image { name = "icon" };
                    element.Insert(0, iconElement);
                }
                iconElement.image = item.icon;
            }
        }

        private void UnbindTreeItem(VisualElement element, int index)
        {
            var buttonsContainer = element.Children().LastOrDefault();
            if (buttonsContainer == null) return;

            var addButton = buttonsContainer.Q<Button>(null, "unity-button");
            var deleteButton = buttonsContainer.Children().LastOrDefault() as Button;

            // Remove click event handlers
            if (addButtonCallbacks.TryGetValue(element, out var addCallback))
            {
                if (addButton != null)
                {
                    addButton.UnregisterCallback(addCallback);
                }
                addButtonCallbacks.Remove(element);
            }

            if (deleteButtonCallbacks.TryGetValue(element, out var deleteCallback))
            {
                if (deleteButton != null)
                {
                    deleteButton.UnregisterCallback(deleteCallback);
                }
                deleteButtonCallbacks.Remove(element);
            }

            // Clear references
            if (element.Q<Image>("icon") is Image iconElement)
            {
                iconElement.image = null;
                element.Remove(iconElement);
            }

            // Clear any text content
            if (element.Q<Label>() is Label label)
            {
                label.text = string.Empty;
            }

            // Reset button container visibility
            buttonsContainer.style.display = DisplayStyle.None;
        }

        private string GetItemDisplayName(LevelHierarchyItem item)
        {
            switch (item.type)
            {
                case LevelHierarchyItem.ItemType.Collection:
                    return Path.GetFileName(item.folderPath);
                case LevelHierarchyItem.ItemType.Group:
                    return item.groupAsset?.name ?? "Missing Group";
                case LevelHierarchyItem.ItemType.Level:
                    return $"Level {item.levelAsset?.number ?? 0}";
                default:
                    return "Unknown Item";
            }
        }

        private void OnTreeSelectionChanged(IEnumerable<object> items)
        {
            selectedItem = items.FirstOrDefault() as LevelHierarchyItem;
            OnSelectionChanged?.Invoke(selectedItem);
        }

        private void OnKeyDown(KeyDownEvent evt)
        {
            if (evt.keyCode == KeyCode.Delete || evt.keyCode == KeyCode.Backspace)
            {
                if (selectedItem != null)
                {
                    if (EditorUtility.DisplayDialog(
                        "Delete Item",
                        $"Are you sure you want to delete {GetItemDisplayName(selectedItem)}?",
                        "Delete",
                        "Cancel"))
                    {
                        OnDeleteItem?.Invoke(selectedItem);
                    }
                }
                evt.StopPropagation();
            }
        }

        private void OnContextClick(ContextClickEvent evt)
        {
            var menu = new GenericMenu();

            if (selectedItem != null)
            {
                switch (selectedItem.type)
                {
                    case LevelHierarchyItem.ItemType.Group:
                        menu.AddItem(new GUIContent("Create Subgroup"), false, () => OnCreateSubgroup?.Invoke(selectedItem));
                        menu.AddItem(new GUIContent("Create Level"), false, () => OnCreateLevel?.Invoke(selectedItem));
                        menu.AddSeparator("");
                        menu.AddItem(new GUIContent("Delete Group"), false, () => OnDeleteItem?.Invoke(selectedItem));
                        break;

                    case LevelHierarchyItem.ItemType.Level:
                        menu.AddItem(new GUIContent("Delete Level"), false, () => OnDeleteItem?.Invoke(selectedItem));
                        break;
                }
            }
            else
            {
                menu.AddItem(new GUIContent("Create Root Group"), false, () => CreateRootGroup());
            }

            menu.ShowAsContext();
            evt.StopPropagation();
        }

        private void CreateRootGroup()
        {
            var newGroup = ScriptableObject.CreateInstance<LevelGroup>();
            var path = EditorUtility.SaveFilePanelInProject(
                "Create New Group",
                "NewGroup",
                "asset",
                "Choose location for the new group"
            );

            if (!string.IsNullOrEmpty(path))
            {
                AssetDatabase.CreateAsset(newGroup, path);
                AssetDatabase.SaveAssets();
                Reload();
            }
        }

        private void OnDragUpdated(DragUpdatedEvent evt)
        {
            var draggedItem = DragAndDrop.GetGenericData("DraggedItem") as LevelHierarchyItem;
            if (draggedItem != null)
            {
                DragAndDrop.visualMode = DragAndDropVisualMode.Move;
                evt.StopPropagation();
            }
        }

        private void OnDragPerform(DragPerformEvent evt)
        {
            var draggedItem = DragAndDrop.GetGenericData("DraggedItem") as LevelHierarchyItem;
            var dropTarget = selectedItem;

            if (draggedItem != null && dropTarget != null && dropTarget.type == LevelHierarchyItem.ItemType.Group)
            {
                if (!WouldCreateCircularReference(draggedItem, dropTarget))
                {
                    UpdateItemParent(draggedItem, dropTarget);
                    OnHierarchyChanged?.Invoke();
                    Reload();
                }
            }

            evt.StopPropagation();
        }

        private bool WouldCreateCircularReference(LevelHierarchyItem draggedItem, LevelHierarchyItem newParent)
        {
            if (draggedItem.type != LevelHierarchyItem.ItemType.Group || newParent == null)
                return false;

            var current = newParent;
            while (current != null)
            {
                if (current == draggedItem)
                    return true;
                current = GetParentItem(current);
            }
            return false;
        }

        private LevelHierarchyItem GetParentItem(LevelHierarchyItem item)
        {
            if (item?.type != LevelHierarchyItem.ItemType.Group || item.groupAsset?.parentGroup == null)
                return null;

            return idToItem.Values.FirstOrDefault(i => 
                i.type == LevelHierarchyItem.ItemType.Group && 
                i.groupAsset == item.groupAsset.parentGroup);
        }

        private void UpdateItemParent(LevelHierarchyItem item, LevelHierarchyItem newParent)
        {
            switch (item.type)
            {
                case LevelHierarchyItem.ItemType.Group:
                    UpdateGroupParent(item, newParent);
                    break;
                case LevelHierarchyItem.ItemType.Level:
                    UpdateLevelParent(item, newParent);
                    break;
            }
        }

        private void UpdateGroupParent(LevelHierarchyItem groupItem, LevelHierarchyItem newParent)
        {
            if (groupItem.groupAsset == null) return;

            var oldParent = groupItem.groupAsset.parentGroup;
            groupItem.groupAsset.parentGroup = newParent?.groupAsset;
            EditorUtility.SetDirty(groupItem.groupAsset);

            if (oldParent != null)
                EditorUtility.SetDirty(oldParent);
            if (newParent?.groupAsset != null)
                EditorUtility.SetDirty(newParent.groupAsset);
        }

        private void UpdateLevelParent(LevelHierarchyItem levelItem, LevelHierarchyItem newParent)
        {
            if (levelItem.levelAsset == null || newParent?.groupAsset == null) return;

            // Find current parent
            var oldParent = idToItem.Values
                .FirstOrDefault(i => i.type == LevelHierarchyItem.ItemType.Group && 
                                   i.groupAsset?.levels?.Contains(levelItem.levelAsset) == true)?.groupAsset;

            // Remove from old parent
            if (oldParent != null)
            {
                oldParent.levels.Remove(levelItem.levelAsset);
                EditorUtility.SetDirty(oldParent);
            }

            // Add to new parent
            if (newParent.groupAsset.levels == null)
                newParent.groupAsset.levels = new List<Level>();

            newParent.groupAsset.levels.Add(levelItem.levelAsset);
            EditorUtility.SetDirty(newParent.groupAsset);
        }

        public void Reload()
        {
            idToItem.Clear();
            rootItems.Clear();

            // Discover all groups and levels
            DiscoverLevelGroups();

            // Update tree view
            var items = new List<TreeViewItemData<LevelHierarchyItem>>(rootItems);
            treeView.Clear();
            treeView.SetRootItems(items);
            treeView.Rebuild();
        }

        private void DiscoverLevelGroups()
        {
            var processedLevelIds = new HashSet<int>();
            int itemId = 1;

            // Find all LevelGroup assets
            string[] groupGuids = AssetDatabase.FindAssets("t:LevelGroup");
            var groupToItem = new Dictionary<LevelGroup, TreeViewItemData<LevelHierarchyItem>>();

            foreach (string groupGuid in groupGuids)
            {
                string assetPath = AssetDatabase.GUIDToAssetPath(groupGuid);
                var group = AssetDatabase.LoadAssetAtPath<LevelGroup>(assetPath);
                
                if (group != null)
                {
                    var item = new LevelHierarchyItem
                    {
                        type = LevelHierarchyItem.ItemType.Group,
                        groupAsset = group,
                        assetPath = assetPath
                    };
                    
                    var itemData = new TreeViewItemData<LevelHierarchyItem>(itemId++, item);
                    groupToItem[group] = itemData;
                    idToItem[itemData.id] = item;
                }
            }

            // Build hierarchy
            foreach (var kvp in groupToItem)
            {
                var group = kvp.Key;
                var itemData = kvp.Value;

                if (group.parentGroup != null && groupToItem.TryGetValue(group.parentGroup, out var parentItemData))
                {
                    // Add as child to parent
                    var parentChildren = parentItemData.children?.ToList() ?? new List<TreeViewItemData<LevelHierarchyItem>>();
                    parentChildren.Add(itemData);
                    parentItemData = parentItemData.UpdateChildren(parentChildren);
                    groupToItem[group.parentGroup] = parentItemData;
                }
                else
                {
                    // Add to root items
                    rootItems.Add(itemData);
                }
            }

            // Add levels to their groups
            string[] levelGuids = AssetDatabase.FindAssets("t:Level");
            foreach (string levelGuid in levelGuids)
            {
                string assetPath = AssetDatabase.GUIDToAssetPath(levelGuid);
                var level = AssetDatabase.LoadAssetAtPath<Level>(assetPath);

                if (level != null)
                {
                    var parent = groupToItem.FirstOrDefault(kvp => 
                        kvp.Key.levels != null && kvp.Key.levels.Contains(level));

                    var item = new LevelHierarchyItem
                    {
                        type = LevelHierarchyItem.ItemType.Level,
                        levelAsset = level,
                        assetPath = assetPath
                    };

                    var itemData = new TreeViewItemData<LevelHierarchyItem>(itemId++, item);
                    idToItem[itemData.id] = item;

                    if (parent.Key != null)
                    {
                        var parentData = parent.Value;
                        var children = parentData.children?.ToList() ?? new List<TreeViewItemData<LevelHierarchyItem>>();
                        children.Add(itemData);
                        parentData = parentData.UpdateChildren(children);
                        groupToItem[parent.Key] = parentData;
                    }
                    else
                    {
                        rootItems.Add(itemData);
                    }
                }
            }

            // Sort items
            SortItems(rootItems);
        }

        private void SortItems(List<TreeViewItemData<LevelHierarchyItem>> items)
        {
            items.Sort((a, b) => 
            {
                var itemA = a.data;
                var itemB = b.data;

                // Groups come before levels
                if (itemA.type != itemB.type)
                    return itemA.type == LevelHierarchyItem.ItemType.Group ? -1 : 1;

                // Sort levels by number
                if (itemA.type == LevelHierarchyItem.ItemType.Level)
                    return (itemA.levelAsset?.number ?? 0).CompareTo(itemB.levelAsset?.number ?? 0);

                // Sort groups by name
                return string.Compare(itemA.groupAsset?.name, itemB.groupAsset?.name);
            });

            // Sort children recursively
            foreach (var item in items)
            {
                if (item.hasChildren)
                {
                    var children = item.children.ToList();
                    SortItems(children);
                    // Replace children with sorted list
                    items[items.IndexOf(item)] = item.UpdateChildren(children);
                }
            }
        }
    }
}
