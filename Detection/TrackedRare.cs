using ExileCore.PoEMemory.MemoryObjects;
using System.Numerics;

namespace WhatsAHeadhunter.Detection;

public class TrackedRare
{
    public Entity Entity { get; set; } = null!;
    public bool HasShroudWalker { get; set; }
    public Vector2 GridPos { get; set; }
    public Vector3 WorldPos { get; set; }
    public string Name { get; set; } = "";
    public bool IsValid => Entity?.IsValid == true && Entity.IsAlive;
}
