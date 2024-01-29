using StoreParser.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using ZakupkiGov.Enums;

namespace ZakupkiGov.Model
{
    internal abstract class AbstractController<T> : BindableObjectBase, IController
    {
        protected Queue<T> _jobs;
        protected ProcessStates _state;
        protected Task[] _tasks;
        protected int _total;
        protected int _processedCount;
        protected CancellationTokenSource _stopCancellationTokenSource;
        protected int _threadCount;

        public ProcessStates State
        {
            get
            {
                return _state;
            }
            protected set
            {
                _state = value;
                OnPropertyChanged("state");
            }
        }

        public int Total
        {
            get
            {
                return _total;
            }
            protected set
            {
                _total = value;
                OnPropertyChanged("total");
                OnPropertyChanged("PercentSucceed");
                OnPropertyChanged("StatisticText");
            }
        }

        public int PercentSucceed
        {
            get
            {
                return (int)((double)_processedCount / Total * 100.0);
            }
        }

        public abstract string StatisticText { get; }

        protected object lock_jobs = new object();

        public AbstractController(int threadCount)
        {
            _threadCount = threadCount;

            _tasks = new Task[_threadCount];
            _stopCancellationTokenSource = new CancellationTokenSource();
            _jobs = new Queue<T>();
        }

        public abstract void AddItems(List<T> items);

        public abstract void Execute();

        public async Task ExecuteAsync()
        {
            await Task.Run((Action)Execute);
        }

        public virtual void Stop()
        {
            if (State == ProcessStates.WORKING)
            {
                State = ProcessStates.STOPPING;

                _stopCancellationTokenSource.Cancel();
            }
        }

        public ProcessStates GetState()
        {
            return State;
        }

        public int GetTotal()
        {
            return Total;
        }
    }
}
