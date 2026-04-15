namespace HW5NoteKeeperSolution.Models
{
    /// <summary>
    /// The JSON response contract returned by the Azure OpenAI tag-generation prompt.
    /// Contains the list of keyword tags extracted from a note's details.
    /// </summary>
    public class KeyTagsResponse
    {
        /// <summary>Gets or sets the list of AI-generated keyword tags (typically 3–5 items).</summary>
        public List<string> Tags { get; set; } = [];
    }
}
