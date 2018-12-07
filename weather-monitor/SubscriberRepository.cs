using Microsoft.WindowsAzure.Storage.Table;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Weather.Monitor
{
    public class SubscriberRepository
    {
        private CloudTable _subscribersTable;
        public SubscriberRepository(CloudTableClient tableClient)
        {
            _subscribersTable = tableClient.GetTableReference("subscribers");
            _subscribersTable.CreateIfNotExistsAsync().Wait();
        }

        public async Task<List<SubscriberModel>> GetActiveSubscribers()
        {
            var allActiveSubscribers = new TableQuery<SubscriberEntity>()
            {
                FilterString = $"{nameof(SubscriberEntity.Active)} eq true"
            };

            var allEntries = new List<SubscriberEntity>();
            TableContinuationToken token = null;
            do
            {
                TableQuerySegment<SubscriberEntity> resultSegment = await _subscribersTable.ExecuteQuerySegmentedAsync(allActiveSubscribers, token);
                token = resultSegment.ContinuationToken;
                allEntries.AddRange(resultSegment.Results);
            } while (token != null);
            return allEntries.Select(x => new SubscriberModel()
            {
                Active = x.Active,
                Subscribers = JsonConvert.DeserializeObject<List<Subscriber>>(x.SubscribersList),
                GridpointWfoY = x.GridpointWfoY,
                GridpointWfoX = x.GridpointWfoX,
                GridpointWfo = x.GridpointWfo
            }).ToList();
        }

        public async Task<SubscriberModel> AddSubscriber(SubscriberModel subscriber)
        {
            var entity = new SubscriberEntity(
                    JsonConvert.SerializeObject(subscriber.Subscribers), 
                    subscriber.GridpointWfo, 
                    subscriber.GridpointWfoX, 
                    subscriber.GridpointWfoY, 
                    subscriber.Active, 
                    Guid.NewGuid()
                );
            var insertOperation = TableOperation.Insert(entity);
            try
            {
                var result = (SubscriberEntity)(await _subscribersTable.ExecuteAsync(insertOperation)).Result;
                return new SubscriberModel()
                {
                    Active = result.Active,
                    GridpointWfo = result.GridpointWfo,
                    GridpointWfoX = result.GridpointWfoX,
                    GridpointWfoY = result.GridpointWfoY,
                    Subscribers = JsonConvert.DeserializeObject<List<Subscriber>>(result.SubscribersList)
                };
            }
            catch (Exception e)
            {
                return null;
            }
        }
    }

    public class SubscriberModel
    {
        public string GridpointWfo { get; set; }
        public string GridpointWfoX { get; set; }
        public string GridpointWfoY { get; set; }
        public bool Active { get; set; }
        public List<Subscriber> Subscribers { get; set; }
    }

    public class SubscriberEntity: TableEntity
    {
        public SubscriberEntity(){}
        public SubscriberEntity(string subscribersList, string gridpointWfo, string gridpointWfoX, string gridpointWfoY, bool active, Guid id = default(Guid))
        {
            id = id == default(Guid) ? Guid.NewGuid() : id;
            PartitionKey = id.ToString();
            RowKey = id.ToString();
            SubscribersList = subscribersList;
            GridpointWfo = gridpointWfo;
            GridpointWfoX = gridpointWfoX;
            GridpointWfoY = gridpointWfoY;
            Active = active;
        }

        public string SubscribersList { get; set; }
        public string GridpointWfo { get; set; }
        public string GridpointWfoX { get; set; }
        public string GridpointWfoY { get; set; }
        public bool Active { get; set; }
    }

    public class Subscriber
    {
        public string Name { get; set; }
        public string Email { get; set; }
    }
}
