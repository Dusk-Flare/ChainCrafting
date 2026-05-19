using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace ChainCrafting.Utils
{
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
        public List<Resource> Ingredients
        {
            get
            {
                List<Resource> ingredients = new();
                if (Type == TechType.None || !CraftTree.IsCraftable(Type)) return ingredients; 
                ReadOnlyCollection<Ingredient> ingredientArray = TechData.GetIngredients(Type);
                foreach (Ingredient ingredient in ingredientArray) ingredients.Add(ingredient);
                return ingredients;
            }
        }
        public float CraftTime
        {
            get
            {
                TechData.GetCraftTime(Type, out float craftTime);
                return craftTime;
            }
        }

        public static List<Resource> ListOf(Dictionary<TechType, int> dictionary)
        {
            List<Resource> list = new();
            foreach (var pair in dictionary) list.Add(pair);
            return list;
        }
        public static Resource operator +(Resource resource, int value) => resource with { Amount = resource.Amount + value };
        public static Resource operator -(Resource resource, int value) => resource with { Amount = resource.Amount - value };
        public static Resource operator *(Resource resource, int value) => resource with { Amount = resource.Amount * value };

        public static implicit operator Ingredient(Resource resource) => new(resource.Type, resource.Amount);
        public static implicit operator Resource(uGUI_CraftingMenu.Node node) => new(node);
        public static implicit operator Resource(KeyValuePair<TechType, int> pair) => new(pair);
        public static implicit operator Resource(Ingredient ingredient) => new(ingredient);
        public override string ToString() => $"{Type}: {Amount}";
    }

    public record ResourceTable(Dictionary<TechType, int> Resources)
    {
        public ResourceTable() : this(new Dictionary<TechType, int>()) { }
        public bool Contains(TechType type) => Resources.ContainsKey(type);
        public bool Contains(Resource resource) => Contains(resource.Type);
        public Dictionary<TechType, int>.KeyCollection Keys => Resources.Keys;
        public Dictionary<TechType, int>.ValueCollection Values => Resources.Values;

        public Resource this[TechType type] 
        { 
            get 
            {
                if (Resources.TryGetValue(type, out int amount)) return new(type, amount);
                return null;
            }
            set
            {
                Set(type, value.Amount);
            }
        }
        public void Set(TechType type, int ammount) => Resources[type] = ammount;

        public bool Add(TechType type, int amount)
        {
            if (Contains(type)) 
            { 
                Resources[type] += amount; 
                return true;
            }
            Resources[type] = amount; 
            return false;
        }
        public void Remove(TechType type) => Resources.Remove(type);
        public void Remove(TechType type, int amount)
        {
            if (Contains(type)) 
            {
                Resources[type] -= amount;
                if (Resources[type] <= 0) Remove(type);
            }
        }
        public void Set(Resource resource) => Set(resource.Type, resource.Amount);
        public bool Add(Resource resource) => Add(resource.Type, resource.Amount);
        public void Remove(Resource resource) => Remove(resource.Type, resource.Amount);
        public void Clear() => Resources.Clear();
        public Dictionary<TechType, int>.Enumerator GetEnumerator() => Resources.GetEnumerator();
        public List<Resource> ToList() => Resources.Select(entry => new Resource(entry)).ToList();
        public static implicit operator ResourceTable(Dictionary<TechType, int> resources) => new(resources);
    }

    /*public record ResourceTree(Resource Resource, List<ResourceTree> Children) : IEnumerable<ResourceTree>, IEnumerable
    {
        public ResourceTree(TechType type, int amount) : this(new Resource(type, amount), new List<ResourceTree>()) { }
        public ResourceTree(TechType type) : this(type, 1) { }

        public static implicit operator ResourceTree(uGUI_CraftingMenu.Node node)
        {
            Resource resource = node;
            List<ResourceTree> children = new();
            foreach (uGUI_CraftingMenu.Node child in node) children.Add(child);
            return new ResourceTree(resource, children);
        }

        public void AddChild(ResourceTree child) => Children.Add(child);

        public IEnumerator<ResourceTree> GetEnumerator()
        {
            for (int i = 0; i < this.Children.Count; i++) yield return this.Children[i];
            yield break;
        }

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }

    public class _ResourceTree 
    {
        public Resource Root;
        public List<_ResourceTree> Children;
        public int Depth;
        public Dictionary<int, List<Resource>> Layers
        {
            get
            {
                Dictionary<int, List<Resource>> dict = new();
                Queue<_ResourceTree> queue = new();
                queue.Enqueue(this);
                dict[0].Add(Root);
                while (queue.Count > 0)
                {
                    _ResourceTree node = queue.Dequeue();
                    foreach (_ResourceTree child in node.Children)
                    {
                        dict[Depth - child.Depth].Add(child.Root);
                        queue.Enqueue(child);
                    }
                }
                return dict;
            }
        }

        public _ResourceTree(Resource Root) 
        {
            this.Root = Root;
            (Children, Depth) = Populate(Root, 0);
            Padd(this, 0, Depth);
        }

        private void AddChild(_ResourceTree node)
        {
            Children.Add(node);
            Depth += node.Depth;
        }

        private void AddChild(Resource resource)
        {
            AddChild(new _ResourceTree(resource));
        }

        private static (List<_ResourceTree>, int) Populate(Resource Node, int Depth)
        {
            List<_ResourceTree> children = new();
            foreach(Resource item in Node.Ingredients)
            {
                (List<_ResourceTree> babies, int newDepth) = Populate(item, Depth + 1);
                children.AddRange(babies);
                Depth = Math.Max(Depth, newDepth);
            }
            return (children,  Depth);
        }

        private static void Padd(_ResourceTree current, int depth, int maxDepth)
        {
            if (current.Children.Count == 0 && depth < maxDepth)
            {
                current.Children = new List<_ResourceTree>();
                current.AddChild(new Resource(TechType.None));
            }
            foreach(_ResourceTree item in current.Children) Padd(current, depth++, maxDepth);
        }
    }*/

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
            foreach (Resource child in data.Ingredients) AddChild(child);
            if (Root == this) Padd(Root, Root.layer, Depth(Root));
        }

        public int GetOuterRing() 
        {
            return GetLayer(Root, Depth(Root));
        }

        private static int GetLayer(ResourceTree node, int target)
        {
            if (node.layer == target) return 1;
            int sum = 0;
            foreach (ResourceTree child in node.children) sum += GetLayer(child, target);
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
