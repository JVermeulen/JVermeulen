using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Text;
using System.Threading.Tasks;

namespace JVermeulen.Processing
{
    public class Generator<T> : TimeCounter
    {
        public string Name { get; set; }

        private Subject<T> Value { get; set; }
        public IObservable<T> OnReceive => Value.AsObservable();

        public Generator(string name = null) : base(false)
        {
            Name = name ?? typeof(T).Name;
            Value = new Subject<T>();
        }

        public override void Start()
        {
            base.Start();
        }

        public override void Stop()
        {
            base.Stop();
        }

        public void Send(T value)
        {
            Value.OnNext(value);
        }

        public override void Dispose()
        {
            Value?.Dispose();

            base.Dispose();
        }
    }
}
