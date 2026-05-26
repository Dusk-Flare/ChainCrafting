using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;

namespace ChainCrafting.Utils
{
    public static class Resources
    {
        public static List<Resource> ListOf(Dictionary<TechType, int> dictionary) => dictionary.Select(keyPair => (Resource)keyPair).ToList();
        public static List<Resource> ListOf(List<Ingredient> ingredients) => ingredients.Select(ing => (Resource)ing).ToList();
    }


    public record Resource(TechType Type, int Amount)
    {
        public Resource(TechType type) : this(type, 1) { }
        public Resource(KeyValuePair<TechType, int> pair) : this(pair.Key, pair.Value) { }
        public Resource(Ingredient ingredient) : this(ingredient.techType, ingredient.amount) { }
        public Resource() : this(TechType.None) { }
        public Resource(uGUI_CraftingMenu.Node node) : this(node.techType, 1) { }


        public bool Craftable => CraftTree.IsCraftable(Type);
        public int PickupCount => Inventory.main.GetPickupCount(Type);
        public int Yield => TechData.GetCraftAmount(Type);
        public List<Resource> Components => ComponentsOf(Type);
        public float CraftTime => CraftTimeOf(Type);

        public static List<Resource> ComponentsOf(TechType type)
        {
            List<Resource> ingredients = new();
            if (type == TechType.None || !CraftTree.IsCraftable(type)) return ingredients;
            ReadOnlyCollection<Ingredient> ingredientArray = TechData.GetIngredients(type);
            foreach (Ingredient ingredient in ingredientArray) ingredients.Add(ingredient);
            return ingredients;
        }

        public static float CraftTimeOf(TechType type) => TechData.GetCraftTime(type, out float time) ? time : 0;

        public static Resource operator +(Resource resource, int value) => resource with { Amount = resource.Amount + value };
        public static Resource operator -(Resource resource, int value) => resource with { Amount = Math.Max(0, resource.Amount - value) };
        public static Resource operator *(Resource resource, int value) => resource with { Amount = resource.Amount * value };

        public static implicit operator Ingredient(Resource resource) => new(resource.Type, resource.Amount);
        public static implicit operator Resource(uGUI_CraftingMenu.Node node) => new(node);
        public static implicit operator Resource(KeyValuePair<TechType, int> pair) => new(pair);
        public static implicit operator Resource(Ingredient ingredient) => new(ingredient);
        public override string ToString() => $"{Type}: {Amount}";
    }

    public record ResourceTable(Dictionary<TechType, int> Table)
    {
        public ResourceTable() : this(new Dictionary<TechType, int>()) { }
        public ResourceTable(List<Resource> resources) : this(resources.ToDictionary(entry => entry.Type, entry => entry.Amount)) { }
        public bool Contains(TechType type) => Table.ContainsKey(type);
        public bool Contains(Resource resource) => Contains(resource.Type);
        public Dictionary<TechType, int>.KeyCollection Keys => Table.Keys;
        public Dictionary<TechType, int>.ValueCollection Values => Table.Values;

        public Resource this[TechType type] 
        { 
            get 
            {
                if (Table.TryGetValue(type, out int amount)) return new(type, amount);
                return null;
            }
            set
            {
                Set(type, value.Amount);
            }
        }
        public void Set(TechType type, int ammount) => Table[type] = ammount;

        public bool Add(TechType type, int amount)
        {
            if (Contains(type)) 
            { 
                Table[type] += amount; 
                return true;
            }
            Table[type] = amount; 
            return false;
        }
        public bool AddAll(List<Resource> resources)
        {
            bool anyAdded = false;
            foreach (Resource resource in resources) anyAdded |= Add(resource);
            return anyAdded;
        }
        public void Subtract(TechType type, int amount)
        {
            if (Contains(type)) 
            {
                Table[type] -= amount;
                if (Table[type] <= 0) Remove(type);
            }
        }
        public void Remove(TechType type) => Table.Remove(type);
        public void Set(Resource resource) => Set(resource.Type, resource.Amount);
        public bool Add(Resource resource) => Add(resource.Type, resource.Amount);
        public bool AddAll(ResourceTable resourceTable) => AddAll(resourceTable.ToList());
        public void Remove(Resource resource) => Remove(resource.Type);
        public void Subtract(Resource resource) => Subtract(resource.Type, resource.Amount);
        public void Clear() => Table.Clear();
        public List<Resource> ToList() => ToList(this);
        public Dictionary<TechType, int>.Enumerator GetEnumerator() => Table.GetEnumerator();

        public static List<Resource> ToList(ResourceTable resourceTable) => Resources.ListOf(resourceTable.Table);
        public static implicit operator List<Resource>(ResourceTable resources) => resources.Table.Select(entry => new Resource(entry)).ToList();
        public static implicit operator ResourceTable(List<Resource> resources) => new(resources);
        public static implicit operator ResourceTable(Dictionary<TechType, int> resources) => new(resources);
    }

    public class ResourceTree
    {
        public Resource Data{ get; private set; }
        private ResourceTree Parent { init; get; }
        private ResourceTree Root { init; get; }
        private readonly int layer;
        private readonly List<ResourceTree> children = new();
        public ResourceTree(ResourceTree parent, Resource data, int layer = 0)
        {
            Parent = parent;
            Data = data ?? new(TechType.None);
            Root = GetRoot();
            this.layer = layer;
            foreach (Resource child in data.Components) AddChild(child);
            if (Root == this) Padd(Root, Root.layer, Depth(Root));
        }

        public int GetOuterRing() 
        {
            return GetLayerCount(Root, Depth(Root));
        }

        private static int GetLayerCount(ResourceTree node, int target)
        {
            if (node.layer == target) return 1;
            int sum = 0;
            foreach (ResourceTree child in node.children) sum += GetLayerCount(child, target);
            return sum;
        }

        public ResourceTree GetRoot()
        {
            if (Parent == null) return this;
            return Parent.GetRoot();
        }

        private static int Depth(ResourceTree node)
        {
            int depth = node.layer;
            if (node.children.Count == 0) return depth;
            foreach (ResourceTree child in node.children) depth = Math.Max(depth, Depth(child));
            return depth;
        }

        public static void Padd(ResourceTree currentNode, int depth, int maxDepth)
        {
            if (depth >= maxDepth) return;
            if (currentNode.children.Count == 0) currentNode.AddChild();
            foreach (ResourceTree child in currentNode.children) Padd(child, child.layer, maxDepth);
        }

        public void AddChild(Resource child = null) 
        {
            child ??= new();
            children.Add(new(this, child, layer + 1));
        }

        public void LogTree()
        {
            Plugin.Logger.LogInfo(Data);
            foreach (ResourceTree child in children) child.LogTree();
        }
    }
}
