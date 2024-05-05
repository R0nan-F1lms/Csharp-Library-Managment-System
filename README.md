# Library Management System

This is a simple Library Management System implemented in C# using MongoDB as the database.

## Setup

1. **Install MongoDB**: If you haven't already, [install MongoDB](https://docs.mongodb.com/manual/installation/) on your machine.

2. **Clone the Repository**: Clone this repository to your local machine using Git:
```bash
git clone <repository_url>
```

3. **Restore NuGet Packages**: Open the solution in Visual Studio and restore the NuGet packages for the solution.

4. **Database Configuration**:

- MongoDB Connection String: Ensure that your MongoDB server is running. Update the `connectionString` variable in the `Main` method of the `Program` class in `Program.cs` file with the appropriate connection string.

- Database Name: Update the `databaseName`, `booksCollectionName`, `membersCollectionName`, and `transactionsCollectionName` variables in the `Main` method with the names of your MongoDB database and collections.

5. **Build and Run**: Build the solution in Visual Studio and run the application.

## Usage

- When you run the application, you'll be prompted to enter your username and password to log in.
- Once logged in, you'll see a list of available commands.
- Enter the command number to execute the desired action.

### Available Commands:

0: Add a book
1: Add a member
2: Create a transaction
3: View books
4: View transactions
5: View transaction history
6: View members
7: Edit a book's details
8: Sign out

### Adding a Book (Command 0):

- To add a book, select command 0.
- Enter the details of the new book as prompted: title, author, genre, ISBN, publication year, and quantity.
- The new book will be added to the database.

## Contributing

Contributions are welcome! If you find any issues or have suggestions for improvements, feel free to open an issue or submit a pull request.

## License

This project is licensed under the [MIT License](LICENSE).
