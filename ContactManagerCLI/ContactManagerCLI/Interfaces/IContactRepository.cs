using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using ContactManagerCLI.Models;

namespace ContactManagerCLI.Interfaces
{
    public interface IContactRepository
    {
        void Add(Contact contact);
        void Update(Contact contact);
        void Delete(Guid id);
        Contact? GetById(Guid id);
        IEnumerable<Contact> GetAll();
        IEnumerable<Contact> SearchByName(string name);
        IEnumerable<Contact> FilterByDate(DateTime from, DateTime to);
        Task SaveAsync();
        Task LoadAsync();

        // Pagination support
        IEnumerable<Contact> GetPage(int page, int pageSize);
        int GetTotalCount();
    }
}
