# 📇 Contact Manager CLI

A thread-safe Contact Management System built with C# (.NET), using JSON file-based persistence and a Trie-based name index for fast prefix searching.

---

## 🚀 Features

- ✅ Add, Update, Delete Contacts (CRUD)
- 🔍 Fast prefix-based search using Trie data structure
- 📅 Filter contacts by creation date range
- 📄 Pagination support
- 💾 JSON file-based persistence (one file per contact)
- 🔐 Thread-safe operations using ReaderWriterLockSlim
- ⚡ Asynchronous file I/O operations
- 🧠 In-memory caching with lazy disk loading

---

## 🏗️ Architecture Overview

The project follows a clean layered structure:

```
ContactManagerCLI
│
├── Models
│   └── Contact.cs
│
├── Interfaces
│   └── IContactRepository.cs
│
├── Repositories
│   └── JsonContactRepository.cs
│
└── Program.cs
```

### Repository Pattern

The system uses an abstraction (`IContactRepository`) and a concrete implementation (`JsonContactRepository`), allowing future extension (e.g., database repository).

---

## 📦 JsonContactRepository

Main responsibilities:

- Stores contacts in-memory using:
  ```
  Dictionary<Guid, Contact>
  ```

- Maintains a Trie index for name-based prefix search
- Persists each contact as a separate JSON file
- Supports thread-safe concurrent operations
- Lazy-loads data from disk when needed

---

## 🔎 Search Optimization (Trie Implementation)

A custom Trie data structure is used to enable fast prefix search.

### Why Trie?

- Prefix search complexity: **O(k)**
- Efficient for large datasets
- Supports dynamic insert and delete

Where:
- `k` = length of the search prefix

### Supported Operations

- Add name to index
- Remove name from index
- Get all contact IDs matching a prefix

---

## 💾 Persistence Strategy

Each contact is saved as:

```
contacts/{contactId}.json
```

### Save

- Uses `System.Text.Json`
- Uses async file writing
- Writes all contacts in parallel using `Task.WhenAll`

### Load

- Async deserialization
- Rebuilds in-memory dictionary
- Rebuilds Trie index

---

## 📄 Pagination

```csharp
GetPage(int page, int pageSize)
```

- Orders by `CreatedAt`
- Uses `Skip` and `Take`
- Supports disk-based fallback if memory is empty

---

## 🔐 Thread Safety

All operations are protected using:

```
ReaderWriterLockSlim
```

### Strategy

- Multiple concurrent readers allowed
- Exclusive writer lock for Add/Update/Delete
- Prevents race conditions
- Ensures data consistency

---

## 🛠 Technologies Used

- .NET
- C#
- System.Text.Json
- ReaderWriterLockSlim
- Custom Trie Data Structure
- LINQ

---

## ▶️ How To Run

### 1️⃣ Clone the repository

```bash
git clone https://github.com/yourusername/ContactManagerCLI.git
```

### 2️⃣ Navigate to project

```bash
cd ContactManagerCLI
```

### 3️⃣ Run the application

```bash
dotnet run
```

---

## 📊 Performance Notes

| Operation            | Complexity |
|----------------------|------------|
| Add Contact          | O(k)       |
| Update Contact       | O(k)       |
| Delete Contact       | O(k)       |
| Search By Prefix     | O(k)       |
| Pagination           | O(n log n) (due to ordering) |

Where `k` = length of name  
Where `n` = number of contacts

---

## 📜 License

This project is built for educational purposes and learning advanced data structures and concurrency in .NET.

---

## 👨‍💻 Author

Built as a CLI-based Contact Manager to practice:

- Data structures
- File persistence
- Concurrency control
- Repository pattern
- Clean architecture principles

---

⭐ If you find this project useful, feel free to star the repository.
