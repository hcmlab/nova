using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Apache.NMS;
using Apache.NMS.ActiveMQ;

namespace ssi
{
    public class ActiveMQSender
    {
        public IConnectionFactory connectionFactory;
        public IConnection connection;
        public ISession session;

        public string[] dependencies(Dictionary<string, object> parameters)
        {
            return new string[] { "Apache.NMS", "Apache.NMS.ActiveMQ" };
        }

        public string open(Dictionary<string,object> parameters)
        {
            try
            {
                string uri = (string)parameters["uri"];
              
                connectionFactory = new ConnectionFactory(uri);

                if (connection == null)
                {
                    connection = connectionFactory.CreateConnection();
                    connection.Start();
                    session = connection.CreateSession();
                }
               
            }
            catch (Exception ex)
            {
                return ex.ToString();
            }            

            return null;
        }

        public string update_enter(Dictionary<string, object> parameters)
        {
            try
            { 
                string label = (string)parameters["label"];
                string topicname = (string)parameters["topic"];

                ITopic topic = session.GetTopic(topicname);
                using (IMessageProducer producer = session.CreateProducer(topic))
                {
                    var textMessage = producer.CreateTextMessage(label);
                    producer.Send(textMessage);
                }
               
            }
            catch (Exception ex)
            {
                return ex.ToString();
            }

            return null;
        }

        public string update_leave(Dictionary<string, object> parameters)
        {
            return null;
        }

        public string close(Dictionary<string, object> parameters)
        {
            try
            { 
                string uri = (string)parameters["uri"];
                string topic = (string)parameters["topic"];
                session.Close();
                connection.Close();

            }
            catch (Exception ex)
            {
                return ex.ToString();
            }

            return null;
        }
    }
}
