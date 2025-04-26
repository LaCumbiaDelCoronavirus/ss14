using Content.Shared.SignalJammer.EntitySystems;
using Robust.Shared.GameStates;

namespace Content.Shared.Radio.Components;

/// <summary>
/// Prevents all radio in range from sending messages, and prevents AI from interacting with anything in-range
/// </summary>
[RegisterComponent, NetworkedComponent]
[Access(typeof(SharedJammerSystem))]
public sealed partial class ActiveSignalJammerComponent : Component
{
}
