using System;

/// <summary>
/// One crafting recipe (M9, K4): a set of material inputs → an output item. Recipes
/// unlock via <see cref="unlockFlag"/> (a <see cref="QuestLog"/> flag; empty = known
/// by default). Plain data authored in code by <see cref="Crafting"/> from the
/// game-design §4.7.3 table.
/// </summary>
[Serializable]
public class CraftingRecipe
{
    [Serializable]
    public struct Ingredient
    {
        public string id;
        public int count;
        public Ingredient(string id, int count) { this.id = id; this.count = count; }
    }

    public readonly string outputId;
    public readonly int outputCount;
    public readonly Ingredient[] inputs;
    /// <summary>QuestLog flag that unlocks this recipe; empty/null = unlocked by default.</summary>
    public readonly string unlockFlag;

    public CraftingRecipe(string outputId, int outputCount, string unlockFlag, params Ingredient[] inputs)
    {
        this.outputId = outputId;
        this.outputCount = outputCount;
        this.unlockFlag = unlockFlag;
        this.inputs = inputs;
    }
}
