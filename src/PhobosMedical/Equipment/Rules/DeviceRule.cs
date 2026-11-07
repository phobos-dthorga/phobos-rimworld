namespace Phobos.Medical.Rules;

/// <summary>What a hand-held medical device is for. Each one is matched by what the patient's body shows, never by a condition's name.</summary>
public enum DeviceNeed
{
    /// <summary>A field oxygen kit: a patient who needs oxygen and isn't already breathing supplied oxygen.</summary>
    SuppliedOxygen,
    /// <summary>A manual resuscitator: breathing so weak the patient needs air pushed in by hand.</summary>
    BreathingAssist,
    /// <summary>An intubation kit: a breathing tube past a closing upper airway, or for a ventilator to breathe through.</summary>
    Intubation,
}

/// <summary>Why a doctor won't use a device on a patient, most urgent first: when several devices refuse, the lowest is the one to tell.</summary>
public enum DeviceRefusal
{
    None,
    /// <summary>Breathing is at zero: the airway is shut, and pushing air at it does nothing until it is opened.</summary>
    AirwayShut,
    /// <summary>The patient needs a device, but none can be reached.</summary>
    NoDevice,
    /// <summary>The device needs more medical skill than the doctor has.</summary>
    NeedsSkill,
    AlreadyOnOxygen,
    /// <summary>A breathing tube is already in (a tracheostomy or an earlier intubation).</summary>
    AlreadyHasTube,
    NotNeeded,
}

/// <summary>Who a device helps. Game-free; every threshold is passed in from the rules Def.</summary>
public static class DeviceRule
{
    /// <summary>A field oxygen kit helps a patient who needs oxygen, unless a machine or another kit already gives it.</summary>
    public static DeviceRefusal FieldOxygen(bool needsOxygen, bool onSuppliedOxygen, float breathing)
    {
        if (breathing <= 0f) return DeviceRefusal.AirwayShut;
        if (onSuppliedOxygen) return DeviceRefusal.AlreadyOnOxygen;
        return needsOxygen ? DeviceRefusal.None : DeviceRefusal.NotNeeded;
    }

    /// <summary>
    /// A resuscitator helps breathing that is weak but above zero. At zero no air gets past the airway, so a bag pushes nothing
    /// in (a gameplay line drawn sharper than real life, where a partly blocked airway can sometimes still be ventilated).
    /// </summary>
    public static DeviceRefusal BreathingAssist(float breathing, float threshold)
    {
        if (breathing <= 0f) return DeviceRefusal.AirwayShut;
        return breathing < threshold ? DeviceRefusal.None : DeviceRefusal.NotNeeded;
    }

    /// <summary>
    /// A breathing tube helps a patient without one whose upper airway is closing (breathing weak, with a condition a tube bypasses), or
    /// who lies at a working ventilator that needs a tube to breathe through. It works even with breathing at zero: getting past a shut
    /// airway is what it is for.
    /// </summary>
    public static DeviceRefusal Intubation(bool hasTube, bool airwayClosing, bool ventilatorWaiting)
    {
        if (hasTube) return DeviceRefusal.AlreadyHasTube;
        return airwayClosing || ventilatorWaiting ? DeviceRefusal.None : DeviceRefusal.NotNeeded;
    }

    /// <summary>Whether a refusal is worth showing in the right-click menu: not "breathes well enough", which is simply normal.</summary>
    public static bool WorthTelling(DeviceRefusal refusal) => refusal is not (DeviceRefusal.None or DeviceRefusal.NotNeeded);

    /// <summary>The one reason to tell when several devices refuse: the most urgent.</summary>
    public static DeviceRefusal MostUrgent(DeviceRefusal a, DeviceRefusal b) => a < b ? a : b;
}

/// <summary>How much a liquid oxygen store loses when its cooler has no power. Game-free.</summary>
public static class BoilOffRule
{
    public const int TicksPerDay = 60000;

    /// <summary>The oxygen that boils away in one interval: none while powered, else a fixed share of what is stored per day.</summary>
    public static float LossForInterval(float stored, float fractionPerDay, int intervalTicks, bool powered)
    {
        if (powered || stored <= 0f || fractionPerDay <= 0f || intervalTicks <= 0) return 0f;
        float loss = stored * fractionPerDay * intervalTicks / TicksPerDay;
        return loss > stored ? stored : loss;
    }
}
