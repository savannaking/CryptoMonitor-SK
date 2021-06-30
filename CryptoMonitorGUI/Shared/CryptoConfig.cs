using Azure;
using Azure.Data.Tables;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CryptoMonitorGUI.Shared
{
    public class CryptoConfig : ITableEntity
    {
        public CryptoConfig()
        {
            this.PartitionKey = "CryptoMonitor";
            this.RowKey = "Config";
        }

        public IEnumerable<string> Currencies { get; set; }
        public int? Frequency { get; set; }
        public double? Threshold { get; set; }
        public string PartitionKey { get; set; }
        public string RowKey { get; set; }
        public DateTimeOffset? Timestamp { get; set; }
        public ETag ETag { get; set; }

        public static CryptoConfig ToCryptoConfig(TableEntity entity)
        {
            CryptoConfig config = new CryptoConfig();
            config.Currencies = entity["Currencies"].ToString().Split(',');
            config.Frequency = (int?)entity["Frequency"];
            config.Threshold = (double?)entity["Threshold"];
            return config;
        }

        public static TableEntity ToTableEntity(CryptoConfig config)
        {
            var entity = new TableEntity("CryptoMonitor", "Config");
            entity["Currencies"] = string.Join(',', config.Currencies);
            entity["Frequency"] = config.Frequency;
            entity["Threshold"] = config.Threshold;
            return entity;
        }
    }
}
