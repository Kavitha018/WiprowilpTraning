using System;
using System.Collections.Generic;

class Student
{
    public int StudentId { get; set; }
    public string StudentName { get; set; }
    public Dictionary<string, int> SubjectMarks { get; set; }

    public double CalculateAverage()
    {
        int total = 0;
        foreach (var mark in SubjectMarks.Values)
        {
            total += mark;
        }
        return SubjectMarks.Count > 0 ? (double)total / SubjectMarks.Count : 0;
    }
}

class Program
{
    static void Main()
    {
        List<Student> students = new List<Student>();

        // Adding sample student data
        students.Add(new Student
        {
            StudentId = 1,
            StudentName = "Tej",
            SubjectMarks = new Dictionary<string, int>
            {
                { "Math", 85 },
                { "English", 78 },
                { "Science", 90 }
            }
        });

        students.Add(new Student
        {
            StudentId = 2,
            StudentName = "Kavitha",
            SubjectMarks = new Dictionary<string, int>
            {
                { "Math", 70 },
                { "English", 65 },
                { "Science", 80 },
                { "Computer", 75 }
            }
        });

        // Displaying student details with average marks
        foreach (var student in students)
        {
            Console.WriteLine($"Student ID: {student.StudentId}");
            Console.WriteLine($"Student Name: {student.StudentName}");
            Console.WriteLine("Subject Marks:");
            foreach (var subject in student.SubjectMarks)
            {
                Console.WriteLine($"  {subject.Key}: {subject.Value}");
            }
            Console.WriteLine($"Average Score: {student.CalculateAverage():F2}");
            Console.WriteLine(new string('-', 40));
        }
    }
}