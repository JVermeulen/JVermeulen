using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Text;
using System.Threading.Tasks;

namespace JVermeulen.Processing
{
    public class Generator<T> : IDisposable
    {
        public string Name { get; set; }

        private Subject<T> Value { get; set; }
        public IObservable<T> OnReceive => Value.AsObservable();

        public Generator(string name = null)
        {
            Name = name ?? typeof(T).Name;
            Value = new Subject<T>();
        }

        public void Send(T value)
        {
            Value.OnNext(value);
        }

        public void Dispose()
        {
            Value?.Dispose();
        }
    }
}
