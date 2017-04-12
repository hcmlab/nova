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
        public IConnection _connection;
        public ISession _session;

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

                if (_connection == null)
                {
                
                        _connection = connectionFactory.CreateConnection();
                        _connection.Start();
                        _session = _connection.CreateSession();
                    
                }
               
            }
            catch (Exception ex)
            {
                return ex.ToString();
            }            

            return null;
        }

        public string update(Dictionary<string, object> parameters)
        {
            try
            { 
                string label = (string)parameters["label"];
                string topicname = (string)parameters["topic"];

                ITopic topic = _session.GetTopic(topicname);
                using (IMessageProducer producer = _session.CreateProducer(topic))
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

        public string close(Dictionary<string, object> parameters)
        {
            try
            { 
                string uri = (string)parameters["uri"];
                string topic = (string)parameters["topic"];
                _session.Close();
                _connection.Close();

            }
            catch (Exception ex)
            {
                return ex.ToString();
            }

            return null;
        }
    }
}
