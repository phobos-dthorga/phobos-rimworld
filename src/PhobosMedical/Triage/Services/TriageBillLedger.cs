using System.Collections.Generic;
using RimWorld;
using Verse;

namespace Phobos.Triage.Services;

/// <summary>
/// Which operation bills the doctors queued on their own, so the mod withdraws only those and never a bill the player added.
/// Saved by reference: the bills themselves live in each pawn's health tracker. Pruned before saving so a bill that has gone
/// never leaves a dangling reference.
/// </summary>
public sealed class TriageBillLedger : GameComponent
{
    List<Bill> bills = new();

    public TriageBillLedger(Game game) { }

    public static TriageBillLedger? Current => Verse.Current.Game?.GetComponent<TriageBillLedger>();

    public int Count => bills.Count;

    public void Add(Bill bill)
    {
        if (!bills.Contains(bill)) bills.Add(bill);
    }

    public bool IsOurs(Bill bill) => bills.Contains(bill);

    /// <summary>Our bills on one stack, in the stack's order.</summary>
    public void OnStack(BillStack stack, List<Bill> into)
    {
        into.Clear();
        List<Bill> stackBills = stack.Bills;
        for (int i = 0; i < stackBills.Count; i++)
        {
            if (bills.Contains(stackBills[i])) into.Add(stackBills[i]);
        }
    }

    /// <summary>Deletes one of our bills from its stack and forgets it. A doctor mid-job ends cleanly: the game's driver checks for a deleted bill.</summary>
    public void Withdraw(Bill bill)
    {
        bill.billStack?.Delete(bill);
        bills.Remove(bill);
    }

    /// <summary>Forgets bills that no longer sit in any stack (done, deleted by the player, or their pawn is gone).</summary>
    public void Prune()
    {
        for (int i = bills.Count - 1; i >= 0; i--)
        {
            Bill bill = bills[i];
            if (bill == null || bill.DeletedOrDereferenced || bill.billStack == null || !bill.billStack.Bills.Contains(bill)) bills.RemoveAt(i);
        }
    }

    public override void ExposeData()
    {
        base.ExposeData();
        if (Scribe.mode == LoadSaveMode.Saving) Prune();
        Scribe_Collections.Look(ref bills, "PhobosTriage_Bills", LookMode.Reference);
        if (Scribe.mode == LoadSaveMode.PostLoadInit)
        {
            bills ??= new List<Bill>();
            bills.RemoveAll(bill => bill == null);
        }
    }
}
