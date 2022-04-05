using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JVermeulen.Monitoring.Influx
{
    /// <summary>
    /// InfluxDB uses line protocol to write data points. It is a text-based format that provides the measurement, tag set, field set, and timestamp of a data point.
    /// <seealso cref="https://docs.influxdata.com/influxdb/v2.1/reference/syntax/line-protocol/"/>
    /// </summary>
    public record InfluxMeasurement
    {
        /// <summary>
        /// The measurement name. InfluxDB accepts one measurement per point. Measurement names are case-sensitive and subject to naming restrictions.
        /// </summary>
        public string Name { get; init; }

        /// <summary>
        /// All field key-value pairs for the point. Points must have at least one field. Field keys and string values are case-sensitive. Field keys are subject to naming restrictions.
        /// </summary>
        public Dictionary<string, object> Fields { get; init; }

        /// <summary>
        /// All tag key-value pairs for the point. Key-value relationships are denoted with the = operand. Multiple tag key-value pairs are comma-delimited. Tag keys and tag values are case-sensitive. Tag keys are subject to naming restrictions.
        /// </summary>
        public Dictionary<string, string> Tags { get; init; }

        /// <summary>
        /// The constructor of this class.
        /// </summary>
        /// <param name="name">The measurement name. InfluxDB accepts one measurement per point. Measurement names are case-sensitive and subject to naming restrictions.</param>
        /// <param name="tags">All tag key-value pairs for the point. Key-value relationships are denoted with the = operand. Multiple tag key-value pairs are comma-delimited. Tag keys and tag values are case-sensitive. Tag keys are subject to naming restrictions.</param>
        /// <param name="fields">All field key-value pairs for the point. Points must have at least one field. Field keys and string values are case-sensitive. Field keys are subject to naming restrictions.</param>
        public InfluxMeasurement(string name, Dictionary<string, object> fields = null)
        {
            if (name == null)
                throw new ArgumentNullException(nameof(name));
            else if (string.IsNullOrWhiteSpace(name))
                throw new ArgumentException("Measurement name is required.");
            else if (name.StartsWith('_'))
                throw new ArgumentException("Measurement names, tag keys, and field keys cannot begin with an underscore _. The _ namespace is reserved for InfluxDB system use.");

            Name = name;
            Fields = fields ?? new Dictionary<string, object>();
            
            Tags = new Dictionary<string, string>();
        }

        /// <summary>
        /// Returns a string that represents the current object.
        /// </summary>
        public override string ToString()
        {
            CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;

            if (!Fields.Any())
                return $"Measurement '{Name}' is invalid. Fields is empty.";

            var builder = new StringBuilder(Name);

            foreach (var tag in Tags)
            {
                builder.Append($",{tag.Key}={tag.Value}");
            }

            builder.Append(' ');
            builder.Append(string.Join(',', Fields.Select(field => $"{field.Key}={field.Value}")));

            return builder.ToString();
        }
    }
}
