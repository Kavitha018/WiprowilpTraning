 using System.Collections.Generic;
 public class CollectionExamples
 {

     public static void Main()
     {

          List<string> students = new List<string>();
          students.Add("Niti");
          students.Add("Preeti");
         students.Add(34);

          foreach (string student in students)
          {
             Console.WriteLine(student);
          }
          Dictionary<int, string> data = new Dictionary<int, string>();
          data.Add(101, "Niti");
          data.Add(102, "Preeti");
         Console.WriteLine(data[101]);
         foreach (KeyValuePair<int, string> kv in data)
          {
            Console.WriteLine(kv.Key + " " + kv.Value);
          }

          HashSet<string> employees = new HashSet<string>();
          employees.Add("Avc");
         employees.Add("xyz");
          employees.Add("Avc");

          foreach (string e in employees)
          {
              Console.WriteLine(e);

          }

         Stack<string> todotask = new Stack<string>();
         
          todotask.Push("Learn C#");
        todotask.Push("Revise the concepts");
         todotask.Push("Clear your exam");

            foreach (string task in todotask)
             {

             Console.WriteLine(task);
             }
             while (todotask.Count > 0)     
            {
                todotask.Pop();
                 Console.WriteLine("One task in completed ");
                }


Queue<string> tickets = new Queue<string>();
        tickets.Enqueue("Learn C#");
        tickets.Enqueue("Revise the concepts");
         tickets.Enqueue("Clear your exam");

         foreach (string task in tickets)
         {
             Console.WriteLine(task);
         }     }



 }
