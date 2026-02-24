using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using ContactManagerCLI.Models;
using ContactManagerCLI.Interfaces;

namespace ContactManagerCLI.Repositories
{
    public class JsonContactRepository : IContactRepository
    {
        private readonly string _directoryPath = "contacts";

        private readonly Dictionary<Guid, Contact> _contacts = new();

        private readonly Trie _nameTrie = new();

        private readonly ReaderWriterLockSlim _lock = new();

        #region CRUD

        public void Add(Contact contact)
        {
            _lock.EnterWriteLock();
            try
            {
                _contacts[contact.Id] = contact;
                IndexName(contact);
                // Removed immediate serialization. Save only on SaveAsync.
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
                // Removed immediate serialization. Save only on SaveAsync.
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
                    // Removed immediate file deletion. Save only on SaveAsync.
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
            if (_contacts.Count == 0)
            {
                LoadAllFromDisk();
            }

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
            if (string.IsNullOrWhiteSpace(name))
                return Array.Empty<Contact>();

            if (_contacts.Count == 0)
            {
                LoadAllFromDisk();
            }

            _lock.EnterReadLock();
            try
            {
                var result = new List<Contact>();
                var ids = _nameTrie.GetIdsByPrefix(name);

                foreach (var id in ids)
                {
                    if (_contacts.TryGetValue(id, out var contact))
                        result.Add(contact);
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
                Directory.CreateDirectory(_directoryPath);

                var options = new JsonSerializerOptions
                {
                    WriteIndented = true
                };

                var tasks = new List<Task>();

                foreach (var contact in _contacts.Values)
                {
                    var path = Path.Combine(_directoryPath, $"{contact.Id}.json");
                    var json = JsonSerializer.Serialize(contact, options);
                    tasks.Add(File.WriteAllTextAsync(path, json));
                }

                await Task.WhenAll(tasks);
            }
            finally
            {
                _lock.ExitReadLock();
            }
        }

        public async Task LoadAsync()
        {
            if (!Directory.Exists(_directoryPath))
                return;

            _lock.EnterWriteLock();
            try
            {
                _contacts.Clear();
                _nameTrie.Clear();

                var files = Directory.EnumerateFiles(_directoryPath, "*.json");

                foreach (var file in files)
                {
                    try
                    {
                        using var stream = new FileStream(
                            file,
                            FileMode.Open,
                            FileAccess.Read,
                            FileShare.Read,
                            4096,
                            useAsync: true);

                        var contact = await JsonSerializer.DeserializeAsync<Contact>(stream);

                        if (contact == null)
                            continue;

                        _contacts[contact.Id] = contact;
                        IndexName(contact);
                    }
                    catch
                    {
                    }
                }
            }
            finally
            {
                if (_lock.IsWriteLockHeld)
                    _lock.ExitWriteLock();
            }
        }
        #endregion

        #region Pagination

        public IEnumerable<Contact> GetPage(int page, int pageSize)
        {
            _lock.EnterReadLock();
            try
            {
                if (_contacts.Count > 0)
                {
                    return _contacts.Values
                        .OrderBy(c => c.CreatedAt)
                        .Skip((page - 1) * pageSize)
                        .Take(pageSize)
                        .ToList();
                }

                if (!Directory.Exists(_directoryPath))
                    return new List<Contact>();

                var files = Directory.EnumerateFiles(_directoryPath, "*.json")
                    .OrderBy(f => f)
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .ToList();

                var results = new List<Contact>();

                foreach (var file in files)
                {
                    try
                    {
                        var text = File.ReadAllText(file);
                        var contact = JsonSerializer.Deserialize<Contact>(text);
                        if (contact != null)
                            results.Add(contact);
                    }
                    catch
                    {
                    }
                }

                return results;
            }
            finally
            {
                _lock.ExitReadLock();
            }
        }

        public int GetTotalCount()
        {
            _lock.EnterReadLock();
            try
            {
                if (_contacts.Count > 0)
                    return _contacts.Count;

                if (!Directory.Exists(_directoryPath))
                    return 0;

                try
                {
                    return Directory.EnumerateFiles(_directoryPath, "*.json").Count();
                }
                catch
                {
                    return 0;
                }
            }
            finally
            {
                _lock.ExitReadLock();
            }
        }

        #endregion

        #region Index Helpers

        private void IndexName(Contact contact)
        {
            if (string.IsNullOrWhiteSpace(contact.Name))
                return;

            _nameTrie.Add(contact.Name, contact.Id);
        }

        private void RemoveNameIndex(Contact contact)
        {
            if (string.IsNullOrWhiteSpace(contact.Name))
                return;

            _nameTrie.Remove(contact.Name, contact.Id);
        }

        #endregion

        private class Trie
        {
            private class Node
            {
                public Dictionary<char, Node> Children { get; } = new();
                public HashSet<Guid> Ids { get; } = new();
            }

            private readonly Node _root = new();

            public void Add(string key, Guid id)
            {
                if (string.IsNullOrWhiteSpace(key))
                    return;

                var s = key.ToLowerInvariant();
                var node = _root;
                node.Ids.Add(id);

                foreach (var ch in s)
                {
                    if (!node.Children.TryGetValue(ch, out var next))
                    {
                        next = new Node();
                        node.Children[ch] = next;
                    }

                    node = next;
                    node.Ids.Add(id);
                }
            }

            public void Remove(string key, Guid id)
            {
                if (string.IsNullOrWhiteSpace(key))
                    return;

                var s = key.ToLowerInvariant();
                var node = _root;
                var stack = new Stack<(Node node, char ch)>();

                node.Ids.Remove(id);

                foreach (var ch in s)
                {
                    if (!node.Children.TryGetValue(ch, out var next))
                        return; 

                    stack.Push((node, ch));
                    node = next;
                    node.Ids.Remove(id);
                }

                while (stack.Count > 0)
                {
                    var (parent, ch) = stack.Pop();
                    if (parent.Children.TryGetValue(ch, out var child))
                    {
                        if (child.Ids.Count == 0 && child.Children.Count == 0)
                        {
                            parent.Children.Remove(ch);
                        }
                        else
                        {
                            break;
                        }
                    }
                }
            }

            public IEnumerable<Guid> GetIdsByPrefix(string prefix)
            {
                if (string.IsNullOrWhiteSpace(prefix))
                    return Array.Empty<Guid>();

                var s = prefix.ToLowerInvariant();
                var node = _root;

                foreach (var ch in s)
                {
                    if (!node.Children.TryGetValue(ch, out var next))
                        return Array.Empty<Guid>();

                    node = next;
                }

                return node.Ids;
            }

            public void Clear()
            {
                _root.Children.Clear();
                _root.Ids.Clear();
            }
        }

        private void LoadAllFromDisk()
        {
            if (!Directory.Exists(_directoryPath))
                return;

            _lock.EnterWriteLock();
            try
            {
                _contacts.Clear();
                _nameTrie.Clear();

                var files = Directory.EnumerateFiles(_directoryPath, "*.json");
                foreach (var file in files)
                {
                    try
                    {
                        var text = File.ReadAllText(file);
                        var contact = JsonSerializer.Deserialize<Contact>(text);
                        if (contact == null)
                            continue;

                        _contacts[contact.Id] = contact;
                        IndexName(contact);
                    }
                    catch
                    {
                    }
                }
            }
            finally
            {
                _lock.ExitWriteLock();
            }
        }
    }
}