using System.Collections.Generic;
using static CraftingRecipe;

/// <summary>
/// The crafting system (M9, K4): the recipe table (game-design §4.7.3) plus the
/// craft operation against the <see cref="Inventory"/>. Recipes unlock through
/// <see cref="QuestLog"/> flags. Includes the craftable <b>Mảnh Cổng</b> (Portal
/// Shard, 4× Energy Shard) so a player short on quest-drop shards can still power the
/// Stage-2 portal — no soft-lock. Static, no state of its own; the crafting UI
/// listens to <see cref="Inventory.OnChanged"/> / <see cref="QuestLog.OnChanged"/>.
/// </summary>
public static class Crafting
{
    static List<CraftingRecipe> recipes;

    public static IReadOnlyList<CraftingRecipe> Recipes
    {
        get { EnsureLoaded(); return recipes; }
    }

    /// <summary>True if the recipe is known (default, or its unlock flag is set).</summary>
    public static bool IsUnlocked(CraftingRecipe r) =>
        string.IsNullOrEmpty(r.unlockFlag) || QuestLog.HasFlag(r.unlockFlag);

    /// <summary>True if unlocked and the bag holds every ingredient.</summary>
    public static bool CanCraft(CraftingRecipe r)
    {
        if (!IsUnlocked(r)) return false;
        foreach (var ing in r.inputs)
            if (!Inventory.Has(ing.id, ing.count)) return false;
        return true;
    }

    /// <summary>Consume the inputs and grant the output. False if not craftable now.</summary>
    public static bool TryCraft(CraftingRecipe r)
    {
        if (!CanCraft(r)) return false;
        foreach (var ing in r.inputs)
            Inventory.TryRemove(ing.id, ing.count);
        Inventory.TryAdd(r.outputId, r.outputCount);
        return true;
    }

    static void EnsureLoaded()
    {
        if (recipes != null) return;
        recipes = new List<CraftingRecipe>
        {
            // Default recipes.
            new CraftingRecipe("spring_water", 1, "",
                new Ingredient("bottle", 3)),
            new CraftingRecipe("energy_drink", 1, "",
                new Ingredient("herb", 2), new Ingredient("bottle", 1)),

            // Unlocked by Ông Bear's "Tái Chế Nâng Cao" quest.
            new CraftingRecipe("sprout_shield", 1, QuestLog.FlagRecipeAdvanced,
                new Ingredient("scrap", 5)),
            new CraftingRecipe("seed_bomb", 1, QuestLog.FlagRecipeAdvanced,
                new Ingredient("scrap", 3), new Ingredient("bottle", 2)),

            // Unlocked by Ông Sáu after the antidote quest (QuestLog bridges M8).
            new CraftingRecipe("large_heal", 1, QuestLog.FlagRecipeLargeHeal,
                new Ingredient("herb", 2), new Ingredient("energy_shard", 1)),

            // Unlocked by Cô Lan mid-game — the anti-soft-lock Portal Shard path.
            new CraftingRecipe("portal_shard", 1, QuestLog.FlagRecipePortal,
                new Ingredient("energy_shard", 4)),
        };
    }
}
