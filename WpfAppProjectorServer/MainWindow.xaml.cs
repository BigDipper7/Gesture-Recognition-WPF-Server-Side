using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Media.Animation;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Threading;
using System.IO.Ports;
using ServiceExample;

using Eneter.Messaging.EndPoints.TypedMessages;
using Eneter.Messaging.MessagingSystems.MessagingSystemBase;
using Eneter.Messaging.MessagingSystems.TcpMessagingSystem;
using Eneter.Messaging.MessagingSystems.Composites;
using Eneter.Messaging.DataProcessing.Serializing;

using Fleck;


namespace WpfApplicationTest
{
    /// <summary>
    /// MainWindow.xaml 的交互逻辑
    /// </summary>
    public partial class MainWindow : Window
    {
        private int currentState = STATE_CLOSED;
        private int beforePauseState = STATE_PAUSE;
        private const int STATE_OPENING = 0;
        private const int STATE_CLOSING = 1;
        private const int STATE_PAUSE = 2;
        private const int STATE_OPENED = 3;
        private const int STATE_CLOSED = 4;

        private const int EVENT_BTN_CLOSE_CLICK = 11;
        private const int EVENT_BTN_OPEN_CLICK = 12;

        private Storyboard sbopening = null;
        private Storyboard sbclosing = null;
        private TimeSpan pauseTime;

        private SerialPort mySerialPort = null;
        private const string GESTURE_CLOSE = "#CLOSING_20#";
        private const string GESTURE_OPEN = "#OPENING_20#";
        private const string GESTURE_STOP = "#STOPPED#";

        public MainWindow()
        {
            InitializeComponent();

            //this.Topmost = true;
            //this.Hide();
            //this.Show();

            refreshStoryBoard();
            Thread eneterThread = new Thread(new ThreadStart(runWebSocketServer));
            eneterThread.Start();

            Thread portThread = new Thread(new ThreadStart(initSerialPortInfo));
            portThread.Start();
        }

        private void refreshStoryBoard() 
        {
            if (sbopening == null)
                // Locate Storyboard resource
                sbopening = (Storyboard)TryFindResource("sbopening");
            if (sbclosing == null)
                // Locate Storyboard resource
                sbclosing = (Storyboard)TryFindResource("sbclosing");

            sbclosing.Completed += (o, s) => { currentState = STATE_CLOSED; labelState.Content = currentState; };
            sbopening.Completed += (o, s) => { currentState = STATE_OPENED; labelState.Content = currentState; };
        }

        public void simulateOpenAction()
        {
            log("Befor Open - " + currentState);
            //refreshStoryBoard();
            log("Befor Open - " + currentState);
            this.Dispatcher.Invoke(new Action(() => {
                checkStateWithBtnClickEvent(EVENT_BTN_OPEN_CLICK);
            }));
            log("After - " + currentState);
        }

        public void simulateCloseAction()
        {
            log("Befor Close - " + currentState);
            //refreshStoryBoard();
            this.Dispatcher.Invoke(new Action(() =>
            {
                checkStateWithBtnClickEvent(EVENT_BTN_CLOSE_CLICK);
            }));
            //checkStateWithBtnClickEvent(EVENT_BTN_CLOSE_CLICK);
            log("After - " + currentState);
        }


        private void checkStateWithBtnClickEvent(int EVENT_BTN_CLICK)
        {

            log("curr: " + currentState);
            switch (currentState) {
                case STATE_CLOSED:
                    if (EVENT_BTN_CLICK == EVENT_BTN_CLOSE_CLICK)
                    {
                        //Do Nothing #CLOSED
                        currentState = STATE_CLOSED;
                        sbclosing.Stop();
                        sbopening.Stop();
                    }
                    else if (EVENT_BTN_CLICK == EVENT_BTN_OPEN_CLICK)
                    { 
                        //Transfer #OPENING
                        currentState = STATE_OPENING;
                        
                        //#Closed -> # Opening
                        sbclosing.Stop();
                        sbopening.Begin();
                    }
                    break;
                case STATE_CLOSING:
                    if (EVENT_BTN_CLICK == EVENT_BTN_CLOSE_CLICK)
                    {
                        //Do nothing while closing
                        currentState = STATE_CLOSING;
                    }
                    else if (EVENT_BTN_CLICK == EVENT_BTN_OPEN_CLICK)
                    {
                        //Pause it
                        currentState = STATE_PAUSE;
                        sbclosing.Pause();
                        //must record the timeSpan
                        pauseTime = sbclosing.GetCurrentTime();
                        beforePauseState = STATE_CLOSING;
                    }
                    break;
                case STATE_OPENED:
                    if (EVENT_BTN_CLICK == EVENT_BTN_CLOSE_CLICK)
                    {
                        //# Transfer #closing
                        currentState = STATE_CLOSING;

                        log("curr inner: " + currentState);
                        //#Opened -> Closing
                        sbopening.Stop();
                        sbclosing.Begin();
                        log("curr inner end: " + currentState);
                    }
                    else if (EVENT_BTN_CLICK == EVENT_BTN_OPEN_CLICK)
                    {
                        //Do Nothing #opened
                        currentState = STATE_OPENED;
                        log("curr inner: "+currentState);
                    }
                    break;
                case STATE_OPENING:
                    if (EVENT_BTN_CLICK == EVENT_BTN_CLOSE_CLICK)
                    {
                        // Pause
                        currentState = STATE_PAUSE;
                        sbopening.Pause();
                        //must record the timeSpan
                        pauseTime = sbopening.GetCurrentTime();
                        beforePauseState = STATE_OPENING;
                    }
                    else if (EVENT_BTN_CLICK == EVENT_BTN_OPEN_CLICK)
                    {
                        //Do Nothing while opening
                        currentState = STATE_OPENING;
                    }
                    break;
                case STATE_PAUSE:
                    if (EVENT_BTN_CLICK == EVENT_BTN_CLOSE_CLICK)
                    {
                        currentState = STATE_CLOSING;
                        //Go to closing, but must reset all  seek
                        sbclosing.Stop();
                        sbopening.Stop();
                        sbclosing.Begin();
                        if (beforePauseState == STATE_CLOSING)
                            sbclosing.Seek(pauseTime, TimeSeekOrigin.BeginTime);
                        else
                            sbclosing.Seek(new TimeSpan(0, 0, 0, 10, 0).Subtract(pauseTime), TimeSeekOrigin.BeginTime);
                    }
                    else if (EVENT_BTN_CLICK == EVENT_BTN_OPEN_CLICK)
                    {
                        currentState = STATE_OPENING;
                        //Go to opening, but must reset all  seek
                        sbclosing.Stop();
                        sbopening.Stop();
                        sbopening.Begin();

                        if (beforePauseState == STATE_OPENING)
                            sbopening.Seek(pauseTime, TimeSeekOrigin.BeginTime);
                        else
                            sbopening.Seek(new TimeSpan(0, 0, 0, 10, 0).Subtract(pauseTime), TimeSeekOrigin.BeginTime);
                        //sbopening.Seek(pauseTime, TimeSeekOrigin.BeginTime);
                    }
                    break;
                default:
                    break;
            }

            log("curr end: " + currentState);
        }


        public void runWebSocketServer()
        {
            var server = new WebSocketServer("ws://0.0.0.0:8090");
            server.RestartAfterListenError = true;
            server.Start(socket =>
            {
                socket.OnOpen = () =>
                    {
                        Console.WriteLine("Open!");
                    };
                socket.OnClose = () => 
                    {
                        Console.WriteLine("Close!");
                    };
                socket.OnMessage = message =>
                    {
                        socket.Send("received : " + message);
                        Console.WriteLine("receive msg..." + message);
                        
                        String msg = message;
                        if (msg.Equals("Open"))
                        {
                            Console.WriteLine("Simulate Open Action");
                            simulateOpenAction();
                        }
                        else if (msg.Equals("Close"))
                        {
                            Console.WriteLine("Simulate Close Action");
                            simulateCloseAction();
                        }
                    };
            });

            Console.WriteLine("Press any key to continue...");
            Console.WriteLine();
            Console.ReadLine();

            mySerialPort.Close();
        }

        public void initSerialPortInfo()
        {
            mySerialPort = new SerialPort("COM3");

            mySerialPort.BaudRate = 921600;
            mySerialPort.Parity = Parity.None;
            mySerialPort.StopBits = StopBits.One;
            mySerialPort.DataBits = 8;
            mySerialPort.Handshake = Handshake.None;
            mySerialPort.RtsEnable = true;

            mySerialPort.DataReceived += new SerialDataReceivedEventHandler(DataReceivedHandler);

            try
            {
                mySerialPort.Open();
            }
            catch (Exception e)
            {
                Console.WriteLine("Exception e:"+e);
            }
            //Console.WriteLine("Press any key to continue...");
            //Console.WriteLine();
            //Console.ReadKey();
        }


        //private string GESTURE_BEFORE_INFO = null;
        private void DataReceivedHandler(
                            object sender,
                            SerialDataReceivedEventArgs e)
        {
            SerialPort sp = (SerialPort)sender;
            string indata = sp.ReadExisting();
            Console.WriteLine("Data Received:");
            Console.WriteLine(indata);

            if (indata.Equals(GESTURE_OPEN))
            {
                Console.WriteLine("[GESTURE] Simulate Open Action");
                simulateOpenAction();
            }
            else if (indata.Equals(GESTURE_STOP))
            {
                Console.WriteLine("data: " + indata);
            }
            else if (indata.Equals(GESTURE_CLOSE))
            {
                Console.WriteLine("[GESTURE] Simulate Close Action");
                simulateCloseAction();
            }
            else
            {
                Console.WriteLine("data: [" + indata+"]");
            }
        }






        private IDuplexTypedMessageReceiver<MyResponse, MyRequest> myReceiver;

        public void run()
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
        private void OnMessageReceived(object sender, TypedRequestReceivedEventArgs<MyRequest> e)
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
                simulateOpenAction();
            }
            else if (msg.Equals("Close"))
            {
                Console.WriteLine("Simulate Close Action");
                simulateCloseAction();
            }
        }

        public void log(string str)
        {
            Console.WriteLine(str);
        }


        
        private void btnOpen_Click(object sender, RoutedEventArgs e)
        {
            checkStateWithBtnClickEvent(EVENT_BTN_OPEN_CLICK);
            labelState.Content = currentState.ToString();


            //DoubleAnimation doubleAnimation = new DoubleAnimation();
            //doubleAnimation.Duration = TimeSpan.FromSeconds(10);
            //doubleAnimation.To = 200;

           //FloatInElement(0, 20, rectMask);
          // MoveToSW(rectMask, 222, 0);

            // Locate Storyboard resource
            //Storyboard s = (Storyboard)TryFindResource("sb");
            //s.Begin();	// Start animation
        }

        private void btnClose_Click(object sender, RoutedEventArgs e)
        {
            checkStateWithBtnClickEvent(EVENT_BTN_CLOSE_CLICK);
            labelState.Content = currentState.ToString();

            /*
            Thread thread = new Thread(new ThreadStart(refreshCord));
            //thread.Start();
            refreshCord();

            // Locate Storyboard resource
            Storyboard s = (Storyboard)TryFindResource("sb");
            s.Stop();	// Start animation*/
        }

        /*
        private void refreshCord()
        {
            double top = Canvas.GetTop(rectMask);
            double left = Canvas.GetLeft(rectMask);

            double caLe = canvas1.Margin.Left;
            double caRi = canvas1.Margin.Right;
            double caTo = canvas1.Margin.Top;
            double caBo = canvas1.Margin.Bottom;

            labelState.Content = String.Format("top:{0} | left:{1} | | caTop:{2} | caLeft:{3} | caBottom:{4} | caRight:{5}", top, left, caTo, caLe, caBo, caRi);

        }

        /// 移动动画 
        /// 
        /// <param name="top">目标点相对于上端的位置</param>
        /// <param name="left">目标点相对于左端的位置</param>
        /// <param name="elem">移动元素</param>
        public static void FloatInElement(double top, double left, UIElement elem)
        {
            try
            {
                DoubleAnimation floatY = new DoubleAnimation()
                {
                   
                    Duration = new TimeSpan(0, 0, 0, 5, 0),
                };
                floatY.To = top;
                floatY.IsCumulative = true;
                DoubleAnimation floatX = new DoubleAnimation()
                {
                    
                    Duration = new TimeSpan(0, 0, 0, 5, 0),
                };
                floatX.To = left;
                floatX.IsCumulative = true;
                
                elem.BeginAnimation(Canvas.TopProperty, floatY);
                elem.BeginAnimation(Canvas.LeftProperty, floatX);
            }
            catch (Exception)
            {

                throw;
            }
        }

        public void MoveToSW( UIElement target, double newX, double newY)
        {
            var top = Canvas.GetTop(target);
            var left = Canvas.GetLeft(target);

            labelState.Content = "top:" + top + "|left:" + left;
            TranslateTransform trans = new TranslateTransform();
            target.RenderTransform = trans;
            //DoubleAnimation anim1 = new DoubleAnimation(top, newY - top, TimeSpan.FromSeconds(10));
            DoubleAnimation anim2 = new DoubleAnimation(left, newX - left, TimeSpan.FromSeconds(10));
            //trans.BeginAnimation(TranslateTransform.XProperty, anim1);
            trans.BeginAnimation(TranslateTransform.YProperty, anim2);
        }



        private void button1_Click(object sender, RoutedEventArgs e)
        {
            // Locate Storyboard resource
            Storyboard s = (Storyboard)TryFindResource("sb");
            s.Begin();	// Start animation
        }

        private void button2_Click(object sender, RoutedEventArgs e)
        {
            // Locate Storyboard resource
            Storyboard s = (Storyboard)TryFindResource("sb");
            s.Stop();	// Start animation
        }

        private void button3_Click(object sender, RoutedEventArgs e)
        {
            // Locate Storyboard resource
            Storyboard s = (Storyboard)TryFindResource("sb");
            s.Pause();	// Start animation

            labelState.Content = s.GetCurrentTime().Seconds;

        }

        private void button4_Click(object sender, RoutedEventArgs e)
        {
            // Locate Storyboard resource
            Storyboard s = (Storyboard)TryFindResource("sb");
            s.Resume();	// Start animation
            labelState.Content = s.GetCurrentTime().Seconds;
        }

        */
    }

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

  
}
