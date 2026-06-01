using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "Recipe", menuName = "Perraria/Recipe")]
public sealed class Recipe : ScriptableObject
{
    [System.Serializable]
    public struct Ingredient
    {
        public ItemData Item;
        public int Count;
    }

    [SerializeField] private Ingredient[] _inputs;
    [SerializeField] private ItemData _output;
    [SerializeField] private int _outputCount = 1;
    [SerializeField] private ItemData[] _requiredStations;

    public IReadOnlyList<Ingredient> Inputs => _inputs;
    public ItemData Output => _output;
    public int OutputCount => _outputCount;
    public IReadOnlyList<ItemData> RequiredStations => _requiredStations;
    public bool RequiresStation => _requiredStations != null && _requiredStations.Length > 0;
}
