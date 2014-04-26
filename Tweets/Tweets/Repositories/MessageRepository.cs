using MongoDB.Driver;
using MongoDB.Driver.Builders;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using Tweets.ModelBuilding;
using Tweets.Models;

namespace Tweets.Repositories
{
    public class MessageRepository : IMessageRepository
    {
        private readonly IMapper<Message, MessageDocument> messageDocumentMapper;
        private readonly MongoCollection<MessageDocument> messagesCollection;

        public MessageRepository(IMapper<Message, MessageDocument> messageDocumentMapper)
        {
            this.messageDocumentMapper = messageDocumentMapper;
            var connectionString = ConfigurationManager.ConnectionStrings["MongoDb"].ConnectionString;
            var databaseName = MongoUrl.Create(connectionString).DatabaseName;
            messagesCollection =
                new MongoClient(connectionString).GetServer().GetDatabase(databaseName).GetCollection<MessageDocument>(MessageDocument.CollectionName);
        }

        public void Save(Message message)
        {
            var messageDocument = messageDocumentMapper.Map(message);
            messagesCollection.Insert(messageDocument);
        }

        public void Like(Guid messageId, User user)
        {
            var message = messagesCollection.FindOne(Query<MessageDocument>.EQ(d => d.Id, messageId));

            if (message != null && message.Likes.All(l => l.UserName != user.Name))
            {
                message.Likes = message.Likes.Concat(new [] {new LikeDocument {CreateDate = DateTime.UtcNow, UserName = user.Name}});
                messagesCollection.Save(message);
            }
        }

        public void Dislike(Guid messageId, User user)
        {
            var message = messagesCollection.FindOne(Query<MessageDocument>.EQ(d => d.Id, messageId));
            if (message != null)
            {
                message.Likes = message.Likes.Where(like => like.UserName != user.Name);

                messagesCollection.Save(message);
            }
        }

        public IEnumerable<Message> GetPopularMessages()
        {
            //TODO: Здесь нужно возвращать 10 самых популярных сообщений
            //TODO: Важно сортировку выполнять на сервере
            //TODO: Тут будет полезен AggregationFramework
            return Enumerable.Empty<Message>();
        }

        public IEnumerable<UserMessage> GetMessages(User user)
        {
            return
                messagesCollection.Find(Query<MessageDocument>.EQ(d => d.UserName, user.Name)).SetSortOrder(SortBy<MessageDocument>.Descending(d => d.CreateDate))
                    .Select(
                        d =>
                            new UserMessage
                            {
                                CreateDate = d.CreateDate,
                                Id = d.Id,
                                Likes = d.Likes == null? 0 : d.Likes.Count(),
                                Text = d.Text,
                                User = user,
                                Liked = d.Likes != null && d.Likes.Any(l => l.UserName == user.Name)
                            });
        }
    }
}