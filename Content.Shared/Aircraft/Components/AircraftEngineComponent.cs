using Robust.Shared.GameStates;

namespace Content.Shared.Aircraft.Components;

[RegisterComponent, NetworkedComponent]
public sealed partial class AircraftEngineComponent : Component
{
    /// <summary>
    /// Whether the engines are working.
    /// </summary>
    public bool Enabled = false;

    /// <summary>
    /// A scalar from 0 (fully off) to 1 (fully on) representing the state of the aircraft's engines.
    /// </summary>
    public float EnginePower = 0;
}
