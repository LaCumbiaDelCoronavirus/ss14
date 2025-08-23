using Content.Shared.DoAfter;
using Robust.Shared.Serialization;

namespace Content.Shared.Aircraft.Events;

/// <summary>
/// Event raised upon finishing the doafter when a person tries to enter an aircraft, on both success and failure.
/// </summary>
[Serializable, NetSerializable]
public sealed partial class AircraftEntryDoAfterEvent : SimpleDoAfterEvent;

/// <summary>
/// Event raised upon finishing the doafter when a person tries to exit an aircraft, on both success and failure.
/// </summary>
[Serializable, NetSerializable]
public sealed partial class AircraftExitDoAfterEvent : SimpleDoAfterEvent;
