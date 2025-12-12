#nullable enable
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;

namespace TaskTrackerSystem
{
    // ==========================================
    // ENUMS
    // ==========================================
    public enum Priority { Low, Medium, High }
    public enum Status { ToDo, InProgress, Done }

    // ==========================================
    // UI HELPER (Makes the Console look like a GUI)
    // ==========================================
    public static class UI
    {
        public static void Header(string title)
        {
            Console.Clear();
            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.WriteLine("========================================");
            Console.WriteLine($"   {title.ToUpper()}");
            Console.WriteLine("========================================");
            Console.ResetColor();
            Console.WriteLine();
        }

        public static void PrintSuccess(string msg)
        {
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine($"[SUCCESS] {msg}");
            Console.ResetColor();
            Console.WriteLine("Press Enter to continue...");
            Console.ReadLine();
        }

        public static void PrintError(string msg)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($"[ERROR] {msg}");
            Console.ResetColor();
            Console.WriteLine("Press Enter to continue...");
            Console.ReadLine();
        }
    }

    // ==========================================
    // INTERFACE
    // ==========================================
    public interface ITaskOperation
    {
        string GetDetails();
    }

    // ==========================================
    // SINGLETON LOGGER
    // ==========================================
    public sealed class Logger
    {
        private static Logger? _instance = null;
        private static readonly object _padlock = new object();
        private string _logFilePath = "activity_log.txt";

        Logger() { }

        public static Logger Instance
        {
            get
            {
                lock (_padlock)
                {
                    if (_instance == null) _instance = new Logger();
                    return _instance;
                }
            }
        }

        public void Log(string message)
        {
            try
            {
                string logEntry = $"{DateTime.Now}: {message}";
                File.AppendAllText(_logFilePath, logEntry + Environment.NewLine);
            }
            catch { /* Ignore logging errors */ }
        }
    }

    // ==========================================
    // MODELS
    // ==========================================
    public abstract class BaseTask
    {
        public int Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public DateTime DueDate { get; set; }
    }

    public class ProjectTask : BaseTask, ITaskOperation
    {
        public string Assignee { get; set; } = string.Empty;
        public Priority TaskPriority { get; set; }
        public Status TaskStatus { get; set; }

        public string GetDetails()
        {
            // Formatting for a table-like row
            return string.Format("| {0,-3} | {1,-20} | {2,-10} | {3,-10} | {4,-12} |", 
                Id, 
                Title.Length > 20 ? Title.Substring(0,17)+"..." : Title, 
                TaskPriority, 
                TaskStatus, 
                DueDate.ToShortDateString());
        }
    }

    // ==========================================
    // FILE SERVICE
    // ==========================================
    public static class FileService
    {
        private static string _filePath = "tasks.json";

        public static void SaveTasks(List<ProjectTask> tasks)
        {
            try
            {
                string json = JsonSerializer.Serialize(tasks, new JsonSerializerOptions { WriteIndented = true });
                File.WriteAllText(_filePath, json);
            }
            catch (Exception ex)
            {
                UI.PrintError($"Save failed: {ex.Message}");
            }
        }

        public static List<ProjectTask> LoadTasks()
        {
            if (!File.Exists(_filePath)) return new List<ProjectTask>();
            try
            {
                string json = File.ReadAllText(_filePath);
                return JsonSerializer.Deserialize<List<ProjectTask>>(json) ?? new List<ProjectTask>();
            }
            catch
            {
                return new List<ProjectTask>();
            }
        }
    }

    // ==========================================
    // MAIN LOGIC
    // ==========================================
    public class TaskManager
    {
        private List<ProjectTask> _tasks;

        public TaskManager()
        {
            _tasks = FileService.LoadTasks();
        }

        public void AddTask()
        {
            UI.Header("Add New Task");

            Console.Write("Enter Title: ");
            string title = Console.ReadLine() ?? "Untitled";

            Console.Write("Enter Assignee: ");
            string assignee = Console.ReadLine() ?? "Unassigned";

            Console.Write("Priority (0=Low, 1=Med, 2=High): ");
            string? pInput = Console.ReadLine();
            int priority = int.TryParse(pInput, out int p) ? p : 0;

            Console.Write("Days until due: ");
            string? dInput = Console.ReadLine();
            int days = int.TryParse(dInput, out int d) ? d : 1;

            var newTask = new ProjectTask
            {
                Id = _tasks.Count > 0 ? _tasks.Max(t => t.Id) + 1 : 1,
                Title = title,
                Assignee = assignee,
                TaskPriority = (Priority)priority,
                TaskStatus = Status.ToDo,
                DueDate = DateTime.Now.AddDays(days)
            };

            _tasks.Add(newTask);
            FileService.SaveTasks(_tasks);
            Logger.Instance.Log($"Added Task {newTask.Id}");
            UI.PrintSuccess("Task created!");
        }

        public void ListTasks(List<ProjectTask>? tasksToShow = null)
        {
            var list = tasksToShow ?? _tasks;
            UI.Header("Task List");

            if (list.Count == 0)
            {
                Console.WriteLine("No tasks found.");
            }
            else
            {
                Console.WriteLine("-------------------------------------------------------------------------");
                Console.WriteLine("| ID  | Title                | Priority   | Status     | Due Date     |");
                Console.WriteLine("-------------------------------------------------------------------------");
                foreach (var task in list)
                {
                    if (task.DueDate < DateTime.Now && task.TaskStatus != Status.Done)
                    {
                        Console.ForegroundColor = ConsoleColor.Red;
                        Console.WriteLine(task.GetDetails() + " <OVERDUE>");
                        Console.ResetColor();
                    }
                    else
                    {
                        Console.WriteLine(task.GetDetails());
                    }
                }
                Console.WriteLine("-------------------------------------------------------------------------");
            }
            Console.WriteLine("\nPress Enter to return to menu...");
            Console.ReadLine();
        }

        public void UpdateTaskStatus()
        {
            UI.Header("Update Status");
            Console.Write("Enter Task ID: ");
            if (int.TryParse(Console.ReadLine(), out int id))
            {
                var task = _tasks.FirstOrDefault(t => t.Id == id);
                if (task != null)
                {
                    Console.WriteLine($"Current: {task.TaskStatus}");
                    Console.Write("New Status (0=ToDo, 1=InProg, 2=Done): ");
                    if (int.TryParse(Console.ReadLine(), out int s))
                    {
                        task.TaskStatus = (Status)s;
                        FileService.SaveTasks(_tasks);
                        Logger.Instance.Log($"Updated Task {id} to {task.TaskStatus}");
                        UI.PrintSuccess("Status updated!");
                        return;
                    }
                }
            }
            UI.PrintError("Task not found or invalid input.");
        }

        public void SearchTasks()
        {
            UI.Header("Search");
            Console.Write("Keyword: ");
            string kw = (Console.ReadLine() ?? "").ToLower();
            var results = _tasks.Where(t => t.Title.ToLower().Contains(kw) || t.Assignee.ToLower().Contains(kw)).ToList();
            ListTasks(results);
        }

        public void SortByPriority()
        {
            _tasks = _tasks.OrderByDescending(t => t.TaskPriority).ToList();
            UI.PrintSuccess("Sorted by Priority.");
            ListTasks();
        }

        public void SortByDate()
        {
            _tasks = _tasks.OrderBy(t => t.DueDate).ToList();
            UI.PrintSuccess("Sorted by Due Date.");
            ListTasks();
        }
    }

    // ==========================================
    // MAIN MENU
    // ==========================================
    class Program
    {
        static void Main(string[] args)
        {
            TaskManager manager = new TaskManager();
            bool running = true;

            while (running)
            {
                Console.Clear();
                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.WriteLine("╔════════════════════════════════════════╗");
                Console.WriteLine("║          PROJECT TASK TRACKER          ║");
                Console.WriteLine("╚════════════════════════════════════════╝");
                Console.ResetColor();
                Console.WriteLine(" 1. Add New Task");
                Console.WriteLine(" 2. View All Tasks");
                Console.WriteLine(" 3. Search Tasks");
                Console.WriteLine(" 4. Update Task Status");
                Console.WriteLine(" 5. Sort by Priority");
                Console.WriteLine(" 6. Sort by Due Date");
                Console.WriteLine(" 7. Exit");
                Console.WriteLine("------------------------------------------");
                Console.Write("Select Option: ");

                string choice = Console.ReadLine() ?? "";

                try
                {
                    switch (choice)
                    {
                        case "1": manager.AddTask(); break;
                        case "2": manager.ListTasks(); break;
                        case "3": manager.SearchTasks(); break;
                        case "4": manager.UpdateTaskStatus(); break;
                        case "5": manager.SortByPriority(); break;
                        case "6": manager.SortByDate(); break;
                        case "7": running = false; break;
                        default: UI.PrintError("Invalid Selection"); break;
                    }
                }
                catch (Exception ex)
                {
                    UI.PrintError($"Critical Error: {ex.Message}");
                }
            }
        }
    }
}