using System.Diagnostics;

namespace CallCenterReport
{
    internal class Program
    {
        static void Main(string[] args)
        {
            Stopwatch stopwatch = new Stopwatch();
            stopwatch.Start();

            // в задании указано, что мы передаем только имя файла
            string fullPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, args[0] + ".csv");

            // сразу берем весь файл, тк нам нужны все строки и файл не большого размера
            // чтобы не проходить дважды по массиву сразу материализую
            var sessions = File.ReadLines(fullPath).Skip(1).Select(line =>
            {
                var splitValues = line.Split(';');
                return new Session
                {
                    DateTimeStart = DateTime.Parse(splitValues[0]),
                    DateTimeEnd = DateTime.Parse(splitValues[1]),
                    Employee = splitValues[3],
                    State = splitValues[4]
                };
            }).ToList();

            ReportOne(sessions);
            ReportTwo(sessions);

            stopwatch.Stop();
            Console.WriteLine($"Время выполнения программы: {stopwatch.Elapsed}");
        }

        private static void ReportOne(List<Session> sessions)
        {
            var events = new List<(DateTime dateTime, bool isStart)>();
            foreach (var s in sessions)
            {
                events.Add((s.DateTimeStart, true));
                events.Add((s.DateTimeEnd, false));
            }

            // немного не понял про граничные условия, ThenByDescending сделает так что мы учитываем границы как два звонка
            var sortedEvents = events.OrderBy(e => e.dateTime).ThenByDescending(e => e.isStart).ToArray();
            int activeSessions = 0;
            var maxActivities = new List<(DateTime date, int maxActiveSessions)>();

            foreach (var e in sortedEvents)
            {
                // выглядит не очень
                if (e.isStart)
                    activeSessions++;
                else
                    activeSessions--;

                if (activeSessions > 1)
                    maxActivities.Add((e.dateTime, activeSessions));
            }

            var result = maxActivities.GroupBy(r => r.date.Date)
                .Select(s => new 
                { 
                    date = s.Key.ToString("yyyy-MM-dd"), 
                    maxActiveSessions = s.Max(r => r.maxActiveSessions) 
                });

            Console.WriteLine("День\tКоличество сессий\n");
            foreach (var r in result)
                Console.WriteLine($"{r.date} {r.maxActiveSessions}\n");
        }

        private static void ReportTwo(List<Session> sessions)
        {
            var result = sessions.GroupBy(item => item.Employee)
                .Select(group => new
                {
                    Employee = group.Key,
                    StateCounts = group.GroupBy(item => item.State)
                        .ToDictionary(stateGroup => stateGroup.Key, stateGroup => stateGroup.Count())
                });

            Console.WriteLine("ФИО\tПауза\tГотов\tРазговор\tОбработка\tПерезвон\n");
            foreach (var stat in result)
            {
                // выглядит не очень
                stat.StateCounts.TryGetValue("Пауза", out int pause);
                stat.StateCounts.TryGetValue("Готов", out int ready);
                stat.StateCounts.TryGetValue("Разговор", out int conversation);
                stat.StateCounts.TryGetValue("Обработка", out int handle);
                stat.StateCounts.TryGetValue("Перезвон", out int recall);

                Console.WriteLine($"{stat.Employee}\t{pause}\t{ready}\t{conversation}\t{handle}\t{recall}\n");
            }
        }
    }
}