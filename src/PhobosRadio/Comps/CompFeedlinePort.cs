using Phobos.Radio.Services;
using PipeSystem;
using Verse;

namespace Phobos.Radio;

/// <summary>
/// Where coax joins a station or an antenna. A Vanilla Expanded Framework pipe-net connector carrying no
/// resource: the net is only a connectivity graph, and <see cref="FeedlineService"/> walks it.
/// </summary>
public class CompProperties_FeedlinePort : CompProperties_Resource
{
    public CompProperties_FeedlinePort() => compClass = typeof(CompFeedlinePort);
}

public class CompFeedlinePort : CompResource
{
    public override string CompInspectStringExtra()
    {
        if (!parent.Spawned) return string.Empty;
        if (PipeNet == null) return "PhobosRadio_PortNoCable".Translate();
        var isStation = parent.TryGetComp<CompRadio>()?.Props.isStation == true;
        if (isStation)
        {
            var antennas = FeedlineService.AntennasOn(this);
            return antennas.Count == 0
                ? "PhobosRadio_PortCableNoAntenna".Translate()
                : "PhobosRadio_PortAntennasOnCable".Translate(antennas.Count);
        }
        var station = FeedlineService.StationOn(this);
        return station == null
            ? "PhobosRadio_PortCableNoStation".Translate()
            : "PhobosRadio_PortConnectedTo".Translate(station.parent.LabelCap);
    }
}
