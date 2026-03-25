using System.Text.Json;

namespace Lab3.Services;

public class UserItem
{
    public string Name { get; set; } = "";
    public string Surname { get; set; } = "";
    public string Email { get; set; } = "";
    public string Address { get; set; } = "";
    public string PhotoPath { get; set; } = "";

    public string Initials
    {
        get
        {
            var firstName = string.IsNullOrWhiteSpace(Name) ? "" : Name.Substring(0, 1);
            var firstSurname = string.IsNullOrWhiteSpace(Surname) ? "" : Surname.Substring(0, 1);
            return (firstName + firstSurname).ToUpper();
        }
    }
}

public static class UserStorage
{
    public static List<UserItem> GetUsers(string filePath)
    {
        if (!File.Exists(filePath))
        {
            return new List<UserItem>();
        }

        var json = File.ReadAllText(filePath);

        if (string.IsNullOrWhiteSpace(json))
        {
            return new List<UserItem>();
        }

        var users = JsonSerializer.Deserialize<List<UserItem>>(json);

        return users ?? new List<UserItem>();
    }

    public static void SaveUsers(string filePath, List<UserItem> users)
    {
        var directory = Path.GetDirectoryName(filePath);

        if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
        {
            Directory.CreateDirectory(directory);
        }

        var json = JsonSerializer.Serialize(users, new JsonSerializerOptions
        {
            WriteIndented = true
        });

        File.WriteAllText(filePath, json);
    }

    public static void AddUser(string filePath, UserItem newUser)
    {
        var users = GetUsers(filePath);
        users.Add(newUser);
        SaveUsers(filePath, users);
    }
}