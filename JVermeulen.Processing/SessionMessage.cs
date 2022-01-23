using System;
using System.Collections.Generic;
using System.Threading;

namespace JVermeulen.Processing
{
    /// <summary>
    /// A generic session message.
    /// </summary>
    public class SessionMessage : ICloneable
    {
        /// <summary>
        /// A global unique Id.
        /// </summary>
        private static long GlobalId;

        /// <summary>
        /// A unique Id for this message.
        /// </summary>
        public long Id { get; private set; }

        /// <summary>
        /// The time this message has been created.
        /// </summary>
        public DateTime CreatedAt { get; private set; }

        /// <summary>
        /// The sender of this message.
        /// </summary>
        public Session Sender { get; set; }

        /// <summary>
        /// The content of this message.
        /// </summary>
        public object Content { get; set; }

        /// <summary>
        /// The constructor of this class.
        /// </summary>
        /// <param name="sender">The sender of this message.</param>
        /// <param name="content">The content of this message.</param>
        public SessionMessage(Session sender, object content)
        {
            Id = Interlocked.Increment(ref GlobalId);
            CreatedAt = DateTime.Now;

            Sender = sender;
            Content = content;
        }

        /// <summary>
        /// The content is of type ContentMessage.
        /// </summary>
        public bool IsContentMessageType => Content != null && Content.GetType().IsGenericType && Content.GetType().GetGenericTypeDefinition() == typeof(ContentMessage<>);

        /// <summary>
        /// Checks if the content is of the given type. T is the type to check for.
        /// </summary>
        public bool ContentIsTypeof<T1>()
        {
            if (Content is T1)
                return true;
            else if (Content is SessionMessage sessionMessage)
                return sessionMessage.ContentIsTypeof<T1>();
            else
                return false;
        }

        /// <summary>
        /// Checks if the content is of the given type. T is the type to check for.
        /// </summary>
        public bool ContentIsTypeof<T1>(out T1 content)
        {
            if (ContentIsTypeof<T1>())
            {
                content = (T1)Content;

                return true;
            }
            else
            {
                content = default;

                return false;
            }
        }

        /// <summary>
        /// Checks if the content is of the given type. Tx is the types to check for.
        /// </summary>
        public bool ContentIsTypeof<T1, T2>()
        {
            if (Content is T1 || Content is T2)
                return true;
            else if (Content is SessionMessage sessionMessage)
                return sessionMessage.ContentIsTypeof<T1, T2>();
            else
                return false;
        }

        /// <summary>
        /// Checks if the content is of the given type. Tx is the types to check for.
        /// </summary>
        public bool ContentIsTypeof<T1, T2, T3>()
        {
            if (Content is T1 || Content is T2 || Content is T3)
                return true;
            else if (Content is SessionMessage sessionMessage)
                return sessionMessage.ContentIsTypeof<T1, T2, T3>();
            else
                return false;
        }

        /// <summary>
        /// Checks if the Sender and content are of the given types.
        /// </summary>
        public bool SenderIsTypeOf<T1>()
        {
            if (Sender == null || Content == null)
                return false;
            else if (Content is SessionMessage sessionMessage)
                return sessionMessage.SenderIsTypeOf<T1>();
            else
                return (Sender.GetType() == typeof(T1));
        }


        /// <summary>
        /// Checks if the Sender and content are of the given types.
        /// </summary>
        public bool SenderIsTypeOf<T1>(out T1 sender) where T1 : Session
        {
            if (SenderIsTypeOf<T1>())
            {
                sender = (T1)Sender;

                return true;
            }
            else
            {
                sender = default;

                return false;
            }
        }

        /// <summary>
        /// Return true when the sender and content are of the given type, or one of the inner contents are.
        /// </summary>
        /// <typeparam name="TSender">The type of sender.</typeparam>
        /// <typeparam name="TContent">The type of content.</typeparam>
        public bool Contains<TSender, TContent>()
        {
            return Find<TSender, TContent>() != null;
        }

        /// <summary>
        /// Return the inner message when the sender and content are of the given type, or one of the inner contents are.
        /// </summary>
        /// <typeparam name="TSender">The type of sender.</typeparam>
        /// <typeparam name="TContent">The type of content.</typeparam>
        public SessionMessage Find<TSender, TContent>()
        {
            return Find(m => m.SenderIsTypeOf<TSender>() && m.ContentIsTypeof<TContent>());
        }

        /// <summary>
        /// Return the inner message when the sender and content are of the given type, or one of the inner contents are.
        /// </summary>
        /// <typeparam name="TSender">The type of sender.</typeparam>
        /// <typeparam name="TContent">The type of content.</typeparam>
        public bool Find<TSender, TContent>(out SessionMessage message)
        {
            message = Find(m => m.SenderIsTypeOf<TSender>() && m.ContentIsTypeof<TContent>());

            return message != null;
        }

        /// <summary>
        /// Return the inner message when the sender and content are of the given type, or one of the inner contents are.
        /// </summary>
        /// <typeparam name="TSender">The type of sender.</typeparam>
        /// <typeparam name="TContent">The type of content.</typeparam>
        /// <param name="sender">The sender of the given type.</param>
        /// <param name="content">The content of the given type.</param>
        /// <returns></returns>
        public bool Find<TSender, TContent>(out TSender sender, out TContent content) where TSender : Session
        {
            content = default;

            if (SenderIsTypeOf(out sender) && ContentIsTypeof(out content))
                return true;
            else if (Content is SessionMessage sessionMessage)
                return sessionMessage.Find(out sender, out content);
            else
                return false;
        }

        /// <summary>
        /// Return true when the sender and content are of the given type, or one of the inner contents are.
        /// </summary>
        /// <param name="query">The where statement.</param>
        public SessionMessage Find(Func<SessionMessage, bool> query)
        {
            if (query.Invoke(this))
                return this;
            else if (Content is SessionMessage sessionMessage)
                return sessionMessage.Find(query);
            else
                return null;
        }

        /// <summary>
        /// Return true when the content is of type ContentMessage, or one of the inner contents are.
        /// </summary>
        public bool TryFindContentMessage(out SessionMessage message)
        {
            message = null;

            if (Content is SessionMessage sessionMessage)
                sessionMessage.TryFindContentMessage(out message);
            else if (IsContentMessageType)
                message = this;

            return message != null;
        }

        /// <summary>
        /// A String that represents the current object.
        /// </summary>
        public override string ToString()
        {
            return $"{Sender}: {Content}";
        }

        /// <summary>
        /// Returns a new object with the same values.
        /// </summary>
        public object Clone()
        {
            return new SessionMessage(Sender, Content is ICloneable cloneable ? cloneable.Clone() : Content);
        }
    }
}
