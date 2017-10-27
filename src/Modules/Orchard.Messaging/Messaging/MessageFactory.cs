using System;
using System.Collections.Generic;
using System.Reflection;
using System.Linq;
using Orchard.Domain.Entities;

namespace Orchard.Messaging
{
    internal delegate IMessage MessageFactoryDelegate(object body);

    public static class MessageFactory
    {
        static readonly Dictionary<Type, MessageFactoryDelegate> CacheFn
            = new Dictionary<Type, MessageFactoryDelegate>();

        public static IMessage Create(object response)
        {
            if (response is IMessage responseMessage)
            {
                if (responseMessage != null)
                    return responseMessage;

                if (response == null) return null;
                var type = response.GetType();

                MessageFactoryDelegate factoryFn;
                lock (CacheFn) CacheFn.TryGetValue(type, out factoryFn);

                if (factoryFn != null)
                    return factoryFn(response);

                var genericMessageType = typeof(Message<>).MakeGenericType(type);
#if NETFX_CORE || NETSTANDARD1_1 || PORTABLE7
            var mi = genericMessageType.GetRuntimeMethods().First(p => p.Name.Equals("Create"));
            factoryFn = (MessageFactoryDelegate)mi.CreateDelegate(
                typeof(MessageFactoryDelegate));
#else
                var mi = genericMessageType.GetMethod("Create",
                    BindingFlags.Public | BindingFlags.Static);
                factoryFn = (MessageFactoryDelegate)Delegate.CreateDelegate(
                    typeof(MessageFactoryDelegate), mi);
#endif

                lock (CacheFn) CacheFn[type] = factoryFn;

                return factoryFn(response);
            }
            else
            {
                return null;
            }
        }
    }

    public class Message : IMessage
    {
        public Guid Id { get; set; }
        public DateTime CreatedDate { get; set; }
        public long Priority { get; set; }
        public int RetryAttempts { get; set; }
        public Guid? ReplyId { get; set; }
        public string ReplyTo { get; set; }
        public int Options { get; set; }
        public ResponseStatus Error { get; set; }
        public string Tag { get; set; }
        public Dictionary<string, string> Meta { get; set; }
        public object Body { get; set; }
    }

    /// <summary>
    /// Basic implementation of IMessage[T]
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class Message<T>
        : Message, IMessage<T>
    {
        /// <summary>
        /// [DataContract]��ʶ�����ͻ�ת��ΪNewtonsoft.Json.Linq.JObject
        /// </summary>
        private T TBody { get; set; }
        public Message()
        {
            this.Id = Guid.NewGuid();
            this.CreatedDate = DateTime.UtcNow;
            this.Options = (int)MessageOption.NotifyOneWay;
        }

        public Message(T body)
            : this()
        {
            Body = body;
            TBody = body;
        }

        public static IMessage Create(object oBody)
        {
            return new Message<T>((T)oBody);
        }

        public T GetBody()
        {
            return (T)Body;
            //return TBody;
        }

        public override string ToString()
        {
            return string.Format("CreatedDate={0}, Id={1}, Type={2}, Retry={3}",
                this.CreatedDate,
                this.Id.ToString("N"),
                typeof(T).Name,
                this.RetryAttempts);
        }

    }
}