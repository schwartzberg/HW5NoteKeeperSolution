using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using HW5NoteKeeperSolution.Data;
using HW5NoteKeeperSolution.Models;

namespace HW5NoteKeeperSolution.Pages.Notes
{
    public class IndexModel : PageModel
    {
        private readonly HW5NoteKeeperSolution.Data.NoteKeeperContext _context;

        public IndexModel(HW5NoteKeeperSolution.Data.NoteKeeperContext context)
        {
            _context = context;
        }

        public IList<Note> Note { get;set; } = default!;

        public async Task OnGetAsync()
        {
            Note = await _context.Notes.ToListAsync();
        }
    }
}
