using System;
using ProbetaControl.Models;

namespace ProbetaControl.Services
{
    /// <summary>
    /// Who may change what on a compression test, as in the legacy app:
    /// a test is "unlocked" for an administrator (or an operator an administrator unlocked it for),
    /// and for the operator who just created it. Otherwise an operator may only record the rupture
    /// load of a specimen that has none yet and whose rupture date has not passed.
    /// </summary>
    public static class SpecimenEditPolicy
    {
        public static EditDecision CanEdit(Specimen specimen, bool testUnlocked, DateTime today)
        {
            if (testUnlocked)
            {
                return EditDecision.Full;
            }

            if (specimen.IsTested)
            {
                return EditDecision.Deny("La probeta ya tiene la rotura registrada. El administrador debe desbloquear el ensayo.");
            }

            if (specimen.RuptureDate.Date < today.Date)
            {
                return EditDecision.Deny("Probeta retrasada. El administrador debe desbloquear el ensayo.");
            }

            return specimen.IsLocked ? EditDecision.RuptureLoadOnly : EditDecision.Full;
        }

        /// <summary>Adding, copying or archiving specimens and editing the test header.</summary>
        public static EditDecision CanChangeStructure(bool testUnlocked) =>
            testUnlocked
                ? EditDecision.Full
                : EditDecision.Deny("El ensayo está bloqueado. El administrador debe desbloquearlo.");
    }

    public sealed class EditDecision
    {
        private EditDecision(bool allowed, bool ruptureLoadOnly, string reason)
        {
            IsAllowed = allowed;
            IsRuptureLoadOnly = ruptureLoadOnly;
            Reason = reason;
        }

        public static EditDecision Full { get; } = new EditDecision(true, false, null);

        public static EditDecision RuptureLoadOnly { get; } = new EditDecision(true, true, null);

        public bool IsAllowed { get; }

        /// <summary>Only the rupture load may change; measurements and dates are read-only.</summary>
        public bool IsRuptureLoadOnly { get; }

        /// <summary>Spanish message for the user when not allowed.</summary>
        public string Reason { get; }

        public static EditDecision Deny(string reason) => new EditDecision(false, false, reason);
    }
}
