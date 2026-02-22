using Graduation_Project.Data;
using Graduation_Project.Interfaces;
using Graduation_Project.Models;
using Microsoft.EntityFrameworkCore;

namespace Graduation_Project.Repository
{
    public class NoteRepository : INote
    {
        private readonly AppDbContext _context;

        public NoteRepository(AppDbContext context)
        {
            _context = context;
        }

        public IEnumerable<Note> GetAll() => _context.Notes.ToList();

        public Note GetById(int id) => _context.Notes.Find(id);

        public void Add(Note note) => _context.Notes.Add(note);

        public void Update(Note note) => _context.Notes.Update(note);

        public void Delete(int id)
        {
            var entity = _context.Notes.Find(id);
            if (entity != null)
                _context.Notes.Remove(entity);
        }

        public void Save() => _context.SaveChanges();

        public IEnumerable<Note> GetByPatientId(int patientId) =>
            _context.Notes
                .Where(n => n.PatientID == patientId)
                .Include(n => n.Doctor).ThenInclude(d => d.User)
                .OrderByDescending(n => n.CreatedDate)
                .ToList();
    }
}
