using System;
using System.Linq;
using System.Collections.Generic;
using MongoDB.Driver;
using MongoDB.Bson;
using System.Globalization;

namespace LibraryManagementSystem
{
    class Program
    {
        // MongoDB connection parameters
        const string MONGO_HOST = "localhost";
        const int MONGO_PORT = 27017;
        const string DATABASE_NAME = "Library";
        const string STAFF_DATABASE_NAME = "staff";

        // MongoDB client and database
        static MongoClient client;
        static IMongoDatabase db;
        static IMongoDatabase staffDb;

        // Collection names
        const string BOOKS_COLLECTION = "books";
        const string MEMBERS_COLLECTION = "members";
        const string TRANSACTIONS_COLLECTION = "transactions";

        static void Main(string[] args)
        {
            // Connect to MongoDB
            client = new MongoClient($"mongodb://{MONGO_HOST}:{MONGO_PORT}");
            db = client.GetDatabase(DATABASE_NAME);
            staffDb = client.GetDatabase(STAFF_DATABASE_NAME);

            // Call the main function
            MainFunction();
        }

        // Function to authenticate staff login
        static BsonDocument Login(string username, string password)
        {
            var staffCollection = staffDb.GetCollection<BsonDocument>("users");
            var filter = Builders<BsonDocument>.Filter.And(
                Builders<BsonDocument>.Filter.Eq("username", username),
                Builders<BsonDocument>.Filter.Eq("password", password)
            );
            var staff = staffCollection.Find(filter).FirstOrDefault();
            return staff;
        }

        // Function to edit a book's details (admin only)
        static void EditBook(string bookId)
        {
            var booksCollection = db.GetCollection<BsonDocument>(BOOKS_COLLECTION);
            var filter = Builders<BsonDocument>.Filter.Eq("_id", ObjectId.Parse(bookId));
            var book = booksCollection.Find(filter).FirstOrDefault();
            if (book != null)
            {
                Console.WriteLine("Current book details:");
                DisplayBooks(new List<BsonDocument> { book });  // Display current book details
                Console.WriteLine("Enter new details (leave blank to keep current):");
                Console.Write($"Title ({book["title"]}): ");
                var newTitle = Console.ReadLine();
                Console.Write($"Author ({book["author"]}): ");
                var newAuthor = Console.ReadLine();
                Console.Write($"Genre ({book["genre"]}): ");
                var newGenre = Console.ReadLine();
                Console.Write($"ISBN ({book["ISBN"]}): ");
                var newISBN = Console.ReadLine();
                Console.Write($"Publication Year ({book["publication_year"]}): ");
                var newPublicationYear = Console.ReadLine();
                Console.Write($"Quantity ({book["quantity"]}): ");
                var newQuantity = Console.ReadLine();

                // Update book details
                var update = Builders<BsonDocument>.Update
                    .Set("title", newTitle == "" ? book["title"] : newTitle)
                    .Set("author", newAuthor == "" ? book["author"] : newAuthor)
                    .Set("genre", newGenre == "" ? book["genre"] : newGenre)
                    .Set("ISBN", newISBN == "" ? book["ISBN"] : newISBN)
                    .Set("publication_year", newPublicationYear == "" ? book["publication_year"] : newPublicationYear)
                    .Set("quantity", newQuantity == "" ? book["quantity"] : int.Parse(newQuantity));
                booksCollection.UpdateOne(filter, update);
                Console.WriteLine("Book details updated successfully.");
            }
            else
            {
                Console.WriteLine("Invalid book ID.");
            }
        }

        // Function to get books based on staff permissions
        static List<BsonDocument> GetBooks(int staffPermission)
        {
            var booksCollection = db.GetCollection<BsonDocument>(BOOKS_COLLECTION);
            var books = new List<BsonDocument>();
            if (staffPermission == 1)  // Admin
            {
                books = booksCollection.Find(new BsonDocument()).ToList();
            }
            else if (staffPermission == 0)  // Teller
            {
                var filter = Builders<BsonDocument>.Filter.Gt("quantity", 0);
                books = booksCollection.Find(filter).ToList();
            }
            return books;
        }

        // Function to display available commands
        static void DisplayCommands()
        {
            Console.WriteLine("Available commands:");
            Console.WriteLine("0: Add a book");
            Console.WriteLine("1: Add a member");
            Console.WriteLine("2: Create a transaction");
            Console.WriteLine("3: View books");
            Console.WriteLine("4: View transactions");
            Console.WriteLine("5: View transaction history");
            Console.WriteLine("6: View members");
            Console.WriteLine("7: Edit a book's details");
            Console.WriteLine("8: Sign out");
        }

        // Function to view books
        static void ViewBooks()
        {
            Console.Write("Enter the subcommand number (0: All books, 1: Books owned, 2: Books in stock, 3: Books taken out, 4: Overdue books): ");
            var subcommand = Console.ReadLine();
            List<BsonDocument> books;
            switch (subcommand)
            {
                case "0":  // All books
                    books = db.GetCollection<BsonDocument>(BOOKS_COLLECTION).Find(new BsonDocument()).ToList();
                    break;
                case "1":  // Books owned
                    // Implement logic to filter books owned by the library
                    books = db.GetCollection<BsonDocument>(BOOKS_COLLECTION).Find(new BsonDocument()).ToList();
                    break;
                case "2":  // Books in stock
                    var filter = Builders<BsonDocument>.Filter.Gt("quantity", 0);
                    books = db.GetCollection<BsonDocument>(BOOKS_COLLECTION).Find(filter).ToList();
                    break;
                case "3":  // Books taken out
                    // Get books that are currently taken out
                    var transactions = db.GetCollection<BsonDocument>(TRANSACTIONS_COLLECTION)
                        .Find(Builders<BsonDocument>.Filter.Eq("actual_return_date", BsonNull.Value)).ToList();
                    var bookIds = transactions.Select(t => t["book_id"]).ToList();
                    var bookFilter = Builders<BsonDocument>.Filter.In("_id", bookIds);
                    books = db.GetCollection<BsonDocument>(BOOKS_COLLECTION).Find(bookFilter).ToList();
                    break;
                case "4":  // Overdue books
                    var today = DateTime.Now;
                    var overdueFilter = Builders<BsonDocument>.Filter.And(
                        Builders<BsonDocument>.Filter.Eq("actual_return_date", BsonNull.Value),
                        Builders<BsonDocument>.Filter.Lt("return_date", today)
                    );
                    var overdueTransactions = db.GetCollection<BsonDocument>(TRANSACTIONS_COLLECTION).Find(overdueFilter).ToList();
                    var overdueBookIds = overdueTransactions.Select(t => t["book_id"]).ToList();
                    var overdueBookFilter = Builders<BsonDocument>.Filter.In("_id", overdueBookIds);
                    books = db.GetCollection<BsonDocument>(BOOKS_COLLECTION).Find(overdueBookFilter).ToList();
                    break;
                default:
                    Console.WriteLine("Invalid subcommand. Please try again.");
                    return;
            }

            if (books.Any())
            {
                DisplayBooks(books);
            }
            else
            {
                Console.WriteLine("No books found.");
            }
        }

        // Function to display books in table format
        static void DisplayBooks(List<BsonDocument> books)
        {
            Console.WriteLine("Title\tAuthor\tGenre\tISBN\tPublication Year\tQuantity");
            foreach (var book in books)
            {
                Console.WriteLine($"{book["title"]}\t{book["author"]}\t{book["genre"]}\t{book["ISBN"]}\t{book["publication_year"]}\t{book["quantity"]}");
            }
        }

        // Function to view transactions
        static void ViewTransactions()
        {
            Console.Write("Enter the subcommand number (0: All transactions, 1: Borrowed transactions, 2: Returned transactions, 3: Overdue transactions): ");
            var subcommand = Console.ReadLine();
            List<BsonDocument> transactions;
            switch (subcommand)
            {
                case "0":  // All transactions
                    transactions = db.GetCollection<BsonDocument>(TRANSACTIONS_COLLECTION).Find(new BsonDocument()).ToList();
                    break;
                case "1":  // Borrowed transactions
                    var borrowedFilter = Builders<BsonDocument>.Filter.Eq("actual_return_date", BsonNull.Value);
                    transactions = db.GetCollection<BsonDocument>(TRANSACTIONS_COLLECTION).Find(borrowedFilter).ToList();
                    break;
                case "2":  // Returned transactions
                    var returnedFilter = Builders<BsonDocument>.Filter.Ne("actual_return_date", BsonNull.Value);
                    transactions = db.GetCollection<BsonDocument>(TRANSACTIONS_COLLECTION).Find(returnedFilter).ToList();
                    break;
                case "3":  // Overdue transactions
                    var today = DateTime.Now;
                    var overdueFilter = Builders<BsonDocument>.Filter.And(
                        Builders<BsonDocument>.Filter.Eq("actual_return_date", BsonNull.Value),
                        Builders<BsonDocument>.Filter.Lt("return_date", today)
                    );
                    transactions = db.GetCollection<BsonDocument>(TRANSACTIONS_COLLECTION).Find(overdueFilter).ToList();
                    break;
                default:
                    Console.WriteLine("Invalid subcommand. Please try again.");
                    return;
            }

            if (transactions.Any())
            {
                DisplayTransactions(transactions);
            }
            else
            {
                Console.WriteLine("No transactions found.");
            }
        }

        // Function to display transactions in table format
        static void DisplayTransactions(List<BsonDocument> transactions)
        {
            Console.WriteLine("Member ID\tBook ID\tTransaction Type\tBorrow Date\tReturn Date\tActual Return Date");
            foreach (var transaction in transactions)
            {
                Console.WriteLine($"{transaction["member_id"]}\t{transaction["book_id"]}\t{transaction["transaction_type"]}\t{transaction["borrow_date"]}\t{transaction["return_date"]}\t{transaction["actual_return_date"]}");
            }
        }

        // Function to view members
        static void ViewMembers()
        {
            Console.Write("Enter the subcommand number (0: All members, 1: Active members, 2: Inactive members): ");
            var subcommand = Console.ReadLine();
            List<BsonDocument> members;
            switch (subcommand)
            {
                case "0":  // All members
                    members = db.GetCollection<BsonDocument>(MEMBERS_COLLECTION).Find(new BsonDocument()).ToList();
                    break;
                case "1":  // Active members
                    var activeFilter = Builders<BsonDocument>.Filter.Eq("status", "Active");
                    members = db.GetCollection<BsonDocument>(MEMBERS_COLLECTION).Find(activeFilter).ToList();
                    break;
                case "2":  // Inactive members
                    var inactiveFilter = Builders<BsonDocument>.Filter.Eq("status", "Inactive");
                    members = db.GetCollection<BsonDocument>(MEMBERS_COLLECTION).Find(inactiveFilter).ToList();
                    break;
                default:
                    Console.WriteLine("Invalid subcommand. Please try again.");
                    return;
            }

            if (members.Any())
            {
                DisplayMembers(members);
            }
            else
            {
                Console.WriteLine("No members found.");
            }
        }

        // Function to display members in table format
        static void DisplayMembers(List<BsonDocument> members)
        {
            Console.WriteLine("Name\tEmail\tPhone\tAddress\tMembership ID\tStatus");
            foreach (var member in members)
            {
                Console.WriteLine($"{member["name"]}\t{member["email"]}\t{member["phone"]}\t{member["address"]}\t{member["membership_id"]}\t{member["status"]}");
            }
        }

        // Function to add a new member (admin only)
        static void AddMember(string name, string email, string phone, string address, string membershipId)
        {
            var membersCollection = db.GetCollection<BsonDocument>(MEMBERS_COLLECTION);
            var member = new BsonDocument
            {
                { "name", name },
                { "email", email },
                { "phone", phone },
                { "address", address },
                { "membership_id", membershipId },
                { "status", "Active" }
            };
            membersCollection.InsertOne(member);
            Console.WriteLine("New member added successfully.");
        }

        // Function to add a new book (admin only)
        static void AddBook(string title, string author, string genre, string ISBN, string publicationYear, int quantity)
        {
            var booksCollection = db.GetCollection<BsonDocument>(BOOKS_COLLECTION);
            var book = new BsonDocument
            {
                { "title", title },
                { "author", author },
                { "genre", genre },
                { "ISBN", ISBN },
                { "publication_year", publicationYear },
                { "quantity", quantity }
            };
            booksCollection.InsertOne(book);
            Console.WriteLine("New book added successfully.");
        }

        // Function to create a new transaction (teller only)
        static void CreateTransaction(string memberId, string bookId)
        {
            var membersCollection = db.GetCollection<BsonDocument>(MEMBERS_COLLECTION);
            var booksCollection = db.GetCollection<BsonDocument>(BOOKS_COLLECTION);
            var transactionsCollection = db.GetCollection<BsonDocument>(TRANSACTIONS_COLLECTION);
            
            var memberFilter = Builders<BsonDocument>.Filter.Eq("_id", ObjectId.Parse(memberId));
            var bookFilter = Builders<BsonDocument>.Filter.Eq("_id", ObjectId.Parse(bookId));
            var member = membersCollection.Find(memberFilter).FirstOrDefault();
            var book = booksCollection.Find(bookFilter).FirstOrDefault();

            if (member != null && book != null)
            {
                var today = DateTime.Now;
                var returnDate = today.AddDays(14);  // Return date is 14 days from today
                var transaction = new BsonDocument
                {
                    { "member_id", ObjectId.Parse(memberId) },
                    { "book_id", ObjectId.Parse(bookId) },
                    { "transaction_type", "Borrow" },
                    { "borrow_date", today },
                    { "return_date", returnDate },
                    { "actual_return_date", BsonNull.Value }
                };
                transactionsCollection.InsertOne(transaction);
                Console.WriteLine("Transaction created successfully.");
            }
            else
            {
                Console.WriteLine("Invalid member ID or book ID.");
            }
        }

        // Function to view transaction history
        static void ViewTransactionHistory()
        {
            var transactions = db.GetCollection<BsonDocument>(TRANSACTIONS_COLLECTION)
                .Find(new BsonDocument())
                .Sort(Builders<BsonDocument>.Sort.Descending("borrow_date"))
                .ToList();

            if (transactions.Any())
            {
                DisplayTransactions(transactions);
            }
            else
            {
                Console.WriteLine("No transaction history found.");
            }
        }

        // Main function
        static void MainFunction()
        {
            Console.WriteLine("Welcome to the Library Management System!");
            Console.Write("Enter your username: ");
            var username = Console.ReadLine();
            Console.Write("Enter your password: ");
            var password = Console.ReadLine();

            // Authenticate staff login
            var staff = Login(username, password);
            if (staff != null)
            {
                Console.WriteLine($"Welcome, {staff["name"]}!");
                var staffPermission = staff["permission"].AsInt32;  // Get staff permission level

                // Continue running commands until sign out
                while (true)
                {
                    DisplayCommands();
                    Console.Write("Enter the command number: ");
                    var command = Console.ReadLine();

                    switch (command)
                    {
                        case "0":  // Add a book
                            if (staffPermission == 1)  // Admin only
                            {
                                Console.Write("Enter the title of the book: ");
                                var title = Console.ReadLine();
                                Console.Write("Enter the author of the book: ");
                                var author = Console.ReadLine();
                                Console.Write("Enter the genre of the book: ");
                                var genre = Console.ReadLine();
                                Console.Write("Enter the ISBN of the book: ");
                                var ISBN = Console.ReadLine();
                                Console.Write("Enter the publication year of the book: ");
                                var publicationYear = Console.ReadLine();
                                Console.Write("Enter the quantity of the book: ");
                                var quantity = int.Parse(Console.ReadLine());
                                AddBook(title, author, genre, ISBN, publicationYear, quantity);
                            }
                            else
                            {
                                Console.WriteLine("You don't have permission to add a book.");
                            }
                            break;

                        case "1":  // Add a member
                            if (staffPermission == 1)  // Admin only
                            {
                                Console.Write("Enter the name of the member: ");
                                var name = Console.ReadLine();
                                Console.Write("Enter the email of the member: ");
                                var email = Console.ReadLine();
                                Console.Write("Enter the phone number of the member: ");
                                var phone = Console.ReadLine();
                                Console.Write("Enter the address of the member: ");
                                var address = Console.ReadLine();
                                Console.Write("Enter the membership ID of the member: ");
                                var membershipId = Console.ReadLine();
                                AddMember(name, email, phone, address, membershipId);
                            }
                            else
                            {
                                Console.WriteLine("You don't have permission to add a member.");
                            }
                            break;

                        case "2":  // Create a transaction
                            if (staffPermission == 0)  // Teller only
                            {
                                Console.Write("Enter the member ID: ");
                                var memberId = Console.ReadLine();
                                Console.Write("Enter the book ID: ");
                                var bookId = Console.ReadLine();
                                CreateTransaction(memberId, bookId);
                            }
                            else
                            {
                                Console.WriteLine("You don't have permission to create a transaction.");
                            }
                            break;

                        case "3":  // View books
                            ViewBooks();
                            break;

                        case "4":  // View transactions
                            ViewTransactions();
                            break;

                        case "5":  // View transaction history
                            ViewTransactionHistory();
                            break;

                        case "6":  // View members
                            ViewMembers();
                            break;

                        case "7":  // Edit a book's details
                            if (staffPermission == 1)  // Admin only
                            {
                                Console.Write("Enter the book ID you want to edit: ");
                                var bookId = Console.ReadLine();
                                EditBook(bookId);
                            }
                            else
                            {
                                Console.WriteLine("You don't have permission to edit a book's details.");
                            }
                            break;

                        case "8":  // Sign out
                            Console.WriteLine("Signing out...");
                            return;

                        default:
                            Console.WriteLine("Invalid command. Please try again.");
                            break;
                    }
                }
            }
            else
            {
                Console.WriteLine("Invalid username or password. Please try again.");
            }
        }
    }
}
