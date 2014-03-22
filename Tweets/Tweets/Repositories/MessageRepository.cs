using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data.Linq;
using System.Data.Linq.Mapping;
using System.Data.SqlClient;
using System.Linq;
using Tweets.ModelBuilding;
using Tweets.Models;

namespace Tweets.Repositories
{
    public class MessageRepository : IMessageRepository, IDisposable
    {
        private readonly string connectionString;
        private readonly AttributeMappingSource mappingSource;
        private readonly IMapper<Message, MessageDocument> messageDocumentMapper;
        private readonly SqlConnection connection;
        private readonly DataContext dataContext;
        private readonly Table<MessageDocument> messagesTable;
        private readonly Table<LikeDocument> likesTable;

        public MessageRepository(IMapper<Message, MessageDocument> messageDocumentMapper)
        {
            this.messageDocumentMapper = messageDocumentMapper;
            mappingSource = new AttributeMappingSource();
            connectionString = ConfigurationManager.ConnectionStrings["SqlConnectionString"].ConnectionString;
            connection = new SqlConnection(connectionString);
            connection.Open();
            dataContext = new DataContext(connection, mappingSource);

            messagesTable = dataContext.GetTable<MessageDocument>();
            likesTable = dataContext.GetTable<LikeDocument>();
        }

        public void Save(Message message)
        {
            var messageDocument = messageDocumentMapper.Map(message);
            messagesTable.InsertOnSubmit(messageDocument);

            dataContext.SubmitChanges();
        }

        public void Like(Guid messageId, User user)
        {
            var likeDocument = new LikeDocument
            {
                MessageId = messageId,
                UserName = user.Name,
                CreateDate = DateTime.UtcNow,
            };

            likesTable.InsertOnSubmit(likeDocument);
            dataContext.SubmitChanges();
        }

        private LikeDocument TryGetLike(User user, Guid messageId)
        {
            return likesTable.FirstOrDefault(l => l.MessageId == messageId && l.UserName == user.Name);
        }

        private MessageDocument TryGetMessage(Guid messageId)
        {
            return messagesTable.FirstOrDefault(m => m.Id == messageId);
        }

        public void Dislike(Guid messageId, User user)
        {
            var message = TryGetMessage(messageId);
            if (message != null)
            {
                var like = TryGetLike(user, messageId);
                if (like != null)
                {
                    likesTable.DeleteOnSubmit(like);
                    dataContext.SubmitChanges();
                }
            }
        }

        public IEnumerable<Message> GetPopularMessages()
        {
            return (from m in messagesTable
                let likesCount = likesTable.Count(l => l.MessageId == m.Id)
                orderby likesCount descending
                select new Message
                {
                    Id = m.Id,
                    CreateDate = m.CreateDate,
                    Text = m.Text,
                    Likes = likesCount,
                    User = new User {Name = m.UserName}
                }).Take(10).ToArray();
        }

        public IEnumerable<UserMessage> GetMessages(User user)
        {
            return messagesTable.Where(m => m.UserName == user.Name).Select(m =>
                new UserMessage
                {
                    Id = m.Id,
                    CreateDate = m.CreateDate,
                    Text = m.Text,
                    User = user,
                    Liked = likesTable.Any(l => l.UserName == user.Name && l.MessageId == m.Id),
                    Likes = likesTable.Count(l => l.MessageId == m.Id)
                }).ToArray();
        }

        public void Dispose()
        {
            connection.Dispose();
        }
    }
}