using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace JVermeulen.Processing
{
    public record Content
    {
        public byte[] Value;

        public Content(byte[] value)
        {
            Value = value;
        }

        public bool TryGetValueAs<T>(out T value)
        {
            value = default;

            try
            {
                if (Value == null)
                {
                    return true;
                }
                else if (typeof(T) == typeof(byte[]))
                {
                    value = (T)Convert.ChangeType(Value, typeof(T));

                    return true;
                }
                else if (typeof(T) == typeof(string))
                {
                    var text = Encoding.UTF8.GetString(Value);

                    value = (T)Convert.ChangeType(text, typeof(T));

                    return true;
                }
                else if (typeof(T) == typeof(JsonDocument))
                {
                    var text = Encoding.UTF8.GetString(Value);
                    var json = JsonDocument.Parse(text);

                    value = (T)Convert.ChangeType(json, typeof(T));

                    return true;
                }
                else if (typeof(T) == typeof(XDocument))
                {
                    var text = Encoding.UTF8.GetString(Value);
                    var xml = XDocument.Parse(text);

                    value = (T)Convert.ChangeType(xml, typeof(T));

                    return true;
                }
            }
            catch
            {
                return false;
            }

            return false;
        }

        public override string ToString()
        {
            return $"{Value.Length} bytes";
        }
    }
}
