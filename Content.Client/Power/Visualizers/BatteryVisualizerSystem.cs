using Robust.Client.GameObjects;
using Content.Shared.Power;
using Content.Shared.Power.Visualizers;
using System.Runtime.CompilerServices;

namespace Content.Client.Power.Visualizers;

public sealed class SmesVisualizerSystem : VisualizerSystem<BatteryVisualizerComponent>
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void UpdateChargeLayerState(SpriteComponent.Layer layer, string? updatedState)
    {
        if (updatedState is { } state)
        {
            SpriteSystem.LayerSetVisible(layer, true);
            SpriteSystem.LayerSetRsiState(layer, state);
        }
        else
            SpriteSystem.LayerSetVisible(layer, false);
    }

    protected override void OnAppearanceChange(EntityUid uid, BatteryVisualizerComponent visualizerComponent, ref AppearanceChangeEvent args)
    {
        if (args.Sprite is not { } spriteComponent)
            return;

        var spriteEntity = (uid, spriteComponent);

        // Charge levels
        if (visualizerComponent.ChargeOverlayPrefix != null &&
            SpriteSystem.TryGetLayer(spriteEntity, BatteryVisualLayers.Charge, out var chargeLayer, false))
        {
            if (!AppearanceSystem.TryGetData<int>(uid, BatteryVisuals.ChargeLevel, out var level, args.Component) || level == 0)
            {
                SpriteSystem.LayerSetVisible(chargeLayer, false);
            }
            else
            {
                SpriteSystem.LayerSetVisible(chargeLayer, true);
                SpriteSystem.LayerSetRsiState(chargeLayer, $"{visualizerComponent.ChargeOverlayPrefix}{level}");
            }
        }

        // Charge states

        // Layers
        var inputLayerExists = SpriteSystem.TryGetLayer(spriteEntity, BatteryVisualLayers.Input, out var inputLayer, false);
        var outputLayerExists = SpriteSystem.TryGetLayer(spriteEntity, BatteryVisualLayers.Input, out var outputLayer, false);

        // Base logic
        if (!AppearanceSystem.TryGetData<ChargeState>(uid, BatteryVisuals.ChargeState, out var state, args.Component) ||
            !visualizerComponent.ChargeStateOverlays.TryGetValue(state, out var chargeStateOverlayStates))
        {
            if (inputLayerExists)
                SpriteSystem.LayerSetVisible(inputLayer!, false);

            if (outputLayerExists)
                SpriteSystem.LayerSetVisible(outputLayer!, false);

            return;
        }

        // Input layer logic
        if (inputLayerExists)
        {
            if (chargeStateOverlayStates.InputState is { } inputState)
            {
                SpriteSystem.LayerSetVisible(inputLayer!, true);
                SpriteSystem.LayerSetRsiState(inputLayer!, inputState);
            }
            else
                SpriteSystem.LayerSetVisible(inputLayer!, false);
        }

        // Output layer logic
        if (outputLayerExists)
        {
            if (chargeStateOverlayStates.OutputState is { } outputState)
            {
                SpriteSystem.LayerSetVisible(outputLayer!, true);
                SpriteSystem.LayerSetRsiState(outputLayer!, outputState);
            }
            else
                SpriteSystem.LayerSetVisible(outputLayer!, false);
        }
    }
}

/// <summary>
/// Battery visuals layers. On an entity with <see cref="BatteryVisualizerComponent"/>, these are optional. 
/// </summary>
public enum BatteryVisualLayers : byte
{
    Input,
    Output,
    Charge,
}
