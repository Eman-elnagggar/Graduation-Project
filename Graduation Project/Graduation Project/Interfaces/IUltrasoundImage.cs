using Graduation_Project.Models;

namespace Graduation_Project.Interfaces
{
    public interface IUltrasoundImage
    {
        IEnumerable<UltrasoundImage> GetAll();
        UltrasoundImage GetById(int id);
        void Add(UltrasoundImage ultrasoundImage);
        void Update(UltrasoundImage ultrasoundImage);
        void Delete(int id);
        void Save();
    }
}
