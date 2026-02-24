using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using ContactManagerCLI.Models;
using ContactManagerCLI.Interfaces;

namespace ContactManagerCLI.Repositories
{
    public class JsonContactRepository : IContactRepository
    {
        private readonly string _filePath = "contacts.json";

        private readonly Dictionary<Guid, Contact> _contacts = new();
        private readonly Dictionary<string, List<Guid>> _nameIndex =
            new(StringComparer.OrdinalIgnoreCase);

        private readonly ReaderWriterLockSlim _lock = new();

        #region CRUD

        public void Add(Contact contact)
        {
            _lock.EnterWriteLock();
            try
            {
                _contacts[contact.Id] = contact;
                IndexName(contact);
            }
            finally
            {
                _lock.ExitWriteLock();
            }
        }

        public void Update(Contact contact)
        {
            _lock.EnterWriteLock();
            try
            {
                if (_contacts.TryGetValue(contact.Id, out var oldContact))
                {
                    RemoveNameIndex(oldContact);
                }

                _contacts[contact.Id] = contact;
                IndexName(contact);
            }
            finally
            {
                _lock.ExitWriteLock();
            }
        }

        public void Delete(Guid id)
        {
            _lock.EnterWriteLock();
            try
            {
                if (_contacts.TryGetValue(id, out var contact))
                {
                    RemoveNameIndex(contact);
                    _contacts.Remove(id);
                }
            }
            finally
            {
                _lock.ExitWriteLock();
            }
        }

        public Contact? GetById(Guid id)
        {
            _lock.EnterReadLock();
            try
            {
                _contacts.TryGetValue(id, out var contact);
                return contact;
            }
            finally
            {
                _lock.ExitReadLock();
            }
        }

        public IEnumerable<Contact> GetAll()
        {
            _lock.EnterReadLock();
            try
            {
                return new List<Contact>(_contacts.Values);
            }
            finally
            {
                _lock.ExitReadLock();
            }
        }

        #endregion

        #region Search & Filter

        public IEnumerable<Contact> SearchByName(string name)
        {
            _lock.EnterReadLock();
            try
            {
                var result = new List<Contact>();

                if (_nameIndex.TryGetValue(name, out var ids))
                {
                    foreach (var id in ids)
                    {
                        if (_contacts.TryGetValue(id, out var contact))
                            result.Add(contact);
                    }
                }

                return result;
            }
            finally
            {
                _lock.ExitReadLock();
            }
        }

        public IEnumerable<Contact> FilterByDate(DateTime from, DateTime to)
        {
            _lock.EnterReadLock();
            try
            {
                var result = new List<Contact>();

                foreach (var contact in _contacts.Values)
                {
                    if (contact.CreatedAt >= from &&
                        contact.CreatedAt <= to)
                    {
                        result.Add(contact);
                    }
                }

                return result;
            }
            finally
            {
                _lock.ExitReadLock();
            }
        }

        #endregion

        #region Persistence

        public async Task SaveAsync()
        {
            _lock.EnterReadLock();
            try
            {
                var options = new JsonSerializerOptions
                {
                    WriteIndented = true
                };

                using var stream = new FileStream(
                    _filePath,
                    FileMode.Create,
                    FileAccess.Write,
                    FileShare.None,
                    4096,
                    useAsync: true);

                await JsonSerializer.SerializeAsync(
                    stream,
                    _contacts.Values,
                    options);
            }
            finally
            {
                _lock.ExitReadLock();
            }
        }

        public async Task LoadAsync()
        {
            if (!File.Exists(_filePath))
                return;

            _lock.EnterWriteLock();
            try
            {
                _contacts.Clear();
                _nameIndex.Clear();

                using var stream = new FileStream(
                    _filePath,
                    FileMode.Open,
                    FileAccess.Read,
                    FileShare.Read,
                    4096,
                    useAsync: true);

                var contacts =
                    await JsonSerializer.DeserializeAsync<List<Contact>>(stream);

                if (contacts == null)
                    return;

                foreach (var contact in contacts)
                {
                    _contacts[contact.Id] = contact;
                    IndexName(contact);
                }
            }
            finally
            {
                if (_lock.IsWriteLockHeld)
                    _lock.ExitWriteLock();
            }
        }
        #endregion

        #region Index Helpers

        private void IndexName(Contact contact)
        {
            if (!_nameIndex.TryGetValue(contact.Name, out var list))
            {
                list = new List<Guid>();
                _nameIndex[contact.Name] = list;
            }

            if (!list.Contains(contact.Id))
                list.Add(contact.Id);
        }

        private void RemoveNameIndex(Contact contact)
        {
            if (_nameIndex.TryGetValue(contact.Name, out var list))
            {
                list.Remove(contact.Id);

                if (list.Count == 0)
                    _nameIndex.Remove(contact.Name);
            }
        }

        #endregion
    }
}