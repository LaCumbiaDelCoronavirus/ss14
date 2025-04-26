using Robust.Shared.Serialization;
using Robust.Shared.GameStates;

namespace Content.Shared.SignalJammer.Components;

/// <summary>
/// When activated (<see cref="ActiveSignalJammerComponent"/>)
/// This will, for anything in range: Prevent from sending radio messages,
/// Make suit sensors nonfunctional,
/// Make the Station AI unable to interact with anything
/// </summary>
[NetworkedComponent, RegisterComponent]
[AutoGenerateComponentState]
public sealed partial class SignalJammerComponent : Component
{
    [DataDefinition]
    public partial struct RadioJamSetting
    {
        /// <summary>
        /// Power usage per second when enabled.
        /// </summary>
        [DataField(required: true)]
        public float Wattage;

        /// <summary>
        /// Range of the jammer.
        /// </summary>
        [DataField(required: true)]
        public float Range;

        /// <summary>
        /// The message that is displayed when switched.
        /// to this setting.
        /// </summary>
        [DataField(required: true)]
        public LocId Message = string.Empty;

        /// <summary>
        /// Name of the setting.
        /// </summary>
        [DataField(required: true)]
        public LocId Name = string.Empty;
    }

    /// <summary>
    /// List of all the settings for the radio jammer.
    /// </summary>
    [DataField(required: true), ViewVariables(VVAccess.ReadOnly)]
    public RadioJamSetting[] Settings;

    /// <summary>
    /// Index of the currently selected setting.
    /// </summary>
    [DataField]
    [AutoNetworkedField]
    public int SelectedPowerLevel = 1;
}

[Serializable, NetSerializable]
public enum SignalJammerChargeLevel : byte
{
    Low,
    Medium,
    High
}

[Serializable, NetSerializable]
public enum SignalJammerLayers : byte
{
    LED
}

[Serializable, NetSerializable]
public enum SignalJammerVisuals : byte
{
    ChargeLevel,
    LEDOn
}
