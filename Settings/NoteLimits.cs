namespace HW5NoteKeeperSolution.Settings
{
    /// <summary>
    /// Application-level limits for notes and attachments per user.
    /// Bound from the <c>NoteLimits</c> configuration section.
    /// </summary>
    public class NoteLimits
    {
        /// <summary>Gets or sets the maximum number of notes a user may create. Defaults to 10.</summary>
        public int MaxNotes { get; set; } = 10;

        /// <summary>Gets or sets the maximum number of blob attachments allowed per note. Defaults to 3.</summary>
        public int MaxAttachments { get; set; } = 3;
    }
}
