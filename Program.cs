using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;

namespace HumanApp
{
    class Program
    {
        static List<Human> users = new List<Human>();
        static string filePath = "users.json";

        static void Main()
        {
            LoadUsers();

            while (true)
            {
                Console.Clear();
                Console.WriteLine("==== Main Menu ====");
                Console.WriteLine("1. Add User");
                Console.WriteLine("2. View User");
                Console.WriteLine("3. Exit");
                Console.Write("Choose an option: ");
                var key = Console.ReadKey(true);

                if (key.Key == ConsoleKey.D1)
                {
                    AddUser();
                }
                else if (key.Key == ConsoleKey.D2)
                {
                    ViewUser();
                }
                else if (key.Key == ConsoleKey.D3)
                {
                    SaveUsers();
                    break;
                }
            }
        }

        static void AddUser()
        {
            Console.Clear();
            Console.WriteLine("Select User Type:");
            Console.WriteLine("1. Student");
            Console.WriteLine("2. Worker");
            Console.Write("Choice: ");
            var key = Console.ReadKey(true);

            Console.Write("\nEnter First Name: ");
            string firstName = Console.ReadLine();
            Console.Write("Enter Last Name: ");
            string lastName = Console.ReadLine();

            if (key.Key == ConsoleKey.D1) // Student
            {
                var student = new Student(firstName, lastName);
                student.EnterGrades();
                users.Add(student);
            }
            else if (key.Key == ConsoleKey.D2) // Worker
            {
                decimal wage = GetDecimal("Enter total wage: ");
                int hours = GetInt("Enter hours worked: ");
                var worker = new Worker(firstName, lastName, wage, hours);
                users.Add(worker);
            } 
            else
            {
                Console.WriteLine("Invalid input");
            }

            SaveUsers();
            Console.WriteLine("\nUser added successfully!");
            Console.ReadKey();
        }

        static void ViewUser()
        {
            Console.Clear();
            Console.Write("Enter name to search: ");
            string search = Console.ReadLine().ToLower();

            var matches = users.Where(u =>
                u.FirstName.ToLower().Contains(search) ||
                u.LastName.ToLower().Contains(search))
                .ToList();

            if (matches.Count == 0)
            {
                Console.WriteLine("No users found.");
                Console.ReadKey();
                return;
            }

            Console.WriteLine("\nSelect a user:");
            for (int i = 0; i < matches.Count; i++)
            {
                Console.WriteLine($"{i + 1}. {matches[i].FirstName} {matches[i].LastName}");
            }

            Console.Write(": ");
            string option = Console.ReadLine();
            if (int.TryParse(option, out int choice) && choice >= 1 && choice <= matches.Count)
            {
                Console.Clear();
                var user = matches[choice - 1];
                Console.WriteLine(user);

                if (user is Student student)
                {
                    Console.WriteLine($"GPA: {student.CalculateGPA():F2}");
                }
                else if (user is Worker worker)
                {
                    Console.WriteLine($"Hourly Wage: ${worker.CalculateHourlyWage():C}");
                }
            }
            else
            {
                Console.WriteLine("Invalid choice.");
            }

            Console.ReadKey();
        }

        static void SaveUsers()
        {
            string json = JsonSerializer.Serialize(users, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(filePath, json);
        }

        static void LoadUsers()
        {
            if (File.Exists(filePath))
            {
                string json = File.ReadAllText(filePath);
                var options = new JsonSerializerOptions
                {
                    Converters = { new HumanConverter() }
                };
                users = JsonSerializer.Deserialize<List<Human>>(json, options) ?? new List<Human>();
            }
        }

        static int GetInt(string message)
        {
            int value;
            while (true)
            {
                Console.Write(message);
                if (int.TryParse(Console.ReadLine(), out value)) break;
                Console.WriteLine("Invalid number. Try again.");
            }
            return value;
        }

        static decimal GetDecimal(string message)
        {
            decimal value;
            while (true)
            {
                Console.Write(message);
                if (decimal.TryParse(Console.ReadLine(), out value)) break;
                Console.WriteLine("Invalid number. Try again.");
            }
            return value;
        }
    }

    public class Human
    {
        public string FirstName { get; set; }
        public string LastName { get; set; }

        public Human() { }
        public Human(string firstName, string lastName)
        {
            FirstName = firstName;
            LastName = lastName;
        }

        public override string ToString()
        {
            return $"Name: {FirstName} {LastName}";
        }
    }

    public class Student : Human
    {
        private List<double> Grades { get; set; } = new List<double>();

        public Student() { }
        public Student(string firstName, string lastName) : base(firstName, lastName) { }

        public void EnterGrades()
        {
            Console.WriteLine("Enter grades (type 'done' to finish):");
            while (true)
            {
                Console.Write("Grade: ");
                string input = Console.ReadLine();
                if (input.ToLower() == "done") break;

                if (double.TryParse(input, out double grade))
                {
                    Grades.Add(grade);
                }
                else
                {
                    Console.WriteLine("Invalid grade.");
                }
            }
        }

        public double CalculateGPA()
        {
            if (Grades.Count == 0) return 0;
            return Grades.Average();
        }
    }

    public class Worker : Human
    {
        public decimal Wage { get; set; }
        public int HoursWorked { get; set; }

        public Worker() { }
        public Worker(string firstName, string lastName, decimal wage, int hoursWorked)
            : base(firstName, lastName)
        {
            Wage = wage;
            HoursWorked = hoursWorked;
        }

        public decimal CalculateHourlyWage()
        {
            if (HoursWorked <= 0) return 0;
            return Wage / HoursWorked;
        }
    }

    // JSON converter so Student/Worker can deserialize correctly
    public class HumanConverter : System.Text.Json.Serialization.JsonConverter<Human>
    {
        public override Human Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            using (var doc = JsonDocument.ParseValue(ref reader))
            {
                var root = doc.RootElement;

                var noConverterOptions = new JsonSerializerOptions(); // avoid recursion

                if (root.TryGetProperty("Grades", out _))
                    return JsonSerializer.Deserialize<Student>(root.GetRawText(), noConverterOptions);
                else if (root.TryGetProperty("Wage", out _))
                    return JsonSerializer.Deserialize<Worker>(root.GetRawText(), noConverterOptions);
                else
                    return JsonSerializer.Deserialize<Human>(root.GetRawText(), noConverterOptions);
            }
        }

        public override void Write(Utf8JsonWriter writer, Human value, JsonSerializerOptions options)
        {
            if (value is Student student)
                JsonSerializer.Serialize(writer, student, options);
            else if (value is Worker worker)
                JsonSerializer.Serialize(writer, worker, options);
            else
                JsonSerializer.Serialize(writer, value, options);
        }
    }

}
