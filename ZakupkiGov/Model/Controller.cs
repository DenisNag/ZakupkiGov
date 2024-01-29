using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using WebLibrary.Core;
using ZakupkiGov.Enums;

namespace ZakupkiGov.Model
{
    internal class Controller : AbstractController<string>
    {
        private int _minDelay;
        private int _maxDelay;
        private string _directory;
        private string _excelFile;

        public event OnMessageHandler OnMessage;
        public delegate void OnMessageHandler(string message);

        private static object lock_file = new object();

        public override string StatisticText
        {
            get
            {
                return string.Format("Выполнено {0} из {1}", _processedCount, Total);
            }
        }

        public Controller(int threadCount, int minDelay, int maxDelay, string directory, string excelFile) : base(threadCount)
        {
            _minDelay = minDelay;
            _maxDelay = maxDelay;

            _directory = directory;
            _excelFile = excelFile;
        }

        private void Process()
        {
            var finished = false;

            while (!_stopCancellationTokenSource.Token.IsCancellationRequested && !finished)
            {
                var job = (string)null;

                lock (lock_jobs)
                {
                    if (_jobs.Count > 0)
                    {
                        job = _jobs.Dequeue();
                    }
                }

                if (!(finished = job == null))
                {
                    var web = new ZakupkiGovWeb();

                    try
                    {
                        var zakupka = web.ParseMainInfo(job);

                        zakupka.Directory = Path.Combine(_directory, zakupka.Number);

                        if (!Directory.Exists(zakupka.Directory))
                        {
                            Directory.CreateDirectory(zakupka.Directory);
                        }

                        zakupka.Files.AddRange(web.ParseFiles(zakupka.Number, zakupka.Directory));

                        var errorList = new List<string>();

                        lock (lock_file)
                        {
                            ExcelController.AddZakupki(_excelFile, ref errorList, zakupka);
                        }

                        if (errorList.Count > 0)
                        {
                            OnMessage?.Invoke("Номер уже существует: " + job);
                        }
                        else
                        {
                            OnMessage?.Invoke("Загружено: " + job);
                        }
                    }
                    catch(Exception ex)
                    {
                        OnMessage?.Invoke("Не загружено: " + job);
                    }

                    Interlocked.Increment(ref _processedCount);

                    OnPropertyChanged("PercentSucceed");
                    OnPropertyChanged("StatisticText");
                }

                Thread.Sleep(Randomizer.Next(_minDelay, _maxDelay));
            }
        }

        public override void Execute()
        {
            State = ProcessStates.WORKING;

            for (int i = 0; i < _threadCount; i++)
            {
                var task = Task.Factory.StartNew(Process);

                _tasks[i] = task;
            }

            Task.WaitAll(_tasks);

            State = ProcessStates.NONE;
        }

        public async void AsyncExecute()
        {
            await Task.Run((Action)Execute);
        }

        public override void AddItems(List<string> items)
        {
            lock (lock_jobs)
            {
                foreach (var job in items)
                {
                    _jobs.Enqueue(job);
                }

                Total = _jobs.Count;
            }
        }
    }
}
