using Content.Shared.Interaction;
using Content.Shared.RCD;
using Content.Shared.RCD.Components;
using Content.Shared.RCD.Systems;

namespace Content.Client.RCD;

public sealed class RCDSystem : SharedRCDSystem
{
    public override bool TryRcdAfterInteract(Entity<RCDComponent> rcd, RCDPrototype selectedRcdPrototype, ref AfterInteractEvent args)
    {
        base.TryRcdAfterInteract(rcd, selectedRcdPrototype, ref args);
        return false;
    }
}
