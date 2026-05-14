namespace SharpEngine.World.Blocks;

public sealed class BlockRegistry
{
    private readonly Dictionary<ushort, BlockDefinition> _byId = new();
    private readonly Dictionary<string, BlockDefinition> _byName = new(StringComparer.Ordinal);

    public IReadOnlyCollection<BlockDefinition> Blocks => _byId.Values;

    public void Register(BlockDefinition block)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(block.Name);

        if (!_byId.TryAdd(block.Id, block))
        {
            throw new InvalidOperationException($"Block id {block.Id} is already registered.");
        }

        if (!_byName.TryAdd(block.Name, block))
        {
            _byId.Remove(block.Id);
            throw new InvalidOperationException($"Block name '{block.Name}' is already registered.");
        }
    }

    public BlockDefinition Get(ushort id) => _byId.TryGetValue(id, out BlockDefinition? block)
        ? block
        : throw new KeyNotFoundException($"Block id {id} is not registered.");

    public BlockDefinition Get(string name) => _byName.TryGetValue(name, out BlockDefinition? block)
        ? block
        : throw new KeyNotFoundException($"Block name '{name}' is not registered.");
}

