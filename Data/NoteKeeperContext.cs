using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
 
namespace HW5NoteKeeperSolution.Data
{
    public class NoteKeeperContext : DbContext
    {
        public NoteKeeperContext(DbContextOptions<NoteKeeperContext> options)
            : base(options)
        {
        }

        //public DbSet<HW5NoteKeeperSolution.Models.Movie> Movie { get; set; } = default!;
    }
}
