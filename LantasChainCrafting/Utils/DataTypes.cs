using Mono.Cecil;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ChainCrafting.Utils
{
    public record Resource(TechType Type, int Amount)
    {
        public Resource(TechType type) : this(type, 1) { }
        public Resource(KeyValuePair<TechType, int> pair) : this(pair.Key, pair.Value) { }
        public Resource(Ingredient ingredient) : this(ingredient.techType, ingredient.amount) { }

        public Resource(uGUI_CraftingMenu.Node node) : this(node.techType, 1) { }


        public bool Craftable => CraftTree.IsCraftable(Type);
        public int PickupCount => Inventory.main.GetPickupCount(Type);
        public int Yield => TechData.GetCraftAmount(Type);
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

        public static implicit operator Ingredient(Resource resource) => new(resource.Type, resource.Amount);
        public static implicit operator Resource(uGUI_CraftingMenu.Node node) => new(node);
        public static implicit operator Resource(KeyValuePair<TechType, int> pair) => new(pair);
        public static implicit operator Resource(Ingredient ingredient) => new(ingredient);
    }

    public record ResourceTable(Dictionary<TechType, int> Resources)
    {
        public ResourceTable() : this(new Dictionary<TechType, int>()) { }
        public bool Contains(TechType type) => Resources.ContainsKey(type);
        public bool Contains(Resource resource) => Contains(resource.Type);

        public int GetAmmount(TechType type) => Resources[type];
        public Dictionary<TechType, int>.KeyCollection Keys => Resources.Keys;
        public Dictionary<TechType,int>.ValueCollection Values => Resources.Values;

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
            if (Contains(type)) Resources[type] -= amount;
            if (Resources[type] <= 0) Remove(type);
        }
        public void Set(Resource resource) => Set(resource.Type, resource.Amount);
        public bool Add(Resource resource) => Add(resource.Type, resource.Amount);
        public void Remove(Resource resource) => Remove(resource.Type, resource.Amount);
        public void Clear() => Resources.Clear();
        public Dictionary<TechType, int>.Enumerator GetEnumerator() => Resources.GetEnumerator();
        public List<Resource> ToList() => Resources.Select(entry => new Resource(entry)).ToList();
        public static implicit operator ResourceTable(Dictionary<TechType, int> resources) => new(resources);
    }

    public record ResourceTree(Resource Resource, List<ResourceTree> Children) : IEnumerable<ResourceTree>, IEnumerable
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
}
