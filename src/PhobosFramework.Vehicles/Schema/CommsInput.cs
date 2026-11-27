namespace Phobos.VehicleSystems;

/// <summary>One of the Comms system's readings, from <see cref="VehicleComms"/>: <c>reading</c> is Radio or Link. Asked as the system's
/// derived quantities are worked out, on the systems' own rare step; one lookup in the radio net's last pass.</summary>
public class Input_Comms : VehicleSystemInput
{
    public string reading = "Link";

    protected override float Raw(SystemContext context) => reading == "Radio"
        ? (float)VehicleComms.RadioOf(context.Vehicle)
        : (float)VehicleComms.LinkOf(context.Vehicle);
}
