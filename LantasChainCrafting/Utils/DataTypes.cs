using System;
using System.Collections;
using System.Collections.Generic;
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

        public static implicit operator Resource(uGUI_CraftingMenu.Node node) => new(node);
        public static implicit operator Resource(KeyValuePair<TechType, int> pair) => new(pair);
        public static implicit operator Resource(Ingredient ingredient) => new(ingredient);
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
