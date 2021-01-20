using System.IO;
using System.Net.Sockets;
using System.Threading;
using FluorineFx.IO;
using FluorineFx.Messaging.Messages;

namespace Model.SocketModel
{
    //委托，用于实现消息处理
    public delegate void ReceivedHandler(SocketSendReceiveMs pTagSocket, AcknowledgeMessage pArg);

    public class SocketSendReceiveMs
    {
        public AutoResetEvent ThreadEvent = new AutoResetEvent(false);
        public int HttpLength = -1;
        public int tmpLen = 0;
        public string ExecuteMessageId = "";
        public byte[] recvBytes = new byte[1024];
        public MemoryStream Stream1 = new MemoryStream();
        public Socket c;
        public AMFMessage SendMsg;
        public AcknowledgeMessage ReceMsg;
        public ReceivedHandler Received;

        public object ReslutValue = null;

        public SocketSendReceiveMs()//构造函数
        {
            c = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            c.ReceiveTimeout = 100;
        }
    }
}
