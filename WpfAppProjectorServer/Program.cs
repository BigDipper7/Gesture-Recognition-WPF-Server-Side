using System;
using Eneter.Messaging.EndPoints.TypedMessages;
using Eneter.Messaging.MessagingSystems.MessagingSystemBase;
using Eneter.Messaging.MessagingSystems.TcpMessagingSystem;
using Eneter.Messaging.MessagingSystems.Composites;
using Eneter.Messaging.DataProcessing.Serializing;
using WpfApplicationTest;

namespace ServiceExample
{
    // Request message type
    public class MyRequest
    {
        public string Text { get; set; }
    }

    // Response message type
    public class MyResponse
    {
        public int Length { get; set; }
    }

    class Program
    {
        private static IDuplexTypedMessageReceiver<MyResponse, MyRequest> myReceiver;

        public static void run()
        {
            // Create message receiver receiving 'MyRequest' and receiving 'MyResponse'.
            IDuplexTypedMessagesFactory aReceiverFactory = new DuplexTypedMessagesFactory();
            myReceiver = aReceiverFactory.CreateDuplexTypedMessageReceiver<MyResponse, MyRequest>();

            // Subscribe to handle messages.
            myReceiver.MessageReceived += OnMessageReceived;

            // Create TCP messaging.
            IMessagingSystemFactory aMessaging = new TcpMessagingSystemFactory();
            IDuplexInputChannel anInputChannel
                = aMessaging.CreateDuplexInputChannel("tcp://192.168.43.167:8067/");
                //= aMessaging.CreateDuplexInputChannel("tcp://192.168.173.1:8060/");

            // Attach the input channel and start to listen to messages.
            myReceiver.AttachDuplexInputChannel(anInputChannel);

            Console.WriteLine("The service is running. To stop press enter.");
            Console.ReadLine();

            // Detach the input channel and stop listening.
            // It releases the thread listening to messages.
            myReceiver.DetachDuplexInputChannel();
        }

        // It is called when a message is received.
        private static void OnMessageReceived(object sender, TypedRequestReceivedEventArgs<MyRequest> e)
        {
            Console.WriteLine("Received: " + e.RequestMessage.Text);

            // Create the response message.
            MyResponse aResponse = new MyResponse();
            aResponse.Length = e.RequestMessage.Text.Length;

            // Send the response message back to the client.
            myReceiver.SendResponseMessage(e.ResponseReceiverId, aResponse);

            String msg = e.RequestMessage.Text;
            if (msg.Equals("Open"))
            {
                Console.WriteLine("Simulate Open Action");
                //MainWindow.simulateOpenAction();
            }
            else if (msg.Equals("Close"))
            {
                Console.WriteLine("Simulate Close Action");
               // MainWindow.simulateCloseAction();
            }
        }

        public static void log(string str)
        {
            Console.WriteLine(str);
        }
    }
}
