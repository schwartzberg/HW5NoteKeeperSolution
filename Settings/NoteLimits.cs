namespace HW5NoteKeeper.Settings
{
    /// <summary>
    /// Application-level limits for notes per user.
    /// Bound from the <c>NoteLimits</c> configuration section.
    /// </summary>
    public class NoteLimits
    {
        /// <summary>Gets or sets the maximum number of notes a user may create. Defaults to 10.</summary>
        public int MaxNotes { get; set; } = 10;
    }
}
