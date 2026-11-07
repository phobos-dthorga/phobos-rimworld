using System;
using Phobos.Framework.Rules;
using UnityEngine;

namespace Phobos.Framework.Work;

/// <summary>
/// One long job that runs a slice per drawn frame, paused or not, so the game keeps responding: a strategic blast crossing a map,
/// a pass over every cell. Queue it with <see cref="FrameWorkQueue.Enqueue"/>. A subclass says how to start (take its snapshot),
/// how to take one step, when it is finished, and how much of a frame it may use; the queue does the timing. Work is never saved:
/// a job still running when the game is saved is forgotten on load, so a job must leave the map in a sound state after every step.
/// </summary>
public abstract class FrameWork
{
    public bool Started { get; private set; }
    /// <summary>Frames this job has had a slice in.</summary>
    public int Frames { get; internal set; }
    /// <summary>Real time spent inside this job's slices, in milliseconds.</summary>
    public double WorkMs { get; internal set; }
    /// <summary>Real time (Time.realtimeSinceStartup) when the job started, or -1.</summary>
    public float StartedAt { get; private set; } = -1f;
    public float SecondsSinceStart => StartedAt < 0f ? 0f : Time.realtimeSinceStartup - StartedAt;

    /// <summary>For the log: what the job is, in a few words.</summary>
    public abstract string Label { get; }

    /// <summary>The label for logs, which must never throw: a job whose map has gone still needs a name in the line that drops it.</summary>
    public string SafeLabel
    {
        get
        {
            try { return Label; }
            catch (Exception) { return GetType().Name; }
        }
    }

    /// <summary>False once the job can no longer run (its map is gone); the queue drops it and calls <see cref="Dropped"/>.</summary>
    public virtual bool StillValid => true;

    /// <summary>The time this frame's slice may take. Asked once per frame, so it can change as the job goes.</summary>
    public virtual float BudgetMs => SliceBudgetRule.DefaultBudgetMs;

    /// <summary>Steps every slice takes even past its budget.</summary>
    public virtual int MinimumStepsPerSlice => 1;

    public abstract bool Finished { get; }

    internal void DoStart()
    {
        Started = true;
        StartedAt = Time.realtimeSinceStartup;
        Start();
    }

    /// <summary>Called once, in the first slice: take the snapshot of what to do.</summary>
    protected abstract void Start();

    /// <summary>One unit of work. Catch per-step failures here; an exception that escapes stops the job.</summary>
    protected internal abstract void Step();

    /// <summary>Set up and undo per-slice context (held room rebuilds, another mod's setting); EndSlice runs even after an exception.</summary>
    protected internal virtual void BeginSlice() { }
    protected internal virtual void EndSlice() { }

    /// <summary>Called once, after the last step.</summary>
    protected internal virtual void Finish() { }

    /// <summary>Called instead of Finish when the job is dropped unfinished.</summary>
    protected internal virtual void Dropped() { }

    /// <summary>
    /// Offered each job queued after this one while this one has not started: return true to take it over (run its work as part of
    /// this one), and the queue drops it. Repeated identical jobs fold into one this way.
    /// </summary>
    public virtual bool TryAbsorb(FrameWork later) => false;
}
