System Purpose
The Task & Project Tracker is a console-based C# application designed to help teams manage project lifecycles. It allows users to create, track, and manage tasks with features like prioritization, deadline tracking, and persistent data storage.
Design Patterns Applied
Singleton Pattern (Logger class):
Justification: I used the Singleton pattern for the Logger to ensure that only one instance of the file writer exists during the application's lifecycle. This prevents "File in Use" errors and ensures thread safety when writing to activity_log.txt.
Factory Method (Implicit in TaskManager):
Justification: The creation of ProjectTask objects is centralized within the Manager, ensuring that all tasks are initialized with valid IDs and default states (ToDo) before being added to the list.
Data Structures & Algorithms
Collections: List<ProjectTask> was used because it provides dynamic resizing and easy access to LINQ methods for searching and sorting.
Linear Search: Used for the "Search" feature to iterate through the list and find matching strings (Title/Assignee). Time complexity: O(n).
Bubble Sort: A manual sorting algorithm was implemented to sort tasks by Priority. This demonstrates fundamental algorithmic understanding by swapping elements based on their enum integer value.
