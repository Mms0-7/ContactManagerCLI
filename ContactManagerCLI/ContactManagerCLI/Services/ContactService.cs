using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using ContactManagerCLI.Models;
using ContactManagerCLI.Interfaces;

namespace ContactManagerCLI.Services
{
    public class ContactService
    {
        private readonly IContactRepository _repository;

        public ContactService(IContactRepository repository)
        {
            _repository = repository;
        }

        public (bool Success, string Error) ValidateContact(Contact contact)
        {
            if (string.IsNullOrWhiteSpace(contact.Name))
                return (false, "Name cannot be empty.");
            if (string.IsNullOrWhiteSpace(contact.Phone))
                return (false, "Phone cannot be empty.");
            if (!IsValidEmail(contact.Email))
                return (false, "Invalid email format.");
            return (true, string.Empty);
        }

        public void AddContact(Contact contact)
        {
            _repository.Add(contact);
        }

        public void UpdateContact(Contact contact)
        {
            _repository.Update(contact);
        }

        public void DeleteContact(Guid id)
        {
            _repository.Delete(id);
        }

        public Contact? GetContactById(Guid id)
        {
            return _repository.GetById(id);
        }

        public IEnumerable<Contact> GetAllContacts()
        {
            return _repository.GetAll();
        }

        public IEnumerable<Contact> SearchContactsByName(string name)
        {
            return _repository.SearchByName(name);
        }

        public IEnumerable<Contact> FilterContactsByDate(DateTime from, DateTime to)
        {
            return _repository.FilterByDate(from, to);
        }

        private bool IsValidEmail(string email)
        {
            if (string.IsNullOrWhiteSpace(email))
                return false;
            return Regex.IsMatch(email, @"^[^@\s]+@[^@\s]+\.[^@\s]+$");
        }

        public async System.Threading.Tasks.Task SaveAsync()
        {
            await _repository.SaveAsync();
        }

        public async System.Threading.Tasks.Task LoadAsync()
        {
            await _repository.LoadAsync();
        }
    }
}
